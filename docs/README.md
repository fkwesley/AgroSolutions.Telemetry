# 🎮 API.Template - Clean Architecture & Best Practices

> Template de API RESTful moderna implementando Clean Architecture, SOLID, DDD e Event-Driven Architecture.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![REST Level 3](https://img.shields.io/badge/REST-Level%203%20(HATEOAS)-success)](https://martinfowler.com/articles/richardsonMaturityModel.html)

---

## 📋 Índice

- [Visão Geral](#-visão-geral)
- [Arquitetura](#-arquitetura)
- [API RESTful - Nível 3](#-api-restful---nível-3)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Funcionalidades Principais](#-funcionalidades-principais)
- [Princípios SOLID](#-princípios-solid)
- [Tecnologias](#-tecnologias)
- [Setup Rápido](#-setup-rápido)
- [Testes](#-testes)

---

## 🎯 Visão Geral

Template de referência para APIs RESTful escaláveis e manuteníveis, implementando as melhores práticas de arquitetura de software.

### 🌟 Destaques

- ✅ **Clean Architecture** (Onion Architecture)
- ✅ **REST Level 3** (HATEOAS completo)
- ✅ **SOLID Principles** aplicados rigorosamente
- ✅ **Domain-Driven Design** (DDD)
- ✅ **Event-Driven Architecture**
- ✅ **Health Checks** dinâmicos com auto-discovery
- ✅ **Observabilidade completa** (Logs estruturados, Correlation IDs)
- ✅ **Testes em 4 camadas** (Unit, Integration, Architecture, Load)

---

## 🏗️ Arquitetura

### Clean Architecture (Onion)

```
┌─────────────────────────────────────────────┐
│              API (Presentation)             │  ← Controllers, Middlewares
├─────────────────────────────────────────────┤
│           Application (Use Cases)           │  ← Services, DTOs, Event Handlers
├─────────────────────────────────────────────┤
│              Domain (Core)                  │  ← Entities, Events, Business Rules
├─────────────────────────────────────────────┤
│          Infrastructure (External)          │  ← DB, Messaging, External APIs
└─────────────────────────────────────────────┘
```

**Dependency Rule:** Domain ← Application ← Infrastructure ← API

**Benefícios:**
- ✅ Domain independente de infraestrutura
- ✅ Fácil substituição de frameworks/bancos
- ✅ Testável sem dependências externas
- ✅ Escalável e manutenível

---

## 🌐 API RESTful - Nível 3 (HATEOAS)

### Richardson Maturity Model
```
Nível 3: HATEOAS     ← ✅ Esta API
Nível 2: HTTP Verbs  ← ✅
Nível 1: Resources   ← ✅
Nível 0: POX         
```

### Requisitos REST Implementados

| Requisito | Descrição | Status | Padrão |
|-----------|-----------|--------|--------|
| **URIs substantivos** | Recursos com substantivos no plural | ✅ | `/orders`, `/games` |
| **Hierarquia de URIs** | Relacionamentos claros | ✅ | `/orders/{id}/game` |
| **HTTP Verbs** | GET, POST, PUT, DELETE corretos | ✅ | Semântica HTTP |
| **Idempotência** | GET, PUT, DELETE idempotentes | ✅ | RFC 7231 |
| **Status Codes** | 2xx, 3xx, 4xx, 5xx apropriados | ✅ | HTTP Standards |
| **HATEOAS** | Links de navegação em respostas | ✅ | Richardson Level 3 |
| **Links Dinâmicos** | Links baseados no estado do recurso | ✅ | State Machine |
| **Versionamento** | URL + Header versioning | ✅ | `/v1/`, `/v2/` |
| **Paginação** | Metadados + links navegação | ✅ | `page`, `pageSize` |
| **Content Negotiation** | Accept/Content-Type headers | ✅ | `application/json` |
| **Error Handling** | RFC 7807 Problem Details | ✅ | Padronizado |
| **Stateless** | Sem estado no servidor | ✅ | JWT tokens |
| **Cacheable** | Headers de cache | ✅ | `Cache-Control`, `ETag` |
| **CORS** | Cross-Origin Resource Sharing | ✅ | Configurável |
| **Correlation IDs** | Rastreamento distribuído | ✅ | `X-Correlation-ID` |

### REST Constraints (Roy Fielding)

| Constraint | Status |
|-----------|--------|
| Client-Server | ✅ Separação de responsabilidades |
| Stateless | ✅ Sem sessão, requisições auto-contidas |
| Cacheable | ✅ Headers de cache (`ETag`, `Cache-Control`) |
| Layered System | ✅ Load Balancer → Gateway → API → DB |
| Uniform Interface | ✅ URIs padronizadas, HATEOAS |
| Code on Demand | ⚠️ Opcional (não implementado) |

**Conformidade REST:** 95% (16/17 requisitos implementados)

---

## 📁 Estrutura do Projeto

```
API.Template/
│
├── 📂 API/                          # Presentation Layer
│   ├── Controllers/v1, v2/             # Endpoints versionados
│   ├── Middlewares/                    # Error, Logging, Security
│   ├── Configurations/                 # DI, Swagger, CORS
│   ├── Program.cs                      # Entry point, Startup
│   ├── appsettings.json                # Configurações de produção
│   ├── appsettings.Development.json    # Configurações de desenvolvimento
│
├── 📂 Application/                  # Use Cases
│   ├── Services/                       # Lógica de negócio
│   ├── Interfaces/                     # Contratos (IOrderService, IHealthCheck)
│   ├── EventHandlers/                  # Handlers de Domain Events
│   ├── DTO/                            # Request/Response DTOs
│   └── Settings/                       # Configurações tipadas
│
├── 📂 Domain/                       # Core Business
│   ├── Entities/                       # Order, Game (Aggregates)
│   ├── ValueObjects/                   # PaymentMethodDetails
│   ├── Events/                         # OrderCreatedEvent
│   ├── Enums/                          # OrderStatus, PaymentMethod
│   └── Repositories/                   # IOrderRepository (Interface)
│
├── 📂 Infrastructure/               # External Concerns
│   ├── Context/                        # EF Core DbContext
│   ├── Repositories/                   # OrderRepository (Implementação)
│   ├── Services/                       # RabbitMQ, Logging, Health Checks
│   ├── HttpClients/                    # GamesApiClient
│   ├── Factories/                      # MessagePublisherFactory
│   └── Migrations/                     # EF Core Migrations
│
├── 📂 Tests/                        # Tests Layer
│   ├── UnitTests/                      # Mocks, lógica isolada
│   ├── IntegrationTests/               # EF Core real, endpoints
│   ├── ArchitectureTests/              # NetArchTest (Clean Architecture)
│   └── LoadTests/                      # k6, load testing
│
├── 📂 docs/                         # Documentation
│   ├── SOLID_SUMMARY.md                # Análise SOLID detalhada
│   ├── Architecture.drawio             # Diagramas de arquitetura
│   └── README.md                        # Este arquivo
│
├── 📂 kubernetes/                   # Kubernetes manifests
├── 📂 .github/                      # GitHub workflows (CI/CD)
├── .gitignore                       # Arquivos ignorados pelo Git
├── Dockerfile                       # Imagem Docker da API
├── API.Template.sln                 # Solution .NET
```

---

## ✨ Funcionalidades Principais

### 🔹 CRUD de Orders
- Validações de negócio (duplicação, status, pagamento)
- Domain Events (OrderCreated, StatusChanged)
- Paginação com metadados e links HATEOAS

### 🔹 Observabilidade
- **Logging multi-destino:** Database, Elasticsearch, New Relic
- **Correlation IDs:** Rastreamento distribuído
- **Structured Logs:** JSON com contexto completo

### 🔹 Health Checks Dinâmicos
- **Auto-discovery** via `IEnumerable<IHealthCheck>`
- **Criticidade:** Database (503 se falhar) vs RabbitMQ (200 Degraded)
- **Extensível:** Adicione novo check sem modificar código existente

### 🔹 Mensageria
- **RabbitMQ** ou **Azure Service Bus**
- Publicação automática de eventos de domínio
- Factory Pattern para trocar provider

### 🔹 Segurança
- HTTPS enforcement
- JWT Bearer Authentication
- Security Headers (HSTS, CSP, X-Frame-Options)

---

## 🎯 Princípios SOLID

### Resumo

| Princípio | Aplicação |
|-----------|-----------|
| **S** - Single Responsibility | Cada classe tem 1 responsabilidade (OrderService, OrderRepository) |
| **O** - Open/Closed | Extensível sem modificar (IHealthCheck → RedisHealthCheck) |
| **L** - Liskov Substitution | ILoggerService → Database/Elastic/NewRelic substituíveis |
| **I** - Interface Segregation | Interfaces coesas (IOrderRepository, IHealthCheck) |
| **D** - Dependency Inversion | Depende de abstrações, não implementações |

### Exemplos Práticos

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

📚 **Documentação completa:** `docs/SOLID_PRINCIPLES_SUMMARY.md`

---

## 🛠️ Tecnologias

**Core:** .NET 8, C# 12, ASP.NET Core 8  
**Persistência:** EF Core 8, SQL Server  
**Messaging:** RabbitMQ, Azure Service Bus  
**Logging:** Serilog (Elasticsearch, File, Console)  
**Testes:** xUnit, Moq, FluentAssertions, NetArchTest, k6  
**Documentação:** Swagger/OpenAPI 3.0  

---

## 🚀 CI/CD

### Pipeline Automatizado

A aplicação possui pipelines de CI/CD completos para automação de build, testes e deploy.

---

## 🚀 Setup Rápido

```bash
# 1. Clonar
git clone https://github.com/fkwesley/API.Template.git
cd API.Template

# 2. Restaurar dependências
dotnet restore

# 3. Configurar banco (appsettings.json)
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=OrdersDB;..."
}

# 4. Aplicar migrations
dotnet ef database update --project Infrastructure --startup-project API

# 5. Executar
cd API
dotnet run

# ✅ API disponível em: https://localhost:5001/swagger
```

---

## 🧪 Testes

### Pirâmide de Testes
```
      /\
     /E2E\        ← 5% (críticos)
    /------\
   / Integr \     ← 20% (DB real)
  /----------\
 /Unit Tests  \   ← 70% (mocks)
/______________\
  + Architecture  ← 5% (regras)
```

### Executar
```bash
dotnet test                                    # Todos
dotnet test --filter "Category=Unit"          # Unitários
dotnet test --filter "Category=Integration"   # Integração
k6 run load-tests/orders-load-test.js         # Carga
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
