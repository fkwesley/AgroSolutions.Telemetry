# AgroSolutions.Telemetry API

API para recebimento e processamento de dados de telemetria de sensores térmicos de campo agrícola.

## 🌾 Funcionalidades

- **Recepção de Telemetria**: Recebe dados de sensores (umidade do solo, temperatura do ar, precipitação)
- **Armazenamento CosmosDB**: Persiste dados em banco NoSQL otimizado para IoT
- **Alertas Inteligentes**: Detecta condições de seca prolongada (umidade < 30% por 24h+)
- **Integração Service Bus**: Envia alertas para fila do Azure Service Bus

## 🏗️ Arquitetura

### Camadas

```
API (Controllers)
  ↓
Application (Services, DTOs, Event Handlers)
  ↓
Domain (Entities, Events, Repository Interfaces)
  ↓
Infrastructure (CosmosDB Repository, Service Bus Publisher)
```

### Padrões Utilizados

- **Clean Architecture**: Separação clara de responsabilidades
- **SOLID Principles**: Código manutenível e testável
- **Domain Events**: Processamento assíncrono de regras de negócio
- **Repository Pattern**: Abstração de acesso a dados

## 📊 Modelo de Dados

### FieldMeasurement (Entidade de Domínio)

```csharp
{
  "id": "guid",
  "fieldId": "guid",           // Identificador do campo
  "soilMoisture": 0-100,       // Umidade do solo em %
  "airTemperature": -50-80,    // Temperatura do ar em °C
  "precipitation": 0+,         // Precipitação em mm
  "collectedAt": "datetime",   // Data/hora da coleta pelo sensor
  "receivedAt": "datetime",    // Data/hora do recebimento pela API
  "userId": "string"           // Identificador do usuário que criou a medição (do token JWT)
}
```

## ☁️ CosmosDB - Conexão e Melhores Práticas

### Por que CosmosDB?

1. **NoSQL distribuído**: Ideal para dados de telemetria (alta volumetria)
2. **Baixa latência**: Leituras/escritas rápidas globalmente
3. **Escalabilidade automática**: Cresce conforme demanda
4. **Suporte a APIs múltiplas**: SQL, MongoDB, Cassandra, Gremlin

### Configuração da Conexão

#### 1. Connection String

No `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CosmosDbConnection": "AccountEndpoint=https://seu-account.documents.azure.com:443/;AccountKey=sua-chave;"
  },
  "CosmosDb": {
    "DatabaseId": "AgroSolutionsDb"
  }
}
```

#### 2. Estrutura no CosmosDB

- **Database**: `AgroSolutionsDb`
- **Container**: `field-measurements`
- **Partition Key**: `/fieldId` (distribui dados por campo)
- **Throughput**: 400 RU/s (pode ser ajustado)

### Melhores Práticas Implementadas

#### ✅ 1. Partition Key Estratégica

```csharp
partitionKeyPath: "/fieldId"
```

**Por quê?** 
- Queries por campo específico são ultra-rápidas
- Distribuição uniforme de dados
- Cada campo agrícola = uma partição lógica

#### ✅ 2. Lazy Initialization

```csharp
private async Task EnsureContainerAsync()
{
    if (_container != null) return;
    // Inicializa apenas quando necessário
}
```

**Benefícios:**
- API não falha no startup se CosmosDB estiver offline
- Conexão criada sob demanda
- Thread-safe com SemaphoreSlim

#### ✅ 3. Queries Otimizadas

**Com Partition Key (eficiente):**
```csharp
var iterator = _container.GetItemQueryIterator<FieldMeasurement>(
    query,
    requestOptions: new QueryRequestOptions
    {
        PartitionKey = new PartitionKey(fieldId.ToString())
    });
```

**Sem Partition Key (cross-partition):**
```csharp
// Usado apenas quando realmente necessário (ex: listagem geral)
var query = new QueryDefinition("SELECT * FROM c ORDER BY c.receivedAt DESC");
```

#### ✅ 4. Retry Policy Automático

```csharp
new CosmosClient(connectionString, new CosmosClientOptions
{
    MaxRetryAttemptsOnRateLimitedRequests = 9,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
});
```

#### ✅ 5. Monitoramento de RU (Request Units)

```csharp
_logger.LogInformation(
    "Measurement saved. RU consumed: {RU}",
    response.RequestCharge);
```

### Entity Framework vs CosmosDB SDK

**Por que NÃO usar EF Core para CosmosDB?**

❌ **Entity Framework Core (Provider CosmosDB)**
- Abstração pesada para NoSQL
- Migrations não fazem sentido (schema-less)
- Performance inferior
- Limitações em queries complexas

✅ **CosmosDB SDK Direto** (Implementado)
- Performance otimizada
- Controle total sobre RUs
- Queries SQL nativas
- Suporte completo a recursos do CosmosDB

## 🚨 Sistema de Alertas

### Regra de Negócio

**Condição de Seca:**
- Umidade do solo < 30%
- Persistindo por mais de 24 horas
- Baseado em timestamp `collectedAt`

### Fluxo de Alerta

1. **Nova medição recebida** → `AddMeasurementAsync()`
2. **Verificação automática** → `CheckDroughtConditionsAsync()`
3. **Consulta histórico 24h** → `GetByFieldIdAndDateRangeAsync()`
4. **Todas medições < 30%?** → Sim
5. **Dispara evento** → `DroughtAlertRequiredEvent`
6. **Event Handler** → `DroughtAlertRequiredEventHandler`
7. **Publica no Service Bus** → Fila `alert-required-queue`

