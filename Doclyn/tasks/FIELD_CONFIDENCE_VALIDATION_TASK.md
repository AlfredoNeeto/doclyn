# FIELD_CONFIDENCE_VALIDATION_TASK.md

# Task — Implementar Confiança por Campo e Validação de Extração

## Contexto

O Doclyn já possui:

* Upload de documentos
* Storage em MinIO
* PostgreSQL
* OCR com Tesseract
* Pipeline de processamento
* Classificação semântica
* Catálogo de Classes Documentais
* Catálogo de Indexadores
* Extração orientada por classe documental
* Regex + IA trabalhando em conjunto
* Persistência dos resultados em JSONB

Atualmente o sistema extrai informações e salva os valores encontrados.

Entretanto, ele ainda não possui um mecanismo para medir a confiabilidade de cada campo extraído.

---

# Problema Atual

Hoje:

```txt id="v2j9w7"
Documento
↓
Extração
↓
Resultado salvo
```

O sistema não sabe:

```txt id="x4t2an"
Qual campo é altamente confiável
Qual campo possui baixa confiança
Qual campo deveria ser revisado
Qual campo veio de Regex
Qual campo veio da IA
```

Todos os campos possuem o mesmo peso.

---

# Objetivo

Implementar um mecanismo de confiança por campo.

Cada valor extraído deverá possuir:

```txt id="8v8xjz"
Valor
Confiança
Origem
Status de validação
```

Exemplo:

```json id="3m8m8w"
{
  "numeroProcesso": {
    "value": "2026/98765",
    "confidence": 0.98,
    "source": "AI",
    "validationStatus": "Validated"
  }
}
```

Objetivo:

```txt id="f5d8aa"
Identificar campos confiáveis
Identificar campos duvidosos
Identificar campos que precisam revisão
Preparar o sistema para validação humana futura
```

---

# Conceito

Nem todos os dados extraídos possuem a mesma qualidade.

Exemplo:

Regex:

```txt id="r6yhm7"
CNPJ
```

Extraído por regex.

Confiança:

```txt id="rm1q9v"
1.00
```

---

IA:

```txt id="6v3bq7"
Empresa contratada
```

Confiança:

```txt id="dtx8vb"
0.88
```

---

Campo pouco claro:

```txt id="fqqz5h"
Assunto
```

Confiança:

```txt id="q1skmf"
0.61
```

---

O sistema deve diferenciar essas situações.

---

# Novo Modelo de Dados

## ExtractedFieldResult

Criar Value Object:

```csharp id="9l3g8k"
public sealed record ExtractedFieldResult(
    object? Value,
    decimal Confidence,
    ExtractionSource Source,
    ValidationStatus ValidationStatus);
```

---

# Enum

## ExtractionSource

```csharp id="f7i6sj"
public enum ExtractionSource
{
    Regex = 1,
    AI = 2,
    Merged = 3,
    Manual = 4
}
```

---

## ValidationStatus

```csharp id="3q5o3g"
public enum ValidationStatus
{
    Validated = 1,
    NeedsReview = 2,
    Rejected = 3
}
```

---

# Estratégia de Confiança

## Regex

Campos extraídos por regex determinística:

```txt id="c9f4nx"
CPF
CNPJ
CEP
Email
Telefone
Datas
```

Confiança:

```txt id="9qj2o5"
1.00
```

---

## IA

A IA deverá retornar:

```json id="fxnfhk"
{
  "value": "Prefeitura Municipal",
  "confidence": 0.92
}
```

---

Caso o modelo não retorne confiança:

Aplicar valor padrão:

```txt id="yww0ng"
0.80
```

Configurável.

---

## Merge Regex + IA

Quando ambos encontrarem:

```txt id="j2f4b0"
Regex
↓
IA
```

Priorizar:

```txt id="7hfhxv"
Regex
```

Confiança final:

```txt id="2wpr6w"
1.00
```

Origem:

```txt id="e56o26"
Merged
```

---

# Thresholds

Adicionar configuração:

```json id="xxyhqo"
{
  "FieldConfidence": {
    "ValidatedThreshold": 0.90,
    "ReviewThreshold": 0.70
  }
}
```

---

# Regras

## Validado

```txt id="vtb9d9"
Confidence >= 0.90
```

Resultado:

```txt id="o3n1pm"
Validated
```

---

## Revisão

```txt id="w2k0pi"
0.70 <= Confidence < 0.90
```

Resultado:

```txt id="kh0z1w"
NeedsReview
```

