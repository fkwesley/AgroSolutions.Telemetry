using Application.EventHandlers;
using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Domain.Events;
using Domain.Repositories;
using Elastic.Apm.NetCoreAll;
using Infrastructure.Factories;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Elastic;
using Infrastructure.Services.HealthCheck;
using Infrastructure.Services.Logging;

namespace API.Configurations;

// #SOLID - Dependency Inversion Principle (DIP)
// Esta classe de configuração registra as abstrações (interfaces) com suas implementações concretas.
// O código cliente sempre depende de interfaces, nunca de implementações.

// #SOLID - Single Responsibility Principle (SRP)
// Esta classe tem uma única responsabilidade: configurar a injeção de dependências.

// #SOLID - Open/Closed Principle (OCP)
// Para adicionar novos serviços, basta registrá-los aqui sem modificar o código existente.
// Por exemplo: adicionar novo logger ou message publisher não requer mudanças em outras classes.

public static class DependencyInjectionConfiguration
{
    public static WebApplicationBuilder AddDependencyInjection(this WebApplicationBuilder builder)
    {
        // Settings
        builder.Services.Configure<LoggerSettings>(builder.Configuration.GetSection("LoggerSettings"));
        builder.Services.Configure<NewRelicLoggerSettings>(builder.Configuration.GetSection("NewRelic"));
        builder.Services.Configure<ElasticLoggerSettings>(builder.Configuration.GetSection("ElasticLogs"));

        // Elastic Services (Generic)
        builder.Services.AddSingleton<IElasticService, ElasticService>();

        // Domain Services
        builder.Services.AddScoped<IFieldMeasurementService, FieldMeasurementService>();
        
        // Health Check Services
        // #SOLID - Open/Closed Principle (OCP)
        // Para adicionar novo health check:
        // 1. Criar classe implementando IHealthCheck
        // 2. Registrar aqui: builder.Services.AddScoped<IHealthCheck, MeuNovoHealthCheck>()
        // 3. HealthCheckService descobre automaticamente via IEnumerable<IHealthCheck>
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
        builder.Services.AddScoped<IHealthCheck, CosmosDBHealthCheck>();
        builder.Services.AddScoped<IHealthCheck, ServiceBusHealthCheck>();
        builder.Services.AddScoped<IHealthCheck, ElasticsearchHealthCheck>();
        builder.Services.AddScoped<IHealthCheck, SystemHealthCheck>();

        // Domain Event Dispatcher & Handlers
        builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        builder.Services.AddScoped<IDomainEventHandler<MeasurementCreatedEvent>, ElasticMeasurementEventHandler>();
        builder.Services.AddScoped<IDomainEventHandler<DroughtAlertRequiredEvent>, DroughtAlertRequiredEventHandler>();

        // Logger Services
        ConfigureLoggerService(builder);

        // Message Publishers
        builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
        builder.Services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
        builder.Services.AddSingleton<RabbitMQPublisher>();
        builder.Services.AddSingleton<ServiceBusPublisher>();
        builder.Services.AddSingleton<IMessagePublisherFactory, MessagePublisherFactory>();

        // Repositories
        builder.Services.AddScoped<IFieldMeasurementRepository, FieldMeasurementRepository>();

        // Other Services
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Elastic APM
        if (builder.Configuration.GetValue<bool>("ElasticApm:Enabled"))
            builder.Services.AddAllElasticApm();

        return builder;
    }

    private static void ConfigureLoggerService(WebApplicationBuilder builder)
    {
        // #SOLID - Liskov Substitution Principle (LSP)
        // DatabaseLoggerService, ElasticLoggerService e NewRelicLoggerService podem ser substituídos
        // entre si sem quebrar o código, pois todos implementam ILoggerService com o mesmo contrato.

        // #SOLID - Open/Closed Principle (OCP)
        // Para adicionar novo provider (ex: Azure Monitor), basta:
        // 1. Criar classe implementando ILoggerService
        // 2. Adicionar case no switch
        // Nenhum código cliente precisa ser alterado.

        // NOTA: A aplicação usa Serilog para logging estruturado.
        // - "Elastic": Envia logs estruturados para Elasticsearch
        // - "NewRelic": Envia logs para NewRelic APM
        // - "Database": Delega para Serilog (que já envia para Elastic via configuração)
        var loggerProvider = builder.Configuration.GetValue<string>("LoggerSettings:Provider") ?? "Elastic";

        switch (loggerProvider)
        {
            case "NewRelic":
                builder.Services.AddScoped<ILoggerService, NewRelicLoggerService>();
                break;

            case "Database":
                // Mantido para compatibilidade - delega para Serilog/Elastic
                builder.Services.AddScoped<ILoggerService, DatabaseLoggerService>();
                break;

            case "Elastic":
            default:
                builder.Services.AddScoped<ILoggerService, ElasticLoggerService>();
                break;
        }
    }
}