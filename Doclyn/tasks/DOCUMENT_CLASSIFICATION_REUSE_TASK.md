# DOCUMENT_CLASSIFICATION_REUSE_TASK.md

# Task — Implementar Classificação Semântica com IA e Reuso de Classes Documentais

## Contexto

O Doclyn já possui:

* Upload de documentos
* Storage em MinIO
* PostgreSQL
* OCR com Tesseract
* Extração de texto
* Pipeline de processamento
* Classificação documental via IA
* Catálogo de Classes Documentais (`DocumentClass`)
* Catálogo de Indexadores por Classe (`DocumentClassIndexer`)
* Persistência dos dados extraídos em JSONB
* Reprocessamento
* Logs de processamento

Atualmente a classificação documental funciona de forma isolada.

A IA classifica o documento, mas não utiliza o conhecimento acumulado das classes documentais já existentes.

---

# Problema Atual

Hoje o fluxo é:

```txt
Documento
↓
Texto extraído
↓
IA classifica
↓
Resultado utilizado apenas naquele processamento
```

Isso pode gerar problemas como:

```txt
RELATORIO_TECNICO_PRELIMINAR

Relatório Técnico Preliminar

RELATORIO_TECNICO

RELATORIO_PRELIMINAR
```

Todos representando praticamente o mesmo tipo documental.

O sistema não reaproveita o conhecimento existente.

---

# Objetivo

Transformar a classificação documental em um processo semântico baseado em reutilização de classes existentes.

Novo fluxo:

```txt
Texto extraído
↓
Carregar classes documentais existentes
↓
IA compara documento com classes conhecidas
↓
Se existir classe semelhante:
    reutilizar
↓
Se não existir:
    propor nova classe
↓
Persistir resultado
```

Objetivo final:

```txt
Documentos semelhantes
↓
Mesma classe documental
↓
Mesmos indexadores
↓
Mesmo pipeline de extração
```

---

# Conceito

O sistema deve possuir uma memória documental.

Exemplo:

Classe existente:

```txt
RELATORIO_TECNICO_PRELIMINAR

Grupo:
ADMINISTRATIVO

Subgrupo:
PROCESSO_ADMINISTRATIVO
```

Novo documento recebido:

```txt
Relatório Técnico de Fiscalização Contratual
```

A IA deve entender que:

```txt
Pertence à mesma família documental
```

e reutilizar:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

ao invés de criar:

```txt
RELATORIO_TECNICO_FISCALIZACAO
```

---

# Arquitetura

## Novo Serviço

Criar interface:

```csharp
public interface IDocumentSemanticClassificationService
{
    Task<SemanticClassificationResult> ClassifyAsync(
        string extractedText,
        CancellationToken cancellationToken = default);
}
```

---

## Resultado

```csharp
public sealed record SemanticClassificationResult(
    Guid? DocumentClassId,
    string DocumentType,
    string Group,
    string SubGroup,
    decimal Confidence,
    bool ReusedExistingClass,
    bool NewClassSuggested);
```

---

# Estratégia

A classificação ocorrerá em duas etapas.

## Etapa 1 — Recuperação das Classes Existentes

Buscar:

```txt
DocumentClass
```

ativas.

Exemplo:

```txt
RELATORIO_TECNICO_PRELIMINAR
CONTRATO_ADMINISTRATIVO
OFICIO
NOTA_FISCAL
PETICAO_JUDICIAL
```

---

## Etapa 2 — Classificação Semântica

Enviar para IA:

### Entrada

```txt
Texto do documento

+
Lista de classes existentes
```

---

### Exemplo de Prompt

```txt
Você é um classificador documental.

Classes conhecidas:

- RELATORIO_TECNICO_PRELIMINAR
- CONTRATO_ADMINISTRATIVO
- OFICIO
- NOTA_FISCAL
- PETICAO_JUDICIAL

Analise o documento.

Se ele pertencer claramente a uma classe existente,
retorne essa classe.

Somente crie uma nova classe quando nenhuma
classe existente representar corretamente o documento.

Retorne apenas JSON válido.
```

---

# Resposta Esperada

## Classe existente

```json
{
  "documentClassName": "RELATORIO_TECNICO_PRELIMINAR",
  "reuseExistingClass": true,
  "confidence": 0.97
}
```

---

## Nova classe

```json
{
  "documentClassName": "PARECER_JURIDICO",
  "group": "JURIDICO",
  "subGroup": "CONSULTIVO",
  "reuseExistingClass": false,
  "confidence": 0.95
}
```

