using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    /// <summary>
    /// Publisher RabbitMQ com conexão lazy (conecta apenas quando publica).
    /// 
    /// ?? VANTAGEM:
    /// - Não falha no startup se RabbitMQ estiver offline
    /// - Conecta apenas quando realmente precisa publicar
    /// - Permite retry automático em caso de falha
    /// </summary>
    public class RabbitMQPublisher : IMessagePublisher, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<RabbitMQPublisher>? _logger;
        private IConnection? _connection;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private bool _disposed;

        public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher>? logger = null)
        {
            _connectionString = configuration.GetConnectionString("FCGRabbitMQConnection")
                ?? throw new ArgumentNullException("RabbitMQ connection string not found.");
            
            _logger = logger;
            
            // ?? NÃO conecta aqui! Apenas guarda a connection string
        }

        /// <summary>
        /// Garante que existe uma conexão ativa. Conecta lazy apenas quando necessário.
        /// </summary>
        private async Task EnsureConnectionAsync()
        {
            // Se já tem conexão e está aberta, retorna
            if (_connection is { IsOpen: true })
                return;

            // Lock para evitar múltiplas conexões simultâneas
            await _connectionLock.WaitAsync();
            try
            {
                // Double-check dentro do lock
                if (_connection is { IsOpen: true })
                    return;

                _logger?.LogInformation("Connecting to RabbitMQ...");

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_connectionString),
                    AutomaticRecoveryEnabled = true,  // ?? Reconecta automaticamente
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                // ?? AQUI conecta pela primeira vez (lazy)
                _connection = await factory.CreateConnectionAsync();
                
                _logger?.LogInformation("Connected to RabbitMQ successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task PublishMessageAsync(string queueName, object message, IDictionary<string, object>? customProperties = null)
        {
            try
            {
                // ?? Conecta apenas aqui, quando precisa publicar
                await EnsureConnectionAsync();

                await using var channel = await _connection!.CreateChannelAsync();

                // garante que a fila existe
                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                var properties = new BasicProperties
                {
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    Headers = customProperties as IDictionary<string, object?>
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger?.LogInformation("Message published to queue {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
                
                // Invalida conexão para forçar reconexão na próxima tentativa
                _connection?.Dispose();
                _connection = null;
                
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _connection?.Dispose();
            _connectionLock?.Dispose();
            _disposed = true;
            
            GC.SuppressFinalize(this);
        }
    }
}

