namespace Application.Constants
{
    /// <summary>
    /// Mensagens padronizadas para alertas de campo.
    /// Todos os textos estão em português brasileiro (pt-BR).
    /// </summary>
    public static class AlertMessagesConstant
    {
        public static class ExcessiveRainfall
        {
            public const string SubjectTemplate = "⚠️ Alerta de Chuva Excessiva - Campo {0}";
            
            public static string GetBody(int fieldId, decimal precipitation, decimal threshold, DateTime detectedAt)
            {
                var excess = precipitation - threshold;
                var percentAbove = (excess / threshold * 100);
                
                return $@"CHUVA EXCESSIVA DETECTADA

Campo ID: {fieldId}
Precipitação: {precipitation:F1} mm
Limite: {threshold:F1} mm
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema monitora continuamente os níveis de precipitação em todos os campos. Quando uma nova medição é registrada, verifica se a precipitação excede o limite configurado.

MÉTRICAS ATUAIS:
- Precipitação atual: {precipitation:F1} mm
- Limite configurado: {threshold:F1} mm
- Excesso: {excess:F1} mm ({percentAbove:F1}% acima do limite)

POR QUE ISSO É IMPORTANTE:
Chuvas excessivas podem causar:
- Erosão do solo e lixiviação de nutrientes
- Encharcamento e deficiência de oxigênio nas zonas radiculares
- Aumento do risco de doenças fúngicas
- Danos às culturas e perda de produtividade
- Atraso nas operações de campo

AÇÕES RECOMENDADAS:
1. Inspecionar sistemas de drenagem para prevenir alagamento
2. Monitorar níveis de umidade do solo nas próximas 24-48 horas
3. Avaliar a saúde das culturas quanto a sinais de estresse ou doença
4. Adiar irrigação e fertilização até que a umidade do solo normalize
5. Considerar drenagem adicional se o alagamento persistir";
            }
        }

        public static class Drought
        {
            public const string SubjectTemplate = "🌵 Alerta de Condição de Seca - Campo {0}";
            
            public static string GetBody(int fieldId, decimal soilMoisture, decimal threshold, 
                DateTime firstLowMoistureDetected, double durationHours, int historyDays, 
                double minimumDurationHours, DateTime detectedAt)
            {
                var moistureDeficit = threshold - soilMoisture;
                var durationDays = durationHours / 24;
                
                return $@"CONDIÇÃO DE SECA DETECTADA

Campo ID: {fieldId}
Umidade do Solo Atual: {soilMoisture:F1}%
Duração da Seca: {durationHours:F1} horas
Primeira Baixa Umidade Detectada: {firstLowMoistureDetected:yyyy-MM-dd HH:mm:ss}
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema analisa dados de umidade do solo dos últimos {historyDays} dias para detectar períodos prolongados de baixos níveis de umidade. Uma condição de seca é identificada quando a umidade do solo permanece abaixo de {threshold:F1}% por pelo menos {minimumDurationHours} horas.

MÉTRICAS ATUAIS:
- Umidade do solo atual: {soilMoisture:F1}%
- Limite de seca: {threshold:F1}%
- Déficit de umidade: {moistureDeficit:F1}%
- Duração contínua da seca: {durationHours:F1} horas ({durationDays:F1} dias)

POR QUE ISSO É IMPORTANTE:
Condições prolongadas de seca podem causar:
- Estresse hídrico severo afetando crescimento e desenvolvimento das culturas
- Redução da fotossíntese e absorção de nutrientes
- Murchamento permanente e potencial perda da cultura
- Diminuição da qualidade e quantidade da produção
- Degradação do solo a longo prazo

AÇÕES RECOMENDADAS:
1. URGENTE: Programar irrigação imediata para restaurar umidade do solo
2. Calcular necessidades de água baseado no tipo de solo e necessidades da cultura
3. Monitorar indicadores de estresse da cultura (murchamento, enrolamento de folhas, mudanças de cor)
4. Ajustar programação de irrigação para prevenir recorrência
5. Considerar variedades de culturas resistentes à seca para próximas estações
6. Avaliar eficiência e cobertura do sistema de irrigação";
            }
        }

        public static class ExtremeHeat
        {
            public const string SubjectTemplate = "🔥 Alerta de Calor Extremo - Campo {0}";
            