### Mensagem de Alerta

```json
{
  "alertType": "DroughtCondition",
  "fieldId": "guid",
  "currentSoilMoisture": 25.5,
  "firstLowMoistureDetected": "2025-01-15T10:00:00Z",
  "detectedAt": "2025-01-16T11:30:00Z",
  "severity": "High",
  "message": "Alerta de Seca: Campo xxx com umidade abaixo de 30% por mais de 24 horas."
}
```

## 🔧 Configuração Inicial

### 1. Restaurar Pacotes

```bash
dotnet restore
```

### 2. Configurar CosmosDB

1. Criar conta CosmosDB no Azure Portal
2. Copiar Connection String
3. Atualizar `appsettings.json`

### 3. Configurar Service Bus

1. Criar namespace do Service Bus
2. Criar fila `alert-required-queue`
3. Copiar Connection String
4. Atualizar `appsettings.json`

### 4. Executar

```bash
dotnet run --project API
```

## 📡 Endpoints

### POST `/v1/field-measurements`

Adiciona nova medição de telemetria.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Request:**
```json
{
  "fieldId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "soilMoisture": 45.5,
  "airTemperature": 28.3,
  "precipitation": 12.7,
  "collectedAt": "2025-01-16T10:30:00Z"
}
```

**Observação:** O `userId` é extraído automaticamente do token JWT (claim `user_id`).

**Response:** `201 Created`
```json
{
  "id": "7b8c9d1e-2f3a-4b5c-6d7e-8f9a0b1c2d3e",
  "fieldId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "soilMoisture": 45.5,
  "airTemperature": 28.3,
  "precipitation": 12.7,
  "collectedAt": "2025-01-16T10:30:00Z",
  "receivedAt": "2025-01-16T10:31:05Z",
  "userId": "user-abc-123"
}
```

### GET `/v1/field-measurements/{id}`

Retorna medição específica.

### GET `/v1/field-measurements/field/{fieldId}`

Retorna todas medições de um campo.

### GET `/v1/field-measurements?page=1&pageSize=10`

Retorna medições paginadas.

## 🧪 Testes

### Executar Testes Unitários

```bash
dotnet test
```

### Cobertura

- ✅ Testes de entidade (validações de domínio)
- ✅ Testes de mapeamento (DTOs)
- ✅ Testes de serviço (regras de negócio)
- ✅ Testes de integração (repositório)

## 🔐 Segurança

- **Autenticação**: JWT Bearer Token
- **Autorização**: `[Authorize]` em todos os endpoints
- **Validação**: DTOs com DataAnnotations
- **Exceções tratadas**: Middleware global de erro

## 📈 Monitoramento

### Logs Estruturados (Serilog -> Elasticsearch)

Todos os logs da aplicação são enviados automaticamente para **Elasticsearch** via Serilog.

**Configuração:**
```json
{
  "LoggerSettings": {
    "Provider": "Elastic",
    "ServiceName": "agrosolutions-telemetry-api"
  },
  "ElasticLogs": {
    "Endpoint": "https://your-elastic-cloud.elastic-cloud.com",
    "ApiKey": "your-api-key",
    "IndexPrefix": "agro"
  }
}
```

**Logs Capturados:**
- 📊 Requisições HTTP (método, path, status, duração)
- 🔍 Performance de queries CosmosDB (RU consumption)
- ⚠️ Alertas de seca detectados
- ❌ Erros e exceções com stack trace
- 👤 Informações de usuário (do token JWT)

**Exemplo de Log:**
```csharp
_logger.LogInformation(
    "Measurement {MeasurementId} saved for field {FieldId}. SoilMoisture: {SoilMoisture}%. RU consumed: {RU}",
    id, fieldId, moisture, ruConsumed);
```

**Consultar logs no Kibana:**
```
GET agro-logs-*/_search
{
  "query": {
    "bool": {
      "must": [
        { "match": { "serviceName": "agrosolutions-telemetry-api" } },
        { "range": { "@timestamp": { "gte": "now-24h" } } }
      ]
    }
  }
}
```

### Elastic APM

Rastreamento distribuído e monitoramento de performance configurado no `appsettings.json`:

```json
{
  "ElasticApm": {
    "Enabled": true,
    "ServerUrl": "https://your-apm.elastic-cloud.com:443",
    "SecretToken": "your-token",
    "ServiceName": "agrosolutions-telemetry-api",
    "Environment": "Production"
  }
}
```

**Métricas Capturadas:**
- ⏱️ Tempo de resposta de endpoints
- 🎯 Taxa de erro (4xx, 5xx)
- 📊 Throughput (requisições/segundo)
- 💾 Consumo de memória e CPU
- 🌐 Distributed tracing (correlação de requisições)

## 🚀 Próximos Passos

1. **Adicionar mais tipos de alerta**: Temperatura extrema, falta de precipitação
2. **Dashboard de visualização**: Grafana/PowerBI
3. **Machine Learning**: Previsão de necessidade de irrigação
4. **Cache**: Redis para queries frequentes
5. **CDC (Change Data Capture)**: CosmosDB Change Feed para processamento em tempo real

## 📚 Referências

- [Azure CosmosDB Best Practices](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/best-practice-dotnet)
- [Partition Key Design](https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview)
- [Service Bus Messaging](https://learn.microsoft.com/en-us/azure/service-bus-messaging/)

---

## 👨‍💻 Autor

**Frank Vieira** - [GitHub](https://github.com/fkwesley)