---

# Criação Automática de Classe

Quando:

```txt
reuseExistingClass = false
```

o sistema deve:

```txt
Criar nova DocumentClass
↓
Persistir no catálogo
↓
Registrar exemplo documental
↓
Continuar pipeline
```

---

# Integração com DocumentClassExample

Sempre que um documento for classificado:

Criar:

```txt
DocumentClassExample
```

Exemplo:

```txt
Classe:
RELATORIO_TECNICO_PRELIMINAR

Documento:
4c7b845f...

Confiança:
0.97
```

Objetivo:

```txt
Criar histórico de exemplos da classe
```

---

# Integração com Pipeline

Fluxo atual:

```txt
OCR
↓
Regex
↓
IA Extração
```

Novo fluxo:

```txt
OCR
↓
Classificação Semântica
↓
Identificar DocumentClass
↓
Carregar Indexadores da Classe
↓
Regex
↓
IA Extração
↓
Persistir
```

---

# Atualização do JSONB

Salvar:

```json
{
  "classification": {
    "documentClassId": "guid",
    "documentType": "RELATORIO_TECNICO_PRELIMINAR",
    "group": "ADMINISTRATIVO",
    "subGroup": "PROCESSO_ADMINISTRATIVO",
    "confidence": 0.97,
    "reusedExistingClass": true
  }
}
```

---

# Integração com IA

Criar:

```txt
OpenAiSemanticClassifier
```

Responsabilidades:

```txt
Receber texto
Receber catálogo de classes
Comparar semanticamente
Retornar melhor classe
Sugerir nova classe quando necessário
```

---

# Regras de Criação

Criar nova classe apenas quando:

```txt
Confidence < limite mínimo para classes existentes
```

Exemplo:

```txt
Threshold = 0.85
```

Se:

```txt
0.85 ou maior
```

Reutilizar.

Se:

```txt
menor que 0.85
```

Criar nova classe.

Configuração:

```json
{
  "Classification": {
    "ReuseThreshold": 0.85
  }
}
```

---

# Evitar Explosão de Classes

Não permitir criação automática para:

```txt
Diferenças de nomenclatura
Diferenças de escrita
Diferenças de capitalização
```

Exemplo:

```txt
RELATORIO_TECNICO_PRELIMINAR

Relatório Técnico Preliminar

Relatorio Tecnico
```

Devem apontar para:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

---

# Logs

Criar:

```txt
SemanticClassificationStarted

ExistingClassMatched

NewClassSuggested

DocumentClassCreated

DocumentClassReused

SemanticClassificationCompleted

SemanticClassificationFailed
```

---

# Estrutura Sugerida

## Application

```txt
DocumentClassification/
  SemanticClassification/
```

---

## Infrastructure

```txt
AI/
  OpenAiSemanticClassifier.cs

Classification/
  SemanticClassificationService.cs
```

---

# Endpoints

## Consultar Classes Mais Utilizadas

```http
GET /api/document-classes/top
```

---

## Consultar Exemplos de Classe

```http
GET /api/document-classes/{id}/examples
```

---

## Reclassificar Documento

```http
POST /api/documents/{id}/reclassify
```

Força nova classificação usando catálogo atualizado.

---

# Testes Unitários

Criar testes para:

* Reutilizar classe existente.
* Criar nova classe.
* Aplicar threshold corretamente.
* Registrar exemplo.
* Impedir duplicação de classe.
* Normalizar nomes semelhantes.

---

# Testes de Integração

Criar testes para:

* Documento semelhante reutiliza classe existente.
* Documento novo cria classe.
* Classe criada é persistida.
* Exemplo é registrado.
* Reclassificação funciona.
* JSONB é atualizado.

---

# Critérios de Aceite

A task será considerada concluída quando:

* O sistema consultar classes existentes antes de criar novas.
* Classes semelhantes forem reutilizadas.
* Novas classes forem criadas apenas quando necessário.
* DocumentClassExample for alimentado automaticamente.
* O pipeline utilizar a classificação semântica.
* O JSONB armazenar informações da classificação.
* O threshold de reutilização estiver configurável.
* Os testes principais estiverem implementados.
* A solução respeitar Clean Architecture.

---

# Resultado Esperado

Ao final desta task, o Doclyn deixará de classificar documentos de forma isolada e passará a construir conhecimento documental reutilizável.

Documentos semanticamente semelhantes passarão a cair na mesma classe documental, permitindo que futuras etapas utilizem os mesmos indexadores e regras de extração.