            public static string GetBody(int fieldId, decimal airTemperature, decimal threshold, DateTime detectedAt)
            {
                var temperatureExcess = airTemperature - threshold;
                
                return $@"CALOR EXTREMO DETECTADO

Campo ID: {fieldId}
Temperatura do Ar: {airTemperature:F1}°C
Limite: {threshold:F1}°C
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema monitora as leituras de temperatura do ar dos sensores de campo. Quando uma medição excede o limite de calor extremo, um alerta imediato é acionado.

MÉTRICAS ATUAIS:
- Temperatura do ar atual: {airTemperature:F1}°C
- Limite de calor extremo: {threshold:F1}°C
- Excesso de temperatura: {temperatureExcess:F1}°C

POR QUE ISSO É IMPORTANTE:
Calor extremo pode causar:
- Estresse térmico nas culturas, reduzindo eficiência da fotossíntese
- Perda acelerada de água por evapotranspiração
- Desnaturação de proteínas e danos celulares nas plantas
- Redução da polinização e formação de frutos
- Aumento da susceptibilidade a pragas e doenças
- Preocupações com segurança dos trabalhadores durante operações de campo

AÇÕES RECOMENDADAS:
1. Aumentar frequência de irrigação para compensar maior evapotranspiração
2. Monitorar níveis de umidade do solo de perto
3. Considerar medidas de resfriamento emergencial se disponível (ex: nebulização, telas de sombra)
4. Inspecionar culturas quanto a sintomas de estresse térmico (murchamento, queima de folhas)
5. Reagendar trabalho de campo para horários mais frescos (início da manhã/final da tarde)
6. Garantir hidratação adequada para trabalhadores de campo
7. Aplicar medidas protetivas como cobertura morta para reduzir temperatura do solo";
            }
        }

        public static class FreezingTemperature
        {
            public const string SubjectTemplate = "❄️ Alerta de Temperatura de Congelamento - Campo {0}";
            
            public static string GetBody(int fieldId, decimal airTemperature, decimal threshold, DateTime detectedAt)
            {
                var temperatureBelowThreshold = threshold - airTemperature;
                
                return $@"TEMPERATURA DE CONGELAMENTO DETECTADA - RISCO DE GEADA

Campo ID: {fieldId}
Temperatura do Ar: {airTemperature:F1}°C
Limite: {threshold:F1}°C
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema monitora continuamente a temperatura do ar para detectar condições de congelamento. Quando a temperatura cai abaixo do limite de congelamento, um alerta imediato é acionado para prevenir danos por geada.

MÉTRICAS ATUAIS:
- Temperatura do ar atual: {airTemperature:F1}°C
- Limite de congelamento: {threshold:F1}°C
- Temperatura abaixo do limite: {temperatureBelowThreshold:F1}°C

POR QUE ISSO É IMPORTANTE:
Temperaturas de congelamento podem causar:
- Formação de cristais de gelo nas células das plantas, causando ruptura celular
- Danos permanentes a culturas sensíveis e plantas jovens
- Redução da produtividade e qualidade das culturas
- Perda total da cultura para espécies sensíveis à geada
- Danos à infraestrutura de irrigação
- Atraso no desenvolvimento e maturação das culturas

AÇÕES RECOMENDADAS:
1. URGENTE: Ativar medidas de proteção contra geada imediatamente se disponível
2. Considerar aquecimento emergencial ou ventiladores de vento para prevenir formação de geada
3. Aplicar irrigação por aspersão (se temperatura > -2°C) para proteger culturas
4. Cobrir culturas sensíveis com mantas térmicas ou coberturas de fileira
5. Monitorar temperatura continuamente durante toda a noite
6. Avaliar danos às culturas após temperatura subir acima de congelamento
7. Documentar áreas afetadas para reivindicações de seguro se necessário
8. Planejar medidas preventivas para futuros eventos de geada";
            }
        }

        public static class HeatStress
        {
            public const string SubjectTemplate = "🌡️ Alerta de Estresse Térmico - Campo {0} ({1})";
            
