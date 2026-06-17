# DOCUMENT_INSIGHTS_TASK.md

# Task — Implementar Geração de Insights Documentais Após o Processamento

## Contexto

O Doclyn já possui ou está evoluindo para possuir:

- Upload de documentos PDF
- Storage em MinIO
- Processamento assíncrono
- OCR
- Extração de texto
- Classificação semântica com IA
- Catálogo de classes documentais
- Catálogo de indexadores por classe
- Extração orientada por classe
- Confiança por campo
- Persistência em JSONB
- Logs de processamento
- Reprocessamento

Agora será implementada uma camada de **insights documentais**.

Essa camada será responsável por transformar dados extraídos em informações úteis para o usuário.

Exemplo:

```txt
Data de fim de vigência extraída
↓
Sistema compara com a data atual
↓
Insight gerado:
Contrato vencido
```

---

# Objetivo

Criar uma etapa no pipeline para gerar insights relevantes após a extração dos indexadores.

O sistema deve deixar de apenas extrair campos e passar a interpretar informações importantes do documento.

Objetivo prático:

```txt
Documento enviado
↓
OCR / texto
↓
Classificação
↓
Extração de indexadores
↓
Confiança por campo
↓
Geração de insights
↓
Resumo inteligente
↓
Persistência
↓
Exibição no front-end
```

---

# Conceito

## Extração

A extração responde:

```txt
Quais dados existem no documento?
```

Exemplo:

```json
{
  "dataFimVigencia": "2026-06-16",
  "valorContrato": "15000.00",
  "contratada": "Empresa X"
}
```

## Insight

O insight responde:

```txt
O que esses dados significam?
```

Exemplo:

```json
{
  "type": "CONTRACT_EXPIRED",
  "severity": "Warning",
  "message": "O contrato encontra-se vencido desde 16/06/2026."
}
```

---

# Regra Principal

A IA pode ajudar a gerar resumos e alertas semânticos, mas regras críticas devem ser determinísticas.

Exemplo:

```txt
Contrato vencido
```

Deve ser calculado pelo sistema:

```txt
dataFimVigencia < dataAtual
```

e não decidido exclusivamente pela IA.

Motivo:

- Mais segurança
- Mais previsibilidade
- Mais fácil de testar
- Melhor explicação para entrevista
- Menor risco de alucinação

---

# Tipos de Insight

Criar enum:

```csharp
public enum DocumentInsightType
{
    ContractExpired = 1,
    ContractExpiringSoon = 2,
    MissingRequiredField = 3,
    LowConfidenceField = 4,
    HighValueDocument = 5,
    InvalidIdentifier = 6,
    RiskMentioned = 7,
    PaymentSuspended = 8,
    LegalDeadlineMentioned = 9,
    ActionRequired = 10,
    Summary = 11,
    GenericObservation = 12
}
```

---

# Severidade

Criar enum:

```csharp
public enum DocumentInsightSeverity
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Critical = 4
}
```

---

# Nova Entidade

## DocumentInsight

Representa um insight gerado para um documento.

```csharp
public sealed class DocumentInsight
{
    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public DocumentInsightType Type { get; private set; }

    public DocumentInsightSeverity Severity { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public decimal Confidence { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public string? RelatedFieldName { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
```

---

# Banco de Dados

Criar tabela:

```sql
DOCUMENT_INSIGHTS
```

Colunas:

```sql
ID
DOCUMENT_ID
TYPE
SEVERITY
TITLE
MESSAGE
CONFIDENCE
SOURCE
RELATED_FIELD_NAME
CREATED_AT
```

Relacionamento:

```txt
DOCUMENTS 1:N DOCUMENT_INSIGHTS
```

Índices recomendados:

```sql
DOCUMENT_ID
TYPE
SEVERITY
CREATED_AT
```

---

# Estrutura JSONB

Além da tabela `DOCUMENT_INSIGHTS`, o resultado extraído pode manter uma cópia resumida no JSONB para facilitar leitura.

Exemplo:

```json
{
  "classification": {
    "documentClassId": "guid",
    "documentType": "CONTRATO_ADMINISTRATIVO"
  },
  "fields": {
    "dataFimVigencia": {
      "value": "2026-06-16",
      "confidence": 0.94,
      "source": "AI",
      "validationStatus": "Validated"
    }
  },
  "insights": [
    {
      "type": "CONTRACT_EXPIRED",
      "severity": "Warning",
      "title": "Contrato vencido",
      "message": "O contrato encontra-se vencido desde 16/06/2026.",
      "confidence": 0.94,
      "source": "Rule"
    }
  ]
}
```

---

# Fontes de Insight

Criar enum opcional:

```csharp
public enum DocumentInsightSource
{
    Rule = 1,
    AI = 2,
    Hybrid = 3
}
```

---

# Estratégia

## 1. Insights por regras determinísticas

Usar regras C# para casos objetivos.

Exemplos:

```txt
Contrato vencido
Contrato próximo do vencimento
Campo obrigatório ausente
Campo com baixa confiança
CPF/CNPJ inválido
Valor acima de limite configurado
```

## 2. Insights por IA

