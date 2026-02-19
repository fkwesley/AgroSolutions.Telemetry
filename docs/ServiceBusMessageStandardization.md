# Service Bus Message Standardization

## Overview

All field analysis event handlers now send alerts to Service Bus using a standardized message format defined by the `AlertMessage` class. **All email content is in Brazilian Portuguese (pt-BR)** and stored in the `AlertMessages` constants class for easy maintenance and consistency.

## Standard Message Structure

```csharp
public class AlertMessage
{
    public List<string> EmailTo { get; set; }      // Primary recipients
    public List<string> EmailCc { get; set; }      // Carbon copy recipients
    public List<string> EmailBcc { get; set; }     // Blind carbon copy recipients
    public string Subject { get; set; }            // Email subject line (pt-BR)
    public string Body { get; set; }               // Detailed email body (pt-BR)
    public AlertMetadata Metadata { get; set; }    // Alert tracking metadata
}
```

## Alert Messages Constants

All email texts are centralized in the `Application.Constants.AlertMessages` static class. This provides:

- **Consistency**: All alerts use the same format and language
- **Maintainability**: Easy to update texts in one place
- **Localization**: All content in pt-BR by default
- **Type Safety**: Compile-time checking of parameters

### Usage Example

```csharp
var alertMessage = new AlertMessage
{
    EmailTo = new List<string> { "alerts@agrosolutions.com" },
    EmailCc = new List<string>(),
    EmailBcc = new List<string>(),
    Subject = string.Format(AlertMessages.ExcessiveRainfall.SubjectTemplate, fieldId),
    Body = AlertMessages.ExcessiveRainfall.GetBody(
        fieldId,
        precipitation,
        threshold,
        DateTime.UtcNow),
    Metadata = new AlertMetadata
    {
        AlertType = "ExcessiveRainfall",
        FieldId = fieldId,
        DetectedAt = DateTime.UtcNow,
        Severity = "High"
    }
};
```

## Email Body Content Standard (in pt-BR)

Each alert's email body follows this structure:

### 1. **Cabeçalho do Alerta** (Alert Header)
- Tipo de alerta e indicador de severidade
- ID do campo
- Métricas principais (temperatura, umidade, precipitação, etc.)
- Timestamp de detecção

### 2. **O QUE FOI AVALIADO** (What Was Evaluated)
Explicação clara de:
- Quais dados foram analisados
- Quais condições foram verificadas
- Como o alerta foi acionado

### 3. **MÉTRICAS ATUAIS** (Current Metrics)
Detalhamento das:
- Valores atuais
- Valores de threshold
- Desvios do normal
- Duração das condições (se aplicável)

### 4. **POR QUE ISSO É IMPORTANTE** (Why This Is Important)
Explicação de:
- Impactos potenciais nas culturas
- Riscos agrícolas
- Implicações econômicas
- Considerações de segurança

### 5. **AÇÕES RECOMENDADAS** (Recommended Actions)
Lista priorizada de:
- Ações imediatas necessárias
- Medidas preventivas
- Recomendações de monitoramento
- Sugestões de planejamento de longo prazo

## Alert Types

| Alert Type | Handler | Severity | Trigger Condition |
|-----------|---------|----------|-------------------|
| ExcessiveRainfall | ExcessiveRainfallAnalysisEventHandler | High | Precipitation > threshold |
| DroughtCondition | DroughtAnalysisEventHandler | High | Low moisture for X hours |
| ExtremeHeat | ExtremeHeatAnalysisEventHandler | High | Temperature > threshold |
| FreezingTemperature | FreezingTemperatureAnalysisEventHandler | High | Temperature < freezing point |
| HeatStress | HeatStressAnalysisEventHandler | High/Medium | Prolonged high temperature |
| PestRisk | PestRiskAnalysisEventHandler | High/Medium | Favorable pest conditions |
| IrrigationRecommendation | IrrigationAnalysisEventHandler | High/Medium | Low soil moisture |

## Email Configuration

The email recipient is **dynamically configured per measurement** via the `AlertEmailTo` field in the request payload:

```json
{
  "fieldId": 123,
  "soilMoisture": 35.5,
  "airTemperature": 28.3,
  "precipitation": 10.2,
  "collectedAt": "2024-01-15T10:30:00Z",
  "alertEmailTo": "farmer@example.com"
}
```

When a measurement triggers an alert, the email is automatically sent to the address specified in `AlertEmailTo`. This allows different fields or customers to have alerts sent to different recipients.

### Email Field Customization

To customize additional recipients (Cc, Bcc) for specific alert types, update the respective event handler:

```csharp
EmailTo = new List<string> { measurement.AlertEmailTo },
EmailCc = new List<string> { "manager@agrosolutions.com" },
EmailBcc = new List<string> { "archive@agrosolutions.com" }
```

## Metadata Properties

```csharp
public class AlertMetadata
{
    public string AlertType { get; set; }      // Type identifier
    public int FieldId { get; set; }           // Field identifier
    public DateTime DetectedAt { get; set; }   // Detection timestamp
    public string Severity { get; set; }       // Low, Medium, High, Critical
}
```

## Service Bus Queue

All alerts are published to: `alert-required-queue`

## Consumer Expectations

The Service Bus consumer should:
1. Deserialize messages into `FieldAlertMessage` objects
2. Use `EmailTo`, `EmailCc`, `EmailBcc` for routing
3. Use `Subject` as email subject line
4. Use `Body` as email body (already formatted)
5. Use `Metadata` for logging, tracking, and categorization

## Benefits of Standardization

1. **Consistency**: All alerts follow the same format
2. **Maintainability**: Easy to update alert templates
3. **Extensibility**: Simple to add new alert types
4. **Consumer Simplicity**: Single message type to handle
5. **Rich Information**: Detailed context and recommendations
6. **Professional Communication**: Well-structured emails

## Implementation Example

```csharp
var alertMessage = new FieldAlertMessage
{
    EmailTo = new List<string> { "alerts@agrosolutions.com" },
    EmailCc = new List<string>(),
    EmailBcc = new List<string>(),
    Subject = $"🌡️ Heat Stress Alert - Field {fieldId}",
    Body = @"
HEAT STRESS CONDITION DETECTED

Field ID: 123
Temperature: 42.5°C
Duration: 8.5 hours

WHAT WAS EVALUATED:
Temperature data over the last 24 hours...

CURRENT METRICS:
- Peak temperature: 42.5°C
- Average: 38.2°C

WHY THIS IS IMPORTANT:
Heat stress can reduce crop yield...

RECOMMENDED ACTIONS:
1. Increase irrigation frequency
2. Monitor crops for stress symptoms
3. Apply cooling measures if available
",
    Metadata = new AlertMetadata
    {
        AlertType = "HeatStress",
        FieldId = 123,
        DetectedAt = DateTime.UtcNow,
        Severity = "High"
    }
};

await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);
```

## Future Enhancements

Consider implementing:
- Email template engine for better formatting
- HTML email support with styling
- Attachment support (charts, reports)
- Configurable email recipients per field or customer
- Alert suppression rules (avoid duplicate alerts)
- Alert escalation based on severity and response time