            public static string GetBody(int fieldId, string stressLevel, decimal durationHours, 
                decimal averageTemperature, decimal peakTemperature, int historyHours, 
                decimal criticalTemperature, double minimumDurationHours, DateTime detectedAt)
            {
                return $@"CONDIÇÃO DE ESTRESSE TÉRMICO DETECTADA

Campo ID: {fieldId}
Nível de Estresse: {stressLevel}
Duração: {durationHours:F1} horas
Temperatura Média: {averageTemperature:F1}°C
Temperatura Pico: {peakTemperature:F1}°C
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema analisa dados de temperatura das últimas {historyHours} horas para detectar períodos prolongados de alta temperatura que causam estresse térmico nas culturas. Estresse térmico é identificado quando temperaturas excedem {criticalTemperature:F1}°C por pelo menos {minimumDurationHours} horas.

MÉTRICAS ATUAIS:
- Nível de estresse: {stressLevel}
- Duração contínua de alta temperatura: {durationHours:F1} horas
- Temperatura média durante período de estresse: {averageTemperature:F1}°C
- Temperatura pico registrada: {peakTemperature:F1}°C
- Limite de temperatura crítica: {criticalTemperature:F1}°C

POR QUE ISSO É IMPORTANTE:
Estresse térmico prolongado pode resultar em:
- Redução da fotossíntese e taxas de crescimento
- Floração prematura ou queda de frutos
- Diminuição da viabilidade do pólen afetando polinização
- Aumento das taxas de respiração, consumindo energia armazenada
- Danos a proteínas e desativação de enzimas
- Redução da produtividade e qualidade das culturas
- Aumento do consumo de água e custos de irrigação

INTERPRETAÇÃO DO NÍVEL DE ESTRESSE:
- Moderado: Culturas estão sob estresse mas podem se recuperar com cuidado apropriado
- Severo: Nível crítico de estresse requerendo intervenção imediata para prevenir danos permanentes

AÇÕES RECOMENDADAS:
1. Aumentar frequência e volume de irrigação para resfriar plantas e solo
2. Monitorar umidade do solo para prevenir estresse hídrico adicional
3. Considerar medidas de resfriamento emergencial (sistemas de nebulização, estruturas de sombra)
4. Inspecionar culturas quanto a sintomas de danos por calor (enrolamento de folhas, murchamento, queimadura)
5. Aplicar sprays foliares com compostos redutores de estresse se apropriado
6. Ajustar programação de colheita se culturas estiverem próximas da maturidade
7. Planejar estratégias de mitigação de calor a longo prazo (seleção de culturas, sombreamento, manejo de microclima)";
            }
        }

        public static class PestRisk
        {
            public const string SubjectTemplate = "🐛 Alerta de Risco de Pragas - Campo {0} (Risco {1})";
            
            public static string GetBody(int fieldId, string riskLevel, int favorableDaysCount, 
                decimal averageTemperature, decimal averageMoisture, string riskFactors, 
                int historyDays, decimal minTemperature, decimal maxTemperature, 
                decimal minMoisture, int minimumFavorableDays, DateTime detectedAt)
            {
                var riskFactorsText = riskFactors;
                
                return $@"CONDIÇÃO DE RISCO DE PRAGAS DETECTADA

Campo ID: {fieldId}
Nível de Risco: {riskLevel}
Dias Consecutivos Favoráveis: {favorableDaysCount}
Temperatura Média: {averageTemperature:F1}°C
Umidade do Solo Média: {averageMoisture:F1}%
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema analisa dados ambientais dos últimos {historyDays} dias para identificar condições favoráveis ao desenvolvimento e proliferação de pragas. O risco de pragas é avaliado com base na faixa de temperatura ({minTemperature}°C - {maxTemperature}°C) e umidade mínima do solo ({minMoisture}%) sustentadas por dias consecutivos.

MÉTRICAS ATUAIS:
- Nível de risco: {riskLevel}
- Dias consecutivos com condições favoráveis: {favorableDaysCount}
- Temperatura média durante o período: {averageTemperature:F1}°C
- Umidade do solo média durante o período: {averageMoisture:F1}%
- Faixa de temperatura favorável: {minTemperature}°C - {maxTemperature}°C
- Umidade mínima favorável: {minMoisture}%

FATORES DE RISCO IDENTIFICADOS:
{riskFactorsText}

POR QUE ISSO É IMPORTANTE:
Condições favoráveis para pragas podem levar a:
- Rápido crescimento populacional de pragas e infestações
- Danos às culturas através de alimentação, perfuração ou transmissão de doenças
- Redução da produtividade e qualidade das culturas
- Aumento da necessidade de intervenções de controle de pragas
- Perdas econômicas por culturas danificadas
- Potencial disseminação para campos vizinhos

INTERPRETAÇÃO DO NÍVEL DE RISCO:
- Médio: Condições estão se tornando favoráveis; monitoramento preventivo recomendado
- Alto: Condições altamente favoráveis; ação imediata necessária para prevenir infestação

AÇÕES RECOMENDADAS:
1. Conduzir vistoria imediata de campo para avaliar presença atual de pragas
2. Configurar armadilhas de monitoramento de pragas em locais estratégicos
3. Identificar espécies específicas de pragas propensas a estarem ativas nas condições atuais
4. Considerar medidas preventivas de controle de pragas baseadas nos resultados da vistoria
5. Revisar e atualizar protocolos de manejo integrado de pragas (MIP)
6. Monitorar condições de campo diariamente quanto a mudanças na pressão de pragas
7. Coordenar com agrônomo para estratégias de intervenção direcionadas
8. Documentar atividade de pragas para planejamento futuro de prevenção";
            }
        }

        public static class Irrigation
        {
            public const string SubjectTemplate = "💧 Recomendação de Irrigação - Campo {0} (Urgência {1})";
            
            public static string GetBody(int fieldId, decimal currentMoisture, decimal optimalMoisture, 
                string urgency, decimal waterAmountMM, double estimatedDurationMinutes, 
                int historyDays, decimal criticalMoisture, decimal soilWaterCapacity, DateTime detectedAt)
            {
                var moistureDeficit = optimalMoisture - currentMoisture;
                
                return $@"RECOMENDAÇÃO DE IRRIGAÇÃO

Campo ID: {fieldId}
Nível de Urgência: {urgency}
Umidade do Solo Atual: {currentMoisture:F1}%
Umidade Alvo: {optimalMoisture:F1}%
Quantidade de Água Necessária: {waterAmountMM:F1} mm
Duração Estimada: {estimatedDurationMinutes:F0} minutos
Detectado em: {detectedAt:yyyy-MM-dd HH:mm:ss} UTC

O QUE FOI AVALIADO:
O sistema analisa dados de umidade do solo dos últimos {historyDays} dias, comparando níveis atuais com limites ótimos e críticos. Recomendações de irrigação são calculadas com base na capacidade de água do solo, déficit de umidade atual e requisitos de água da cultura.

MÉTRICAS ATUAIS:
- Umidade do solo atual: {currentMoisture:F1}%
- Umidade ótima alvo: {optimalMoisture:F1}%
- Limite de umidade crítica: {criticalMoisture:F1}%
- Déficit de umidade: {moistureDeficit:F1}%
- Capacidade de água do solo: {soilWaterCapacity} mm

REQUISITOS DE IRRIGAÇÃO:
- Quantidade de água: {waterAmountMM:F1} mm
- Tempo estimado de irrigação: {estimatedDurationMinutes:F0} minutos
- Nível de urgência: {urgency}

POR QUE ISSO É IMPORTANTE:
Temporização adequada de irrigação é crítica para:
- Manter crescimento e desenvolvimento ótimo das culturas
- Prevenir estresse hídrico que reduz produtividade
- Gestão eficiente de recursos hídricos
- Evitar sobre-irrigação e desperdício de água
- Manter estrutura do solo e prevenir erosão
- Otimizar disponibilidade e absorção de nutrientes
- Prevenir condições de doenças por excesso de umidade

INTERPRETAÇÃO DO NÍVEL DE URGÊNCIA:
- Baixa: Irrigação preventiva para manter condições ótimas
- Média: Umidade do solo está abaixo do ótimo; irrigação recomendada dentro de 24-48 horas
- Crítica: Umidade do solo está criticamente baixa; irrigação imediata necessária para prevenir estresse da cultura

AÇÕES RECOMENDADAS:
1. {(urgency == "Critical" ? "URGENTE: Iniciar irrigação imediatamente" : "Programar irrigação dentro do prazo recomendado")}
2. Aplicar aproximadamente {waterAmountMM:F1} mm de água (~{estimatedDurationMinutes:F0} minutos em taxa de fluxo típica)
3. Monitorar umidade do solo durante e após irrigação para garantir que o alvo seja atingido
4. Verificar cobertura e eficiência do sistema de irrigação antes de iniciar
5. Ajustar duração de irrigação baseado no tipo de solo e taxa de infiltração
6. Verificar previsão do tempo para evitar irrigar antes de chuva esperada
7. Registrar aplicação de irrigação para registros de manejo de cultura
8. Continuar monitoramento para otimizar programação futura de irrigação";
            }
        }
    }
}
