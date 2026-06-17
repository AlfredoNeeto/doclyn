# DASHBOARD_SUMMARY_ENDPOINT_TASK.md

# Task — Implementar Endpoint de Dashboard com Indicadores do Doclyn

## Contexto

O Doclyn possui funcionalidades de:

* Upload de documentos
* Processamento assíncrono
* OCR
* Classificação documental
* Extração de indexadores
* Confiança por campo
* Insights documentais
* Reprocessamento
* Classes documentais
* Sugestões de aprendizado

Agora é necessário criar um endpoint consolidado para alimentar o dashboard do front-end.

---

# Objetivo

Criar um endpoint que forneça informações importantes para a tela inicial do sistema.

O dashboard deve permitir que o usuário entenda rapidamente:

```txt
Quantos documentos foram enviados
Quantos foram processados
Quantos estão pendentes
Quantos falharam
Quantos campos precisam revisão
Quais insights críticos existem
Quais documentos foram processados recentemente
Quais classes documentais são mais utilizadas
```

---

# Endpoint Principal

```http
GET /api/dashboard/summary
```

Endpoint protegido por JWT.

---

# Regras de Acesso

## Operator

Usuário comum deve visualizar apenas dados dos próprios documentos.

```txt
WHERE DOCUMENTS.USER_ID = currentUserId
```

## Admin

Admin pode visualizar dados globais do sistema.

```txt
Todos os documentos
```

---

# Response Esperada

```json
{
  "documents": {
    "total": 128,
    "pending": 12,
    "processing": 4,
    "processed": 105,
    "failed": 7
  },
  "quality": {
    "averageConfidence": 0.91,
    "fieldsValidated": 842,
    "fieldsNeedsReview": 38,
    "fieldsRejected": 9
  },
  "insights": {
    "total": 46,
    "critical": 3,
    "warning": 18,
    "info": 25
  },
  "classes": {
    "total": 8,
    "mostUsed": [
      {
        "id": "guid",
        "name": "RELATORIO_TECNICO_PRELIMINAR",
        "displayName": "Relatório Técnico Preliminar",
        "documentsCount": 42
      }
    ]
  },
  "recentDocuments": [
    {
      "id": "guid",
      "fileName": "contrato_2026.pdf",
      "documentStatus": "Processed",
      "documentClass": "Contrato Administrativo",
      "averageConfidence": 0.94,
      "insightsCount": 2,
      "needsReviewCount": 1,
      "createdAt": "2026-06-17T12:00:00Z"
    }
  ],
  "attentionRequired": [
    {
      "documentId": "guid",
      "fileName": "contrato_vencido.pdf",
      "reason": "Contrato vencido",
      "severity": "Warning",
      "createdAt": "2026-06-17T12:00:00Z"
    }
  ]
}
```

---

# Indicadores do Dashboard

## Documentos

Calcular:

```txt
total
pending
processing
processed
failed
```

Base:

```txt
DOCUMENTS
```

---

## Qualidade da Extração

Calcular:

```txt
averageConfidence
fieldsValidated
fieldsNeedsReview
fieldsRejected
```

Base:

```txt
EXTRACTED_DATA.DATA_JSON
```

Caso os campos estejam somente em JSONB, implementar leitura e agregação inicial em aplicação.

Se isso ficar custoso futuramente, criar tabela normalizada para campos extraídos.

---

## Insights

Calcular:

```txt
total
critical
warning
info
success
```

Base:

```txt
DOCUMENT_INSIGHTS
```

---

## Classes Documentais

Calcular:

```txt
total de classes ativas
classes mais utilizadas
```

Base:

```txt
DOCUMENT_CLASSES
DOCUMENTS
```

---

## Documentos Recentes

Retornar os últimos documentos enviados/processados.

Quantidade sugerida:

```txt
5
```

Campos:

```txt
id
fileName
documentStatus
documentClass
averageConfidence
insightsCount
needsReviewCount
createdAt
```

---

## Atenção Necessária

Retornar itens que exigem ação do usuário.

Exemplos:

```txt
Documentos com erro
Documentos com insights críticos
Documentos com campos para revisão
Documentos com contrato vencido
Documentos com campo obrigatório ausente
```

Quantidade sugerida:

```txt
5
```

---

# Estrutura Recomendada

## Application

```txt
Doclyn.Application/
  Dashboard/
    GetSummary/
      GetDashboardSummaryQuery.cs
      GetDashboardSummaryHandler.cs
      DashboardSummaryResponse.cs
      DocumentsSummaryResponse.cs
      QualitySummaryResponse.cs
      InsightsSummaryResponse.cs
      ClassesSummaryResponse.cs
      RecentDocumentResponse.cs
      AttentionRequiredResponse.cs
```

---

## Api

```txt
Doclyn.Api/
  Controllers/
    DashboardController.cs
```

---

# Handler

O handler deve:

```txt
Identificar usuário atual
Verificar role
Aplicar filtro por usuário se for Operator
Consultar agregações
Montar response final
Retornar DTO
```

---

# Performance

Evitar carregar documentos completos quando possível.

Preferir consultas agregadas.

Usar:

```txt
CountAsync
GroupBy
Select
Take
OrderByDescending
```

Não retornar JSONB completo no dashboard.

---

# Cache

Para o MVP, não implementar cache.

Futuro:

```txt
Cache de 30 a 60 segundos por usuário
```

---

# Segurança

Não retornar:

```txt
StoragePath
Dados sensíveis completos
JSON bruto
Tokens
Informações internas do MinIO
```

O dashboard deve conter apenas informações resumidas.

---

# Logs

Registrar:

```txt
DashboardSummaryRequested
DashboardSummaryGenerated
DashboardSummaryFailed
```

---

# Endpoint Opcional — Métricas por Período

Criar futuramente:

```http
GET /api/dashboard/metrics?from=2026-06-01&to=2026-06-30
```

Não implementar nesta task, apenas deixar planejado.

---

# Testes Unitários

Criar testes para:

* Operator visualiza apenas seus dados.
* Admin visualiza dados globais.
* Contagem por status está correta.
* RecentDocuments retorna quantidade limitada.
* AttentionRequired prioriza itens importantes.

---

# Testes de Integração

Criar testes para:

* Endpoint exige autenticação.
* Operator não vê documentos de outro usuário.
* Admin vê dados globais.
* Response possui todos os blocos esperados.
* Endpoint não retorna dados sensíveis.

---

# Critérios de Aceite

A task será considerada concluída quando:

* Endpoint `GET /api/dashboard/summary` existir.
* Endpoint exigir JWT.
* Operator visualizar apenas seus dados.
* Admin visualizar dados globais.
* Contadores de documentos forem retornados.
* Indicadores de qualidade forem retornados.
* Insights forem resumidos.
* Classes mais usadas forem retornadas.
* Documentos recentes forem retornados.
* Itens que exigem atenção forem retornados.
* Nenhum dado sensível for exposto.
* Testes principais forem implementados.
* Código respeitar Clean Architecture.

---

# Resultado Esperado

Ao final desta task, o front-end terá um endpoint consolidado para montar a tela inicial do Doclyn.

O usuário poderá abrir o dashboard e entender rapidamente:

```txt
O que foi enviado
O que foi processado
O que falhou
O que precisa revisão
Quais documentos exigem atenção
Quais classes documentais estão sendo usadas
```

Isso melhora a experiência do usuário e torna o sistema mais apresentável como produto SaaS.
