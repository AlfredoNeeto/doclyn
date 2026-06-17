# CLASS_GUIDED_EXTRACTION_TASK.md

# Task — Implementar Extração Orientada por Classe Documental

## Contexto

O Doclyn já possui:

* Upload de documentos
* Storage em MinIO
* PostgreSQL
* OCR com Tesseract
* Extração de texto
* Pipeline de processamento
* Classificação semântica via IA
* Catálogo de Classes Documentais (`DocumentClass`)
* Catálogo de Indexadores por Classe (`DocumentClassIndexer`)
* Extração por Regex
* Extração por IA
* Persistência em JSONB
* Reprocessamento
* Logs de processamento

Atualmente a IA realiza extração de forma relativamente genérica.

Embora o documento seja classificado, a extração ainda não é totalmente guiada pelo conhecimento da classe documental.

---

# Problema Atual

Hoje o fluxo funciona assim:

```txt
Documento
↓
OCR
↓
Classificação
↓
Regex
↓
IA
↓
JSONB
```

A IA recebe o documento e extrai informações usando prompts genéricos.

Consequências:

* Extração inconsistente.
* Campos diferentes para documentos semelhantes.
* Dificuldade de padronização.
* Baixa reutilização do catálogo documental.
* Menor previsibilidade dos resultados.

---

# Objetivo

Transformar a extração em um processo orientado pela classe documental.

Novo fluxo:

```txt
Classe identificada
↓
Buscar indexadores da classe
↓
Montar prompt dinâmico
↓
Extrair exatamente esses campos
↓
Persistir resultado
```

Objetivo:

```txt
Documentos da mesma classe
↓
Mesmo conjunto de indexadores
↓
Mesmo formato de saída
↓
Maior consistência
```

---

# Conceito

Hoje:

```txt
CONTRATO_ADMINISTRATIVO
↓
IA extrai vários campos
↓
Resultado variável
```

Após esta implementação:

```txt
CONTRATO_ADMINISTRATIVO
↓
Carregar indexadores da classe
↓
IA extrai somente esses campos
↓
Resultado padronizado
```

---

# Exemplo

## Classe

```txt
RELATORIO_TECNICO_PRELIMINAR
```

## Indexadores cadastrados

```txt
numeroProcesso
numeroContrato
orgao
empresa
cnpj
cpfs
matriculas
datas
valores
emails
telefones
documentosRelacionados
assunto
resumo
```

---

## Prompt Gerado Dinamicamente

```txt
Você está analisando um documento da classe:

RELATORIO_TECNICO_PRELIMINAR

Extraia exclusivamente os seguintes indexadores:

- numeroProcesso
- numeroContrato
- orgao
- empresa
- cnpj
- cpfs
- matriculas
- datas
- valores
- emails
- telefones
- documentosRelacionados
- assunto
- resumo

Retorne apenas JSON válido.
Não invente informações.
Retorne null quando não encontrar um valor.
```

---

# Resultado Esperado

```json
{
  "numeroProcesso": "2026/98765",
  "numeroContrato": "45/2026",
  "orgao": "Prefeitura Municipal de Vale Verde",
  "empresa": "Empresa X",
  "cnpj": "12.345.678/0001-99",
  "cpfs": [
    "123.456.789-00"
  ],
  "matriculas": [
    "445566"
  ],
  "datas": [
    "2026-06-10"
  ],
  "valores": [
    "15000.00"
  ],
  "emails": [
    "contato@empresa.com.br"
  ],
  "telefones": [
    "(11)99999-9999"
  ],
  "documentosRelacionados": [
    "Contrato Administrativo"
  ],
  "assunto": "Fiscalização contratual",
  "resumo": "Resumo do conteúdo"
}
```

---

# Arquitetura

## Novo Serviço

Criar interface:

```csharp
public interface IClassGuidedExtractionService
{
    Task<ClassGuidedExtractionResult> ExtractAsync(
        Guid documentClassId,
        string documentText,
        CancellationToken cancellationToken = default);
}
```

---

## Resultado

```csharp
public sealed record ClassGuidedExtractionResult(
    Guid DocumentClassId,
    Dictionary<string, ExtractedFieldResult> Fields);
```

---

## Campo Extraído

```csharp
public sealed record ExtractedFieldResult(
    object? Value,
    decimal Confidence,
    string Source);
```

---

# Integração com DocumentClassIndexer

O serviço deve consultar:

```txt
DOCUMENT_CLASS_INDEXERS
```

ativos da classe documental.

Exemplo:

```txt
DocumentClass:
RELATORIO_TECNICO_PRELIMINAR

Indexadores:
numeroProcesso
numeroContrato
orgao
empresa
cnpj
```

---

# Geração Dinâmica de Prompt

Criar:

```csharp
public interface IExtractionPromptBuilder
{
    string Build(
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers,
        string documentText);
}
```

Responsabilidade:

```txt
Receber classe documental
Receber indexadores
Montar prompt específico
```

---

# Estratégia de Extração

## Etapa 1

Regex primeiro.

Para cada indexador:

```txt
Possui RegexPattern?
↓
Sim
↓
Executar Regex
```

---

## Etapa 2

Campos não encontrados:

```txt
Regex não encontrou
↓
Enviar para IA
```

---

## Estratégia Híbrida

```txt
Regex
↓
Campos determinísticos

IA
↓
Campos semânticos

Merge
↓
Resultado final
```

---

# Prioridade das Fontes

Ordem de prioridade:

```txt
Regex
↓
IA
↓
Manual
```

Exemplo:

```txt
Regex encontrou CNPJ
↓
Utilizar Regex

IA encontrou valor diferente
↓
Ignorar
```

---

# Prompt Builder

O Prompt Builder deve incluir:

## Classe

```txt
RELATORIO_TECNICO_PRELIMINAR
```

---

## Grupo

```txt
ADMINISTRATIVO
```

---

## Subgrupo

```txt
PROCESSO_ADMINISTRATIVO
```

---

## Descrição da Classe

```txt
Descrição cadastrada em DocumentClass
```

---

## Indexadores

Para cada indexador:

```txt
Nome
Descrição
Tipo
Obrigatório
Dica de extração
```

---

# Suporte a Novas Classes

Nenhum código deve ser alterado para suportar novas classes.

Exemplo:

Hoje:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

Amanhã:

```txt
PETICAO_JUDICIAL
```

O sistema deve funcionar apenas carregando:

```txt
DocumentClass
DocumentClassIndexer
```

---

# Atualização do Pipeline

Fluxo atual:

```txt
OCR
↓
Classificação
↓
Extração IA
```

Novo fluxo:

```txt
OCR
↓
Classificação Semântica
↓
Identificar Classe
↓
Carregar Indexadores
↓
Regex
↓
Extração Guiada
↓
Merge
↓
Persistir
```

---

# Persistência

Atualizar estrutura JSONB.

Exemplo:

```json
{
  "classification": {
    "documentClassId": "guid",
    "documentType": "RELATORIO_TECNICO_PRELIMINAR"
  },
  "fields": {
    "numeroProcesso": {
      "value": "2026/98765",
      "confidence": 1.0,
      "source": "Regex"
    },
    "orgao": {
      "value": "Prefeitura Municipal",
      "confidence": 0.95,
      "source": "AI"
    }
  }
}
```

---

# Logs

Criar:

```txt
ClassGuidedExtractionStarted

ClassIndexersLoaded

DynamicPromptGenerated

RegexExtractionCompleted

AiGuidedExtractionCompleted

FieldMerged

ClassGuidedExtractionCompleted

ClassGuidedExtractionFailed
```

---

# Estrutura Sugerida

## Application

```txt
DocumentExtraction/
  ClassGuidedExtraction/
```

---

## Infrastructure

```txt
AI/
  PromptBuilder/
    DynamicExtractionPromptBuilder.cs

Extraction/
  ClassGuidedExtractionService.cs
```

---

## Domain

```txt
ValueObjects/
  ExtractedFieldResult.cs
```

---

# Endpoints

## Consultar Campos Extraídos

```http
GET /api/documents/{id}/extracted-data
```

---

## Reprocessar com Extração Guiada

```http
POST /api/documents/{id}/reprocess
```

Deve utilizar:

```txt
ClassGuidedExtractionService
```

---

# Segurança

Mesmas regras já utilizadas no pipeline.

```txt
Operator:
apenas seus documentos

Admin:
todos os documentos
```

---

# Testes Unitários

Criar testes para:

* Carregar indexadores da classe.
* Gerar prompt dinâmico.
* Executar regex corretamente.
* Executar IA somente para campos faltantes.
* Realizar merge corretamente.
* Priorizar regex sobre IA.
* Retornar resultado estruturado.

---

# Testes de Integração

Criar testes para:

* Classe conhecida.
* Classe nova.
* Documento sem campos obrigatórios.
* Reprocessamento.
* Persistência JSONB.
* Carregamento correto dos indexadores.

---

# Critérios de Aceite

A task será considerada concluída quando:

* A extração for guiada pela classe documental.
* Os indexadores forem carregados dinamicamente.
* O prompt for gerado dinamicamente.
* A IA extrair apenas os campos esperados.
* Regex possuir prioridade sobre IA.
* O resultado for persistido em JSONB.
* Novas classes funcionarem sem alteração de código.
* O pipeline utilizar a nova estratégia.
* Os testes principais estiverem implementados.
* A solução respeitar Clean Architecture.

---

# Resultado Esperado

Ao final desta task, o Doclyn deixará de fazer extrações genéricas e passará a executar uma extração inteligente orientada pela classe documental identificada.

Isso garante que documentos semanticamente semelhantes produzam resultados consistentes, previsíveis e reutilizáveis, aproximando o sistema do objetivo principal de criar conhecimento documental reutilizável e evolutivo.