Usar IA para casos interpretativos.

Exemplos:

```txt
Resumo do documento
Riscos mencionados
Ações recomendadas
Assuntos principais
Possíveis pendências
Documentos complementares solicitados
```

## 3. Insights híbridos

Usar IA + regra.

Exemplo:

```txt
IA identifica que há suspensão de pagamento
↓
Regra cria alerta com severidade Warning
```

---

# Serviços

## Interface principal

Criar em Application:

```csharp
public interface IDocumentInsightService
{
    Task<IReadOnlyCollection<DocumentInsightResult>> GenerateAsync(
        Guid documentId,
        ExtractedDocumentData extractedData,
        CancellationToken cancellationToken = default);
}
```

## Resultado

```csharp
public sealed record DocumentInsightResult(
    DocumentInsightType Type,
    DocumentInsightSeverity Severity,
    string Title,
    string Message,
    decimal Confidence,
    DocumentInsightSource Source,
    string? RelatedFieldName);
```

## Serviços específicos

Criar serviços separados:

```csharp
public interface IRuleBasedInsightGenerator
{
    IReadOnlyCollection<DocumentInsightResult> Generate(ExtractedDocumentData extractedData);
}
```

```csharp
public interface IAiInsightGenerator
{
    Task<IReadOnlyCollection<DocumentInsightResult>> GenerateAsync(
        string documentText,
        ExtractedDocumentData extractedData,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IInsightMergeService
{
    IReadOnlyCollection<DocumentInsightResult> Merge(
        IReadOnlyCollection<DocumentInsightResult> ruleInsights,
        IReadOnlyCollection<DocumentInsightResult> aiInsights);
}
```

---

# Regras Determinísticas Iniciais

## Contrato vencido

Aplicar quando existir:

```txt
dataFimVigencia
```

Regra:

```txt
dataFimVigencia < DateTime.UtcNow.Date
```

Insight:

```json
{
  "type": "CONTRACT_EXPIRED",
  "severity": "Warning",
  "title": "Contrato vencido",
  "message": "O contrato encontra-se vencido desde {dataFimVigencia}.",
  "source": "Rule"
}
```

---

## Contrato próximo do vencimento

Aplicar quando:

```txt
dataFimVigencia >= hoje
dataFimVigencia <= hoje + 30 dias
```

Insight:

```json
{
  "type": "CONTRACT_EXPIRING_SOON",
  "severity": "Warning",
  "title": "Contrato próximo do vencimento",
  "message": "O contrato vence em {dias} dias.",
  "source": "Rule"
}
```

---

## Campo obrigatório ausente

Para cada `DocumentClassIndexer` obrigatório:

```txt
campo não extraído
```

Gerar:

```json
{
  "type": "MISSING_REQUIRED_FIELD",
  "severity": "Warning",
  "title": "Campo obrigatório ausente",
  "message": "O campo {campo} não foi encontrado no documento.",
  "relatedFieldName": "{campo}",
  "source": "Rule"
}
```

---

## Campo com baixa confiança

Quando:

```txt
confidence < 0.70
```

Gerar:

```json
{
  "type": "LOW_CONFIDENCE_FIELD",
  "severity": "Info",
  "title": "Campo com baixa confiança",
  "message": "O campo {campo} foi extraído com baixa confiança.",
  "relatedFieldName": "{campo}",
  "source": "Rule"
}
```

---

## CNPJ inválido

Quando o campo `cnpj` existir e falhar na validação.

Gerar:

```json
{
  "type": "INVALID_IDENTIFIER",
  "severity": "Critical",
  "title": "CNPJ inválido",
  "message": "O CNPJ identificado no documento não passou na validação.",
  "relatedFieldName": "cnpj",
  "source": "Rule"
}
```

---

## Valor alto

Configuração:

```json
{
  "Insights": {
    "HighValueThreshold": 50000
  }
}
```

Regra:

```txt
valor >= HighValueThreshold
```

Gerar:

```json
{
  "type": "HIGH_VALUE_DOCUMENT",
  "severity": "Info",
  "title": "Documento de alto valor",
  "message": "O documento menciona valor acima do limite configurado.",
  "source": "Rule"
}
```

---

# Insights por IA

A IA deve gerar:

```txt
Resumo executivo
Pontos de atenção
Riscos mencionados
Ações recomendadas
```

Prompt base:

```txt
Você é um analista documental.

Com base no texto do documento e nos campos extraídos,
gere insights úteis para o usuário.

Não invente informações.
Use apenas o conteúdo do documento.
Retorne apenas JSON válido.

Tipos permitidos:
- RiskMentioned
- PaymentSuspended
- LegalDeadlineMentioned
- ActionRequired
- Summary
- GenericObservation

Cada insight deve conter:
- type
- severity
- title
- message
- confidence
```

---

# Exemplo de insight por IA

```json
{
  "type": "PAYMENT_SUSPENDED",
  "severity": "Warning",
  "title": "Pagamento suspenso",
  "message": "O documento menciona suspensão cautelar do pagamento até esclarecimento das divergências.",
  "confidence": 0.91,
  "source": "AI"
}
```

---

# Integração com Pipeline

Fluxo atualizado:

```txt
Upload
↓
OCR / texto
↓
Classificação semântica
↓
Indexadores
↓
Extração orientada
↓
Confiança por campo
↓
Geração de insights
↓
Persistência
↓
Status PROCESSED
```

---

# Persistência

Após gerar insights:

```txt
1. Remover insights antigos do documento em caso de reprocessamento
2. Inserir novos insights
3. Atualizar JSONB com resumo dos insights
4. Registrar logs
```

---

# Logs

Criar logs:

```txt
InsightGenerationStarted
RuleInsightsGenerated
AiInsightsGenerated
InsightsMerged
InsightPersisted
InsightGenerationCompleted
InsightGenerationFailed
```

Não logar texto completo do documento.

---

# Endpoints

## Listar insights do documento

```http
GET /api/documents/{id}/insights
```

Response:

```json
[
  {
    "id": "guid",
    "type": "ContractExpired",
    "severity": "Warning",
    "title": "Contrato vencido",
    "message": "O contrato encontra-se vencido desde 16/06/2026.",
    "confidence": 0.94,
    "source": "Rule",
    "relatedFieldName": "dataFimVigencia",
    "createdAt": "2026-06-17T12:00:00Z"
  }
]
```

## Gerar insights novamente

```http
POST /api/documents/{id}/generate-insights
```

Regras:

```txt
Documento precisa estar processado
Operator só pode gerar insights dos seus documentos
Admin pode gerar insights de qualquer documento
```

---

# Front-end

Na tela de detalhes do documento, exibir seção:

```txt
Resumo inteligente
```

Exemplo:

```txt
⚠️ Contrato vencido
O contrato encontra-se vencido desde 16/06/2026.

⚠️ Pagamento suspenso
O documento menciona suspensão cautelar do pagamento até esclarecimento das divergências.

ℹ️ Campos para revisão
O campo assunto foi extraído com baixa confiança.
```

---

# UX recomendada

Usar cards com severidade:

```txt
Critical  → vermelho
Warning   → amarelo
Info      → azul/cinza
Success   → verde
```

Não usar linguagem técnica demais.

Preferir mensagens acionáveis.

Exemplo ruim:

```txt
Field validation status low confidence.
```

Exemplo bom:

```txt
O campo "Empresa" precisa de revisão.
```

---

# Estrutura Sugerida

## Domain

```txt
Entities/
  DocumentInsight.cs

Enums/
  DocumentInsightType.cs
  DocumentInsightSeverity.cs
  DocumentInsightSource.cs
```

## Application

```txt
DocumentInsights/
  Generate/
  GetByDocument/

Common/
  Interfaces/
    IDocumentInsightService.cs
    IRuleBasedInsightGenerator.cs
    IAiInsightGenerator.cs
    IInsightMergeService.cs
```

## Infrastructure

```txt
Insights/
  DocumentInsightService.cs
  RuleBasedInsightGenerator.cs
  AiInsightGenerator.cs
  InsightMergeService.cs
```

## Api

```txt
Controllers/
  DocumentInsightsController.cs
```

---

# Configurações

Adicionar:

```json
{
  "Insights": {
    "Enabled": true,
    "ContractExpiringSoonDays": 30,
    "LowConfidenceThreshold": 0.70,
    "HighValueThreshold": 50000,
    "EnableAiInsights": true
  }
}
```

---

# Testes Unitários

Criar testes para:

- Gerar insight de contrato vencido.
- Gerar insight de contrato próximo do vencimento.
- Gerar insight de campo obrigatório ausente.
- Gerar insight de campo com baixa confiança.
- Gerar insight de CNPJ inválido.
- Não gerar insight duplicado.
- Merge de insights por regra e IA.
- Reprocessamento remove insights antigos.

---

# Testes de Integração

Criar testes para:

- Persistir insights.
- Consultar insights do documento.
- Gerar insights novamente.
- Respeitar autorização.
- Atualizar JSONB com insights.

---

# Critérios de Aceite

A task será considerada concluída quando:

- A entidade `DocumentInsight` existir.
- As migrations forem criadas.
- O pipeline gerar insights após a extração.
- Insights por regra estiverem funcionando.
- Insights por IA estiverem funcionando de forma opcional.
- Insights forem persistidos.
- Insights antigos forem substituídos em reprocessamento.
- Endpoint de consulta existir.
- Endpoint de geração manual existir.
- Front-end conseguir consumir os insights.
- Logs estiverem implementados.
- Testes principais estiverem implementados.
- A solução respeitar Clean Architecture.

---

# Resultado Esperado

Ao final desta task, o Doclyn deixará de apenas extrair informações do documento e passará a entregar análise documental acionável.

O usuário não verá apenas campos como:

```txt
dataFimVigencia = 16/06/2026
```

Ele verá uma interpretação útil:

```txt
Contrato vencido
O contrato encontra-se vencido desde 16/06/2026.
```

Essa funcionalidade aproxima o Doclyn de uma plataforma real de inteligência documental, tornando o produto mais fácil de apresentar, mais útil para usuários não técnicos e mais forte em uma entrevista.
