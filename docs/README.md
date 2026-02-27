# 🌾 AgroSolutions.Telemetry API - Hackaton FIAP

> [Vídeo de Apresentação](https://youtu.be/gQLOlJ2EWxc)

> API RESTful para recebimento e processamento de dados de telemetria de sensores de campo agrícola, com análises inteligentes e alertas automatizados.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![REST Level 3](https://img.shields.io/badge/REST-Level%203%20(HATEOAS)-success)](https://martinfowler.com/articles/richardsonMaturityModel.html)

---

## 📋 Índice

- [Visão Geral](#visao-geral)
- [Arquitetura](#arquitetura)
- [Funcionalidades](#funcionalidades)
- [API RESTful - Nível 3](#api-restful-nivel-3)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Princípios SOLID](#principios-solid)
- [Tecnologias](#tecnologias)
- [CI/CD](#cicd)
- [Setup Rápido](#setup-rapido)
- [Testes](#testes)

---

## <a id="visao-geral"></a>🎯 Visão Geral

Microserviço responsável

### 🌟 Destaques

- ✅ **Clean Architecture** (Onion Architecture)
- ✅ **REST Level 3** (HATEOAS completo)
- ✅ **SOLID Principles** aplicados rigorosamente
- ✅ **Domain-Driven Design** (DDD)
- ✅ **Domain Events** com múltiplos Event Handlers de análise
- ✅ **Domain Services** para regras de negócio complexas
- ✅ **Azure CosmosDB** (Serverless) para persistência NoSQL otimizada para IoT
- ✅ **Mensageria** via Service Bus e RabbitMQ (Factory Pattern)
- ✅ **Azure Key Vault** para gestão centralizada de segredos com hierarquia de configuração
- ✅ **Health Checks** dinâmicos com auto-discovery
- ✅ **Observabilidade completa** (Logs estruturados, Correlation IDs, Elastic APM)
- ✅ **Testes em 4 camadas** (Unit, Integration, Architecture, Load)

---

### 📌 Requisitos Hackaton FIAP
  - **Arquitetura baseada em microserviços**
    - Microserviço para gestão de úsuários e autenticação JWT (banco SQL)
    - Microserviço para gestão de fazendas, talhões e safras (banco SQL)
    - Microserviço para injestão dos dados dos sensores (banco NoSQL - Requisito Opcional)
    - Funções Serverless para coleta de dados dos sensores (integração api de previsão do tempo - Requisito Opcional) 
  - **Orquestração com Kubernetes**
    - Imagens Docker otimizadas para .NET 8 (Alpine) 
    - Armazenamento das imagens no Azure Container Registry (ACR)
    - Microserviços hospedados em Azure Kubernetes Services (AKS)   
    - Manifestos Kubernetes para deploy, service, hpa, configMap e secrets
  - **Observabilidade**
    - Elastic APM para monitoramento de performance e rastreamento distribuído
    - Elasticsearch para armazenamento e análise de logs estruturados
    - Kibana para dashboards
  - **Mensageria**
    - ServiceBus para comunicação assíncrona entre microserviços
    - Azure Functions (Queue trigger) para processamento de mensagens em tempo real (componente Serverless - Requisito Opcional)
    - Azure Functions (Timer trigger) para coleta de dados dos sensores a cada hora (componente Serverless - Requisito Opcional)
  - **CI/CD Automatizado**
    - Github Actions para build, testes, build de imagem Docker e deploy no AKS
    - Stages de DEV, STAGING e PROD com aprovações manuais para deploy em produção
  - **Adoção das melhores práticas de arquitetura e dev**
    - Clean Architecture (Onion Architecture)
    - API RESTful Level 3 (HATEOAS completo)
    - SOLID Principles aplicados rigorosamente
    - Patterns como Repository, Unit of Work, Factory, Strategy, 
    - Domain-Driven Design (DDD)
    - Health Checks dinâmicos
    - Observabilidade completa (Logs estruturados, Correlation IDs)
    - Testes em 4 camadas (Unit, Integration, Architecture, Load)

---

## <a id="arquitetura"></a>🏗️ Arquitetura

### Clean

```
┌─────────────────────────────────────────────┐
│              API (Presentation)             │  ← Controllers, Middlewares
├─────────────────────────────────────────────┤
│           Application (Use Cases)           │  ← Services, DTOs, Event Handlers
├─────────────────────────────────────────────┤
│              Domain (Core)                  │  ← Entities, Events, Domain Services
├─────────────────────────────────────────────┤
│          Infrastructure (External)          │  ← CosmosDB, Service Bus, Elastic
└─────────────────────────────────────────────┘
```

**Dependency Rule:** Domain ← Application ← Infrastructure ← API


### Fluxo de Processamento

```
         Sensor → API (POST /v1/field-measurements)
           ↓
         FieldMeasurementService (orquestração)
           ↓
         CosmosDB (persistência)
           ↓
         MeasurementCreatedEvent (Domain Event)
           ↓
         Event Handlers (análises paralelas):
           ├── DroughtAnalysisEventHandler
           ├── IrrigationAnalysisEventHandler
           ├── HeatStressAnalysisEventHandler
           ├── PestRiskAnalysisEventHandler
           ├── ExcessiveRainfallAnalysisEventHandler
           ├── ExtremeHeatAnalysisEventHandler
           ├── FreezingTemperatureAnalysisEventHandler
           └── ElasticMeasurementEventHandler (sync Elasticsearch)
           ↓
         Service Bus / RabbitMQ (alertas e notificações)
```

### Padrões Utilizados

- **Clean Architecture**: Separação clara de responsabilidades
- **SOLID Principles**: Código manutenível e testável
- **Domain Events**: Processamento assíncrono de regras de negócio
- **Domain Services**: Lógica de negócio complexa (DroughtDetection, HeatStressAnalysis, IrrigationRecommendation, PestRiskAnalysis)
- **Repository Pattern**: Abstração de acesso a dados (CosmosDB)
- **Factory Pattern**: Seleção dinâmica de publisher de mensageria (ServiceBus/RabbitMQ)
- **Configuration Hierarchy**: Azure Key Vault > Environment Variables > appsettings.{env}.json > appsettings.json

**Benefícios:**
- ✅ Domain independente de infraestrutura
- ✅ Fácil substituição de frameworks/bancos
- ✅ Testável sem dependências externas
- ✅ Escalável e manutenível

---

## <a id="funcionalidades"></a>🌾 Funcionalidades

- **Recepção
- **Armazenamento CosmosDB**: Persiste dados em banco NoSQL Serverless otimizado para IoT (partition key: `/fieldId`)
- **Análise de Seca**: Detecta condições de seca prolongada (umidade abaixo do threshold por período contínuo)
- **Recomendação de Irrigação**: Calcula urgência e quantidade de água necessária com base nas condições do solo
- **Análise de Estresse Térmico**: Detecta condições de calor que afetam as culturas
- **Avaliação de Risco de Pragas**: Identifica condições favoráveis à proliferação de pragas
- **Detecção de Chuva Excessiva**: Monitora precipitação acima dos limites definidos
- **Detecção de Calor Extremo**: Alerta sobre temperaturas extremas
- **Detecção de Congelamento**: Alerta sobre temperaturas de congelamento
- **Sync Elasticsearch**: Indexação automática de medições para busca e analytics
- **Alertas via Mensageria**: Publica notificações template-based no Service Bus/RabbitMQ

---

## <a id="api-restful-nivel-3"></a>🌐 API RESTful - Nível 3 (HATEOAS)

### Richardson
```
Nível 3: HATEOAS     ← ✅ Esta API
Nível 2: HTTP Verbs  ← ✅
Nível 1: Resources   ← ✅
Nível 0: POX         
```

### Requisitos REST Implementados

| Requisito | Descrição | Status | Padrão |
|-----------|-----------|--------|--------|
| **URIs substantivos** | Recursos com substantivos no plural | ✅ | `/field-measurements` |
| **Hierarquia de URIs** | Relacionamentos claros | ✅ | `/field-measurements/field/{fieldId}` |
| **HTTP Verbs** | GET, POST corretos | ✅ | Semântica HTTP |
| **Idempotência** | GET idempotente | ✅ | RFC 7231 |
| **Status Codes** | 2xx, 4xx, 5xx apropriados | ✅ | HTTP Standards |
| **HATEOAS** | Links de navegação em respostas | ✅ | Richardson Level 3 |
| **Versionamento** | URL versioning | ✅ | `/v1/` |
| **Paginação** | Metadados + links navegação | ✅ | `page`, `pageSize` |
| **Content Negotiation** | Accept/Content-Type headers | ✅ | `application/json` |
| **Error Handling** | RFC 7807 Problem Details | ✅ | Padronizado |
| **Stateless** | Sem estado no servidor | ✅ | JWT tokens |
| **CORS** | Cross-Origin Resource Sharing | ✅ | Configurável |
| **Correlation IDs** | Rastreamento distribuído | ✅ | `X-Correlation-ID` |

### REST Constraints (Roy Fielding)

| Constraint | Status |
|-----------|--------|
| Client-Server | ✅ Separação de responsabilidades |
| Stateless | ✅ Sem sessão, requisições auto-contidas |
| Cacheable | ✅ Headers de cache |
| Layered System | ✅ Load Balancer → Gateway → API → CosmosDB |
| Uniform Interface | ✅ URIs padronizadas, HATEOAS |
| Code on Demand | ⚠️ Opcional (não implementado) |

---

## <a id="estrutura-do-projeto"></a>📁 Estrutura do Projeto

```
AgroSolutions.Telemetry/
│
├── 📂 API/                          # Presentation Layer
│   ├── Controllers/v1/                 # FieldMeasurementsController, HealthController
│   ├── Middlewares/                    # Error, Logging, Security, Cache, ApiVersion
│   ├── Configurations/                 # DI, Swagger, CORS, Auth, Versioning, Validation, Logging
│   ├── Helpers/                        # HATEOAS Helper
│   ├── Models/                         # ErrorResponse
│   ├── Program.cs                      # Entry point (Serilog bootstrap)
│   ├── appsettings.json                # Configurações de produção
│   └── appsettings.Development.json    # Configurações de desenvolvimento
│
├── 📂 Application/                  # Use Cases
│   ├── Services/                       # FieldMeasurementService, HealthCheckService, DomainEventDispatcher
│   ├── Interfaces/                     # Contratos (IFieldMeasurementService, IMessagePublisher, IElasticService, etc.)
│   ├── EventHandlers/                  # Handlers de análise (Drought, Irrigation, HeatStress, PestRisk, etc.)
│   ├── Mappings/                       # Extensions de mapeamento FieldMeasurement ↔ DTO
│   ├── DTO/                            # Request/Response DTOs (FieldMeasurement, Health, Notification, Common)
│   ├── Exceptions/                     # ValidationException
│   ├── Helpers/                        # DateTimeHelper
│   └── Settings/                       # Configurações tipadas (Logger, Elastic, Alert, NewRelic)
│
├── 📂 Domain/                       # Core Business
│   ├── Entities/                       # FieldMeasurement, RequestLog
│   ├── Common/                         # BaseEntity, IHasDomainEvents
│   ├── ValueObjects/                   # AnalysisResults (IrrigationRecommendation, DroughtCondition, etc.)
│   ├── Events/                         # MeasurementCreatedEvent, IDomainEvent
│   ├── Services/                       # Domain Services (DroughtDetection, HeatStressAnalysis, IrrigationRecommendation, PestRiskAnalysis)
│   ├── Enums/                          # LogLevelEnum
│   ├── Exceptions/                     # BusinessException
│   └── Repositories/                   # IFieldMeasurementRepository
│
├── 📂 Infrastructure/               # External Concerns
│   ├── Context/                        # CorrelationContext
│   ├── Configurations/                 # CosmosSystemTextJsonSerializer
│   ├── Repositories/                   # FieldMeasurementRepository (CosmosDB)
│   ├── Factories/                      # MessagePublisherFactory (ServiceBus/RabbitMQ)
│   └── Services/                       
│       ├── Logging/                    # DatabaseLoggerService, ElasticLoggerService, NewRelicLoggerService
│       ├── HealthCheck/                # CosmosDBHealthCheck, ElasticsearchHealthCheck, ServiceBusHealthCheck, SystemHealthCheck
│       ├── Elastic/                    # ElasticService
│       ├── ServiceBusPublisher.cs      # Azure Service Bus publisher
│       └── RabbitMQPublisher.cs        # RabbitMQ publisher
│
├── 📂 Tests/                        # Tests Layer
│   ├── UnitTests/                      
│   │   ├── Domain/Entities/            # FieldMeasurementTests
│   │   ├── Domain/Events/              # MeasurementCreatedEventTests
│   │   ├── Application/EventHandlers/  # DroughtAnalysis, ExcessiveRainfall, ExtremeHeat, FreezingTemperature
│   │   ├── Application/Mappings/       # FieldMeasurementMappingExtensionsTests
│   │   └── Application/Helpers/        # DateTimeHelperTests
│   ├── ArchitectureTests/              # LayerDependency, SolidPrinciples, NamingConvention, Security, Api, Performance, Testability
│   ├── IntegrationTests/               # API Controllers, Security
│   └── LoadTests/                      # k6 load testing (load-test.js)
│
├── 📂 docs/                         # Documentation
│   ├── SOLID_Summary.md                # Análise SOLID detalhada
│   ├── Architecture.drawio             # Diagramas de arquitetura
│   └── README.md                       # Este arquivo
│
├── 📂 Kubernetes/                   # Kubernetes manifests
│   ├── deployment.yaml                 # Deployment do microserviço
│   ├── service.yaml                    # Service (ClusterIP/LoadBalancer)
│   ├── hpa.yaml                        # Horizontal Pod Autoscaler
│   ├── configmap.yaml                  # ConfigMap
│   ├── secret.yaml                     # Secrets
│   ├── namespace.yaml                  # Namespace
│   └── AzureAKS_script.txt            # Scripts auxiliares AKS
│
├── 📂 .github/                      # GitHub workflows
│   └── workflows/ci-cd-aks.yml        # CI/CD para AKS
│
├── .gitignore                       # Arquivos ignorados pelo Git
├── Dockerfile                       # Imagem Docker da API (multi-stage, Alpine)
└── AgroSolutions.Telemetry.sln      # Solution .NET
```

---

## ✨ Funcionalidades Principais

### 🔹 Ingestão de Telemetria (Field Measurements)
- Recepção de medições de sensores (umidade do solo, temperatura do ar, precipitação)
- Validações de negócio (ranges, campos obrigatórios, e-mail de alerta)
- Persistência em Azure CosmosDB (Serverless, partition key: `/fieldId`)
- Links HATEOAS para navegação entre recursos
- Paginação com metadados

### 🔹 Análises Inteligentes (Domain Events)
- **Detecção de Seca**: Análise de umidade do solo abaixo do threshold por período contínuo
- **Recomendação de Irrigação**: Cálculo de urgência (None/Low/Medium/High) e volume de água (mm)
- **Estresse Térmico**: Detecção de condições de calor prejudiciais
- **Risco de Pragas**: Avaliação de condições favoráveis à proliferação
- **Chuva Excessiva**: Monitoramento de precipitação acima dos limites
- **Calor Extremo**: Alerta sobre temperaturas extremas
- **Congelamento**: Alerta sobre temperaturas de congelamento

### 🔹 Mensageria e Notificações
- **Factory Pattern**: Seleção dinâmica entre ServiceBus e RabbitMQ
- **Template-based Notifications**: Alertas com templates parametrizados (TemplateId + Parameters)
- **Prioridade**: Low, Normal, High, Critical

### 🔹 Observabilidade
- **Logging multi-destino:** Database, Elasticsearch, New Relic (via Serilog)
- **Correlation IDs:** Rastreamento distribuído
- **Elastic APM:** Monitoramento de performance e tracing
- **Structured Logs:** Serilog com contexto completo

### 🔹 Health Checks Dinâmicos
- **Auto-discovery** via `IEnumerable<IHealthCheck>`
- **Checks:** CosmosDB, Elasticsearch, Service Bus, System
- **Extensível:** Adicione novo check sem modificar código existente

### 🔹 Segurança
- **Azure Key Vault**: Gestão centralizada de segredos (connection strings, tokens, API keys) com integração nativa via `Azure.Extensions.AspNetCore.Configuration.Secrets`. Hierarquia de configuração com prioridade: KeyVault > Environment Variables > appsettings.{env}.json > appsettings.json
- **Diagnóstico de Configuração**: Log estruturado no startup identificando a origem de cada configuração (KeyVault, EnvVar, JSON) via `ConfigurationSourceLogger`
- JWT Bearer Authentication
- Security Headers (HSTS, CSP, X-Frame-Options)
- CORS configurável

---

## <a id="principios-solid"></a>🎯 Princípios SOLID

### Resumo

| Princípio | Aplicação |
|-----------|-----------|
| **S** - Single Responsibility | Cada classe tem 1 responsabilidade (FieldMeasurementService orquestra, Event Handlers analisam, Domain Services calculam) |
| **O** - Open/Closed | Extensível sem modificar (novo Event Handler → registra no DI; novo Publisher → adiciona case na Factory) |
| **L** - Liskov Substitution | ILoggerService → Database/Elastic/NewRelic substituíveis; IMessagePublisher → ServiceBus/RabbitMQ substituíveis |
| **I** - Interface Segregation | Interfaces coesas (IFieldMeasurementRepository, IMessagePublisher, IElasticService, IHealthCheck) |
| **D** - Dependency Inversion | Depende de abstrações, não implementações (Repository no Domain, implementação na Infrastructure) |

### Exemplos Práticos

**Adicionar novo Event Handler de análise:**
```csharp
// 1. Implementar interface
public class NewAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
{
    public async Task HandleAsync(MeasurementCreatedEvent domainEvent) { ... }
}

// 2. Registrar no DI
builder.Services.AddScoped<IDomainEventHandler<MeasurementCreatedEvent>, NewAnalysisEventHandler>();

// ✅ DomainEventDispatcher descobre e executa automaticamente!
```

**Adicionar novo Health Check:**
```csharp
// 1. Implementar interface
public class RedisHealthCheck : IHealthCheck
{
    public string ComponentName => "Redis";
    public bool IsCritical => false;
    public Task<ComponentHealth> CheckHealthAsync() { ... }
}

// 2. Registrar no DI
builder.Services.AddScoped<IHealthCheck, RedisHealthCheck>();

// ✅ HealthCheckService descobre automaticamente!
```

**Trocar Logger:**
```csharp
// Apenas alterar configuração
"LoggerSettings": { "Provider": "Elastic" }  // ou "Database", "NewRelic"

// Código cliente não muda! (Dependency Inversion)
```

📚 **Documentação completa:** `docs/SOLID_Summary.md`

---

## <a id="tecnologias"></a>🛠️ Tecnologias

**Core:**
**Persistência:** Azure CosmosDB (Serverless, SDK v3)  
**Mensageria:** Azure Service Bus, RabbitMQ  
**Logging:** Serilog (Elasticsearch, SQL Server, New Relic)  
**APM:** Elastic APM  
**Segredos:** Azure Key Vault  
**Auth:** JWT Bearer Authentication  
**Testes:** xUnit, Moq, FluentAssertions, NetArchTest, k6  
**Infra:** Docker (Alpine), Kubernetes (AKS), GitHub Actions  
**Documentação:** Swagger/OpenAPI 3.0  

---

## <a id="cicd"></a>🚀 CI/CD

### Pipeline

A aplicação possui pipeline CI/CD via GitHub Actions (`.github/workflows/ci-cd-aks.yml`) com:
- Build e restauração de dependências
- Execução de testes (Architecture, Unit, Integration) + Code Coverage
- Build e push de imagem Docker no Azure Container Registry (ACR)
- Deploy no Azure Kubernetes Service (AKS)

---

## <a id="setup-rapido"></a>🚀 Setup Rápido

```bash
# 1. Clonar
git clone https://github.com/fkwesley/AgroSolutions.Telemetry.git
cd AgroSolutions.Telemetry

# 2. Restaurar dependências
dotnet restore

# 3. Configurar conexões (appsettings.Development.json)
"ConnectionStrings": {
  "TelemetryDbConnection": "<CosmosDB connection string>",
  "ServiceBusConnection": "<Service Bus connection string>"
}

# 3.1 (Opcional) Habilitar Azure Key Vault
"KeyVault": {
  "VaultUri": "https://<seu-vault>.vault.azure.net/",
  "Enabled": "true"
}
# Quando habilitado, segredos do Key Vault sobrescrevem valores locais automaticamente.

# 4. Executar
cd API
dotnet run

# ✅ API disponível em: https://localhost:5001/swagger
```

---

## <a id="testes"></a>🧪 Testes

### Pirâmide
```
      /\
     /E2E\        ← 5% (críticos)
    /------\
   / Integr \     ← 20% (API, Security)
  /----------\
 /Unit Tests  \   ← 70% (Entities, EventHandlers, Mappings, Helpers)
/______________\
  + Architecture  ← 5% (Layers, SOLID, Naming, Security, API Design, Performance, Testability)
```

### Executar
```bash
dotnet test                                                          # Todos
dotnet test --filter "FullyQualifiedName~UnitTests"                 # Unitários
dotnet test --filter "FullyQualifiedName~IntegrationTests"          # Integração
dotnet test --filter "FullyQualifiedName~ArchitectureTests"         # Arquitetura
k6 run Tests/LoadTests/load-test.js                                  # Carga
```

### Arquitetura (NetArchTest)
```csharp
// Valida Clean Architecture
Types.InAssembly(domainAssembly)
    .ShouldNot().HaveDependencyOn("Infrastructure")
    .GetResult().IsSuccessful.Should().BeTrue();
```

---

## 👨‍💻 Autor

**Frank Vieira** - [GitHub](https://github.com/fkwesley)