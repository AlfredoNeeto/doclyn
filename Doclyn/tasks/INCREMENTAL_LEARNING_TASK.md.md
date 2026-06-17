# INCREMENTAL_LEARNING_TASK.md

# Task — Implementar Aprendizado Incremental para Evolução Automática das Classes Documentais

## Contexto

O Doclyn já possui:

* Upload de documentos
* OCR com Tesseract
* Extração de texto
* Classificação semântica
* Catálogo de Classes Documentais (`DocumentClass`)
* Catálogo de Indexadores (`DocumentClassIndexer`)
* Extração orientada por classe
* Confiança por campo
* Persistência em JSONB
* Logs de processamento
* Reprocessamento

Atualmente o sistema consegue:

```txt
Classificar documentos
↓
Extrair indexadores conhecidos
↓
Persistir resultado
```

Entretanto, o conhecimento documental é estático.

Quando a IA identifica novas informações relevantes em documentos da mesma classe, o sistema não aprende com isso.

---

# Objetivo

Implementar um mecanismo de aprendizado incremental supervisionado.

Fluxo:

```txt
Documento processado
↓
IA identifica possíveis novos indexadores
↓
Sistema registra sugestões
↓
Usuário aprova ou rejeita
↓
Catálogo evolui
↓
Próximos documentos utilizam os novos indexadores
```

Objetivo final:

```txt
O Doclyn melhora continuamente
sem necessidade de alterar código.
```

---

# Conceito

Hoje:

```txt
Documento
↓
IA encontra novo campo
↓
Campo ignorado
```

Após esta implementação:

```txt
Documento
↓
IA encontra novo campo
↓
Sistema cria sugestão
↓
Usuário avalia
↓
Novo indexador pode ser incorporado
```

---

# Exemplo Prático

Classe documental:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

Indexadores atuais:

```txt
numeroProcesso
numeroContrato
orgao
empresa
cnpj
datas
valores
```

Durante o processamento de vários documentos semelhantes a IA começa a identificar:

```txt
responsavelFiscalizacao
unidadeGestora
codigoEmpenho
```

O sistema deve sugerir:

```txt
Possível novo indexador:
responsavelFiscalizacao
```

---

# Resultado Esperado

O conhecimento documental deixa de ser:

```txt
Estático
```

e passa a ser:

```txt
Evolutivo
```

---

# Nova Entidade

## DocumentClassIndexerSuggestion

Representa uma sugestão de novo indexador.

---

### Estrutura

```csharp
public sealed class DocumentClassIndexerSuggestion
{
    public Guid Id { get; private set; }

    public Guid DocumentClassId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public IndexerDataType SuggestedDataType { get; private set; }

    public int OccurrenceCount { get; private set; }

    public decimal AverageConfidence { get; private set; }

    public SuggestionStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }
}
```

---

# Enum

## SuggestionStatus

```csharp
public enum SuggestionStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
```

---

# Banco de Dados

## DOCUMENT_CLASS_INDEXER_SUGGESTIONS

Tabela:

```sql
ID
DOCUMENT_CLASS_ID
NAME
DISPLAY_NAME
DESCRIPTION
SUGGESTED_DATA_TYPE
OCCURRENCE_COUNT
AVERAGE_CONFIDENCE
STATUS
CREATED_AT
REVIEWED_AT
REVIEWED_BY_USER_ID
```

---

# Identificação de Novos Indexadores

A IA deverá receber uma instrução adicional.

Exemplo:

```txt
Além dos indexadores conhecidos,
identifique possíveis novos campos
que apareçam de forma recorrente
e sejam relevantes para esta classe documental.
```

---

# Resposta Esperada da IA

Além dos campos conhecidos:

```json
{
  "knownFields": {},
  "suggestedFields": [
    {
      "name": "responsavelFiscalizacao",
      "description": "Nome do responsável pela fiscalização contratual.",
      "confidence": 0.91
    }
  ]
}
```

---

# Critérios para Criar Sugestão

Não criar sugestão imediatamente.

Criar apenas quando:

```txt
Campo apareceu em múltiplos documentos
```

Configuração:

```json
{
  "IncrementalLearning": {
    "MinimumOccurrences": 3,
    "MinimumConfidence": 0.80
  }
}
```

---

# Exemplo

Documento 1:

```txt
responsavelFiscalizacao
```

↓

Contador:

```txt
1
```

Nenhuma sugestão.

---

Documento 2:

```txt
responsavelFiscalizacao
```

↓

Contador:

```txt
2
```

Nenhuma sugestão.

---

Documento 3:

```txt
responsavelFiscalizacao
```

↓

Contador:

```txt
3
```

Criar sugestão.

---

# Serviço de Aprendizado

Criar interface:

```csharp
public interface IIncrementalLearningService
{
    Task AnalyzeAsync(
        Guid documentClassId,
        Dictionary<string, object?> extractedData,
        CancellationToken cancellationToken = default);
}
```

---

# Responsabilidades

```txt
Receber resultado da IA
Detectar campos novos
Contabilizar ocorrências
Calcular confiança média
Criar sugestões
```

---

# Integração com Pipeline

Fluxo atualizado:

```txt
OCR
↓
Classificação
↓
Extração Orientada
↓
Persistência
↓
Aprendizado Incremental
↓
Registrar sugestões
```

---

# Aprovação Manual

O sistema NÃO deve criar novos indexadores automaticamente.

Sempre exigir validação humana.

Fluxo:

```txt
Sugestão criada
↓
Admin revisa
↓
Aprova ou rejeita
```

---

# Aprovação

Quando aprovada:

```txt
DocumentClassIndexerSuggestion
↓
DocumentClassIndexer
```

Criar automaticamente:

```txt
Novo indexador oficial
```

---

# Rejeição

Quando rejeitada:

```txt
Status = Rejected
```

Manter histórico.

---

# Endpoints

## Listar Sugestões

```http
GET /api/document-classes/{id}/indexer-suggestions
```

---

## Aprovar Sugestão

```http
POST /api/document-class-indexer-suggestions/{id}/approve
```

---

## Rejeitar Sugestão

```http
POST /api/document-class-indexer-suggestions/{id}/reject
```

---

# Exemplo de Aprovação

Sugestão:

```txt
responsavelFiscalizacao
```

↓

Aprovar

↓

Criar:

```txt
DocumentClassIndexer
```

↓

Próximos documentos passam a extrair esse campo oficialmente.

---

# Dashboard Futuro

Preparar métricas:

```txt
Quantidade de sugestões pendentes
Sugestões aprovadas
Sugestões rejeitadas
Novos indexadores por classe
Classes que mais evoluem
```

---

# Logs

Criar:

```txt
IncrementalLearningStarted

IndexerSuggestionDetected

IndexerSuggestionCreated

IndexerSuggestionApproved

IndexerSuggestionRejected

IncrementalLearningCompleted
```

---

# Estrutura Sugerida

## Domain

```txt
Entities/
  DocumentClassIndexerSuggestion.cs

Enums/
  SuggestionStatus.cs
```

---

## Application

```txt
IncrementalLearning/
  AnalyzeSuggestions/

IndexerSuggestions/
  Approve/
  Reject/
  List/
```

---

## Infrastructure

```txt
Learning/
  IncrementalLearningService.cs

Suggestions/
  IndexerSuggestionRepository.cs
```

---

## Api

```txt
Controllers/
  IndexerSuggestionsController.cs
```

---

# Segurança

Somente:

```txt
Admin
```

pode:

```txt
Aprovar sugestões
Rejeitar sugestões
```

---

Leitura:

```txt
Admin
Operator
```

---

# Testes Unitários

Criar testes para:

* Detectar novo campo.
* Contabilizar ocorrências.
* Calcular confiança média.
* Criar sugestão após atingir threshold.
* Aprovar sugestão.
* Rejeitar sugestão.
* Converter sugestão em indexador oficial.

---

# Testes de Integração

Criar testes para:

* Persistência de sugestões.
* Consulta de sugestões.
* Aprovação.
* Rejeição.
* Criação automática de DocumentClassIndexer após aprovação.

---

# Critérios de Aceite

A task será considerada concluída quando:

* O sistema identificar possíveis novos indexadores.
* O sistema acumular ocorrências.
* O sistema criar sugestões automaticamente.
* As sugestões forem persistidas.
* A aprovação criar novos indexadores oficiais.
* A rejeição for registrada.
* O pipeline executar aprendizado incremental após o processamento.
* Os endpoints estiverem disponíveis.
* Os testes principais estiverem implementados.
* A solução respeitar Clean Architecture.

---

# Resultado Esperado

Ao final desta task, o Doclyn deixará de depender exclusivamente de indexadores definidos manualmente.

O sistema passará a observar padrões recorrentes nos documentos processados e sugerir melhorias para o catálogo documental, permitindo que cada classe evolua continuamente ao longo do tempo.

Essa será a última peça para transformar o Doclyn de um simples sistema de OCR e extração em uma plataforma de Inteligência Documental evolutiva.