---

## Rejeitado

```txt id="17pvxa"
Confidence < 0.70
```

Resultado:

```txt id="81yujf"
Rejected
```

---

# Persistência JSONB

Atualizar estrutura.

Exemplo:

```json id="ujc6z2"
{
  "fields": {
    "numeroProcesso": {
      "value": "2026/98765",
      "confidence": 1.0,
      "source": "Regex",
      "validationStatus": "Validated"
    },
    "empresa": {
      "value": "Empresa X",
      "confidence": 0.86,
      "source": "AI",
      "validationStatus": "NeedsReview"
    }
  }
}
```

---

# Serviço de Validação

Criar:

```csharp id="f4pv85"
public interface IFieldValidationService
{
    ValidationStatus DetermineStatus(decimal confidence);
}
```

---

Implementação:

```txt id="7bn3qj"
FieldValidationService
```

Responsabilidades:

```txt id="r9cdb9"
Aplicar thresholds
Determinar status
Padronizar regras
```

---

# Integração com Pipeline

Fluxo atualizado:

```txt id="gn0jrd"
OCR
↓
Classificação
↓
Indexadores
↓
Regex
↓
IA
↓
Merge
↓
Calcular confiança
↓
Definir ValidationStatus
↓
Persistir
```

---

# Atualização da IA

Prompts devem solicitar:

```txt id="3bdm5j"
Valor
Confiança
```

Exemplo:

```txt id="bjlwmx"
Retorne:

value
confidence

Confidence deve variar entre 0 e 1.
```

---

# Campos Obrigatórios

Quando um indexador obrigatório não for encontrado:

```txt id="v2kp7p"
Value = null
Confidence = 0
ValidationStatus = Rejected
```

---

# Dashboard Futuro

Preparar estrutura para métricas:

```txt id="v0t1kp"
Campos validados
Campos em revisão
Campos rejeitados
Confiança média por classe
Confiança média por documento
```

---

# Logs

Criar:

```txt id="qkmvwr"
FieldValidated

FieldNeedsReview

FieldRejected

ConfidenceCalculated
```

---

# Estrutura Sugerida

## Domain

```txt id="3u4v0i"
Enums/
  ValidationStatus.cs
  ExtractionSource.cs

ValueObjects/
  ExtractedFieldResult.cs
```

---

## Application

```txt id="ktcxtk"
Validation/
  IFieldValidationService.cs
```

---

## Infrastructure

```txt id="t5rzz7"
Validation/
  FieldValidationService.cs
```

---

# Endpoints

## Consultar Campos com Status

```http id="j3g5ah"
GET /api/documents/{id}/extracted-data
```

Retornar:

```json id="egonjn"
{
  "fields": {
    "empresa": {
      "value": "Empresa X",
      "confidence": 0.86,
      "source": "AI",
      "validationStatus": "NeedsReview"
    }
  }
}
```

---

## Consultar Campos Pendentes de Revisão

```http id="plpjec"
GET /api/documents/{id}/review-fields
```

---

# Testes Unitários

Criar testes para:

* Regex gera confiança 1.00.
* IA gera confiança correta.
* Merge prioriza Regex.
* Campo acima de 0.90 é validado.
* Campo entre 0.70 e 0.90 exige revisão.
* Campo abaixo de 0.70 é rejeitado.
* Campo obrigatório ausente é rejeitado.

---

# Testes de Integração

Criar testes para:

* Persistência correta no JSONB.
* Cálculo de status.
* Consulta dos campos.
* Consulta dos campos pendentes.
* Reprocessamento recalcula confiança.

---

# Critérios de Aceite

A task será considerada concluída quando:

* Todo campo extraído possuir confiança.
* Todo campo possuir origem.
* Todo campo possuir status de validação.
* Regex gerar confiança máxima.
* IA gerar confiança configurável.
* Merge funcionar corretamente.
* JSONB armazenar metadados de confiança.
* Thresholds forem configuráveis.
* Logs estiverem implementados.
* Testes principais estiverem implementados.
* A solução respeitar Clean Architecture.

---

# Resultado Esperado

Ao final desta task, o Doclyn será capaz de avaliar individualmente cada informação extraída.

O sistema deixará de apenas armazenar dados e passará a medir a qualidade de cada campo, identificando automaticamente quais informações são confiáveis e quais precisam de revisão.

Essa camada será fundamental para auditoria, validação humana futura, aprendizado incremental e evolução da inteligência documental da plataforma.
