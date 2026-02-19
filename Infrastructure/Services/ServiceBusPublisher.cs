using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Publisher Azure Service Bus com conexão lazy (conecta apenas quando publica).
    /// 
    /// ?? VANTAGEM:
    /// - Não falha no startup se Service Bus estiver offline
    /// - Conecta apenas quando realmente precisa publicar
    /// - Permite retry automático em caso de falha
    /// </summary>
    public class ServiceBusPublisher : IMessagePublisher, IDisposable
    {
        private readonly string? _connectionString;
        private readonly ILogger<ServiceBusPublisher>? _logger;
        private ServiceBusClient? _serviceBusClient;
        private readonly SemaphoreSlim _clientLock = new(1, 1);
        private bool _disposed;

        public ServiceBusPublisher(IConfiguration configuration, ILogger<ServiceBusPublisher>? logger = null)
        {
            _connectionString = configuration.GetConnectionString("ServiceBusConnection");
            _logger = logger;
        }

        /// <summary>
        /// Garante que existe um cliente ativo. Conecta lazy apenas quando necessário.
        /// </summary>
        private async Task EnsureClientAsync()
        {
            // Se já tem cliente, retorna
            if (_serviceBusClient != null)
                return;

            // Lock para evitar múltiplas conexões simultâneas
            await _clientLock.WaitAsync();
            try
            {
                // Double-check dentro do lock
                if (_serviceBusClient != null)
                    return;

                if (string.IsNullOrEmpty(_connectionString))
                {
                    _logger?.LogWarning("Azure Service Bus connection string not configured. Messages will not be sent.");
                    return;
                }

                _logger?.LogInformation("Connecting to Azure Service Bus...");

                // ?? AQUI conecta pela primeira vez (lazy)
                _serviceBusClient = new ServiceBusClient(_connectionString);
                
                _logger?.LogInformation("Connected to Azure Service Bus successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect to Azure Service Bus");
                throw;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async Task PublishMessageAsync(string topicName, object message, IDictionary<string, object>? customProperties = null)
        {
            try
            {
                // ?? Conecta apenas aqui, quando precisa publicar
                await EnsureClientAsync();

                if (_serviceBusClient == null)
                {
                    _logger?.LogWarning("Service Bus client not configured. Skipping message publish to {TopicName}", topicName);
                    return;
                }

                var sender = _serviceBusClient.CreateSender(topicName);
                var serviceBusMessage = new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(message))
                {
                    ContentType = "application/json"
                };

                // Adding custom properties if provided
                if (customProperties != null && customProperties.Any())
                {
                    foreach (var kv in customProperties)
                        serviceBusMessage.ApplicationProperties[kv.Key] = kv.Value;
                }

                await sender.SendMessageAsync(serviceBusMessage);
                
                _logger?.LogInformation("Message published to topic {TopicName}", topicName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to publish message to topic {TopicName}", topicName);
                
                // Invalida cliente para forçar reconexão na próxima tentativa
                await (_serviceBusClient?.DisposeAsync() ?? ValueTask.CompletedTask);
                _serviceBusClient = null;
                
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _serviceBusClient?.DisposeAsync().GetAwaiter().GetResult();
            _clientLock?.Dispose();
            _disposed = true;
            
            GC.SuppressFinalize(this);
        }
    }
}
