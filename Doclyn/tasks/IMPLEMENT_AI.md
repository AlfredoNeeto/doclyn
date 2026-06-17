# Task — Implementar IA para Classificação e Extração Inteligente de Documentos

# Contexto

O Doclyn já possui:

* Upload de documentos
* Storage MinIO
* PostgreSQL
* Logs de processamento
* OCR com Tesseract
* Pipeline de processamento
* Extração por Regex
* Persistência em JSONB
* Hangfire para processamento assíncrono

Agora será adicionada a camada de Inteligência Artificial responsável por:

* Classificação documental
* Extração avançada de indexadores
* Tratamento de documentos desconhecidos
* Enriquecimento dos dados extraídos

---

# Objetivo

Implementar IA dentro do pipeline documental.

Fluxo esperado:

```txt
PDF
↓
Extração de texto
↓
OCR (quando necessário)
↓
Regex
↓
IA Classificação
↓
IA Extração Estruturada
↓
Validação
↓
Persistência JSONB
↓
PROCESSED
```

---

# Modelo escolhido

Utilizar:

```txt
gpt-4o-mini
```

Motivos:

* Excelente custo-benefício.
* Suporte multimodal futuro.
* Boa capacidade de classificação documental.
* Boa capacidade de extração estruturada.
* Baixo custo operacional.
* Adequado para MVP e entrevistas.

---

# Estratégia

A IA não deve substituir Regex.

Utilizar arquitetura híbrida:

```txt
Regex
↓
Campos determinísticos

IA
↓
Campos semânticos
```

---

# Regex continua responsável por

```txt
CPF
CNPJ
CEP
Telefone
Email
Datas
Valores
Agência Bancária
Conta Bancária
```

---

# IA responsável por

```txt
Tipo documental
Grupo
Subgrupo
Assunto
Órgão
Empresa
Pessoas citadas
Documentos relacionados
Resumo
Palavras-chave
Entidades relevantes
```

---

# Arquitetura

Criar em Application:

```csharp
public interface IAiDocumentClassifier
{
    Task<DocumentClassificationResult> ClassifyAsync(
        string text,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IAiStructuredDataExtractor
{
    Task<StructuredExtractionResult> ExtractAsync(
        string text,
        string documentType,
        CancellationToken cancellationToken = default);
}
```

---

# Infraestrutura

Criar:

```txt
Doclyn.Infrastructure/
  AI/
    OpenAiOptions.cs
    OpenAiClientFactory.cs
    OpenAiDocumentClassifier.cs
    OpenAiStructuredDataExtractor.cs
    PromptBuilder.cs
```

---

# Configuração

Adicionar:

```json
{
  "OpenAi": {
    "Model": "gpt-4o-mini",
    "ApiKey": "",
    "Temperature": 0.1,
    "MaxTokens": 4000
  }
}
```

A chave deve vir de:

```txt
Environment Variables
User Secrets
Secret Manager
```

Nunca do código.

---

# Etapa 1 — Classificação Documental

Criar classificador.

Entrada:

```txt
Texto completo do documento
```

Saída:

```json
{
  "documentType": "RELATORIO_TECNICO_PRELIMINAR",
  "group": "PROCESSO_ADMINISTRATIVO",
  "subGroup": "APURACAO_CONTRATUAL",
  "confidence": 0.98
}
```

---

# Tipos conhecidos inicialmente

```txt
RELATORIO_TECNICO_PRELIMINAR
CONTRATO
CNH
RG
CPF
COMPROVANTE_RESIDENCIA
NOTA_FISCAL
OFICIO
DESCONHECIDO
```

---

# Prompt de Classificação

A IA deve responder apenas JSON válido.

Exemplo:

```txt
Analise o documento.

Classifique o documento.

Retorne apenas JSON válido.

Campos:

documentType
group
subGroup
confidence
```

---

# Etapa 2 — Extração Estruturada

Após classificar:

```txt
Document Type
↓
Prompt especializado
↓
Extração
```

---

# Documento conhecido

Exemplo:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

Prompt especializado.

Saída:

```json
{
  "numeroProcesso": "",
  "numeroContrato": "",
  "orgao": "",
  "empresa": "",
  "cnpj": "",
  "cpfs": [],
  "matriculas": [],
  "datas": [],
  "valores": [],
  "emails": [],
  "telefones": [],
  "enderecos": [],
  "documentosRelacionados": [],
  "palavrasChave": []
}
```

---

# Documento desconhecido

Caso a classificação retorne:

```txt
DESCONHECIDO
```

Utilizar extrator genérico.

Saída:

```json
{
  "summary": "",
  "entities": {},
  "keywords": [],
  "confidence": 0.0
}
```

---

# Estratégia de Prompts

Criar:

```txt
Prompts/
```

Exemplo:

```txt
classification.prompt.md

relatorio-tecnico.prompt.md

contrato.prompt.md

generic.prompt.md
```

Objetivo:

* Facilitar manutenção.
* Facilitar evolução.
* Versionar prompts.

---

# Pipeline Atualizado

```txt
PROCESSING
↓
PDF
↓
OCR
↓
Regex
↓
Classificação IA
↓
Extração IA
↓
Merge Regex + IA
↓
Validação
↓
Persistência
↓
PROCESSED
```

---

# Merge de Resultados

Regex possui prioridade.

Exemplo:

```txt
Regex encontrou CPF
↓
Utilizar Regex

Regex não encontrou CPF
↓
Utilizar IA
```

Motivo:

```txt
Regex é determinístico.
```

---

# Persistência

Salvar em:

```txt
EXTRACTED_DATA.DATA_JSON
```

Estrutura:

```json
{
  "classification": {},
  "regexExtraction": {},
  "aiExtraction": {},
  "finalResult": {}
}
```

---

# Confiabilidade

Salvar score:

```json
{
  "confidence": 0.97
}
```

Para futura validação humana.

---

# Reprocessamento

Ao reprocessar:

```txt
Limpar extração anterior
↓
Executar OCR
↓
Executar Regex
↓
Executar IA
↓
Persistir novo resultado
```

---

# Logs

Criar logs:

```txt
AiClassificationStarted
AiClassificationCompleted

AiExtractionStarted
AiExtractionCompleted

AiClassificationFailed
AiExtractionFailed
```

Não registrar:

```txt
Texto completo
Prompt completo
Dados sensíveis
```

---

# Falhas

Se IA falhar:

```txt
Utilizar apenas Regex
```

Status:

```txt
PROCESSED
```

Com log:

```txt
AI unavailable. Partial extraction completed.
```

O sistema não deve falhar completamente por indisponibilidade da IA.

---

# Testes Unitários

Criar testes para:

* Merge Regex + IA.
* Fallback quando IA falha.
* Parsing de resposta JSON.
* Documento conhecido.
* Documento desconhecido.
* Score de confiança.

---

# Testes de Integração

Criar testes para:

* Pipeline completo.
* OCR + IA.
* Regex + IA.
* Documento conhecido.
* Documento desconhecido.
* Reprocessamento.
* Falha da IA.

Mockar chamadas OpenAI.

Nunca chamar OpenAI real durante testes.

---

# Critérios de Aceite

A task será considerada concluída quando:

* Classificação documental via IA estiver funcionando.
* Extração estruturada via IA estiver funcionando.
* Documentos desconhecidos forem suportados.
* Regex e IA trabalharem em conjunto.
* Resultado final for persistido em JSONB.
* Fallback sem IA estiver funcionando.
* Reprocessamento utilizar IA.
* Prompts estiverem versionados.
* Logs estiverem implementados.
* Testes estiverem implementados.
* Código respeitar Clean Architecture.
* O modelo utilizado for gpt-4o-mini.
