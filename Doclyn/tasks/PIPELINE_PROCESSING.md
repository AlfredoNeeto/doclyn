# Task — Criar Pipeline Inicial de Processamento Documental

## Contexto

O projeto Doclyn já possui:

* Autenticação JWT
* PostgreSQL
* MinIO
* Upload de documentos PDF
* Registro de metadados em banco
* Status inicial `PENDING`
* Logs de processamento
* Entidades `Document`, `ExtractedData` e `ProcessingLog`

Agora será criado o pipeline inicial de processamento documental.

Nesta etapa, o objetivo é processar documentos PDF digitais usando extração simples de texto, classificação por regra e extração inicial por regex.

## Importante

Nesta task ainda não implementar IA real.

Nesta task ainda não implementar OCR completo.

O OCR será implementado posteriormente como fallback para documentos escaneados ou PDFs sem texto pesquisável.

## Fluxo esperado

```txt
PENDING
↓
PROCESSING
↓
Extração de texto simples do PDF
↓
Classificação mockada ou por regra
↓
Extração inicial por regex
↓
PROCESSED ou FAILED
```

## Objetivo

Implementar um pipeline funcional para processar documentos já enviados ao sistema.

O pipeline deve:

* Buscar documento com status `PENDING`
* Atualizar status para `PROCESSING`
* Baixar o PDF do MinIO
* Extrair texto simples do PDF
* Classificar o tipo documental por regra
* Extrair indexadores iniciais por regex
* Salvar os dados extraídos em `EXTRACTED_DATA`
* Atualizar status para `PROCESSED`
* Registrar logs de cada etapa
* Em caso de erro, atualizar status para `FAILED`

## Fora do escopo

Não implementar nesta task:

* OCR com Tesseract
* OCR em nuvem
* IA generativa
* OpenAI
* Azure Document Intelligence
* Fila externa
* Reprocessamento manual
* Validação humana dos dados extraídos

## Documento modelo inicial

Usar como referência inicial o documento modelo de teste:

```txt
Relatório Técnico Preliminar de Processo Administrativo
```

Tipo esperado:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

Grupo:

```txt
PROCESSO_ADMINISTRATIVO
```

Subgrupo:

```txt
APURACAO_CONTRATUAL
```

## Indexadores iniciais esperados

Extrair, quando existirem:

* Número do processo
* Número do contrato
* Órgão público
* Empresa contratada
* CNPJ
* Pessoas citadas
* CPF
* Matrícula funcional
* Valores financeiros
* Datas
* Nota fiscal
* Ofício
* E-mails
* Telefones
* Endereço
* CEP
* Agência bancária
* Conta bancária
* Documentos relacionados
* Palavras-chave de classificação

## Exemplo de JSON esperado

```json
{
  "tipoDocumento": "RELATORIO_TECNICO_PRELIMINAR",
  "grupo": "PROCESSO_ADMINISTRATIVO",
  "subgrupo": "APURACAO_CONTRATUAL",
  "numeroProcesso": "2026/98765",
  "numeroContrato": "45/2026",
  "orgao": "Prefeitura Municipal de Vale Verde",
  "empresa": "Soluções Integradas do Centro-Oeste LTDA",
  "cnpj": "12.345.678/0001-99",
  "cpfs": ["123.456.789-00"],
  "matriculas": ["445566"],
  "valores": ["12345.67"],
  "datas": ["14/03/2026", "02/04/2026", "12/06/2026", "28/04/2026"],
  "notaFiscal": "NF-2026-000123",
  "oficio": "112/2026",
  "emails": ["contato@solucoesintegradas.com.br"],
  "telefones": ["(65) 99999-9999"],
  "cep": "78000-000",
  "agencia": "1234",
  "contaCorrente": "56789-0",
  "documentosRelacionados": [
    "Contrato",
    "Nota Fiscal",
    "Ofício",
    "Relatório Técnico",
    "Planilhas",
    "Empenhos",
    "Ordens de Serviço"
  ]
}
```

## Interfaces sugeridas

Criar em `Doclyn.Application/Common/Interfaces`:

```csharp
public interface IDocumentProcessingService
{
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
}
```

```csharp
public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
```

```csharp
public interface IDocumentClassifier
{
    DocumentClassificationResult Classify(string text);
}
```

```csharp
public interface IDocumentIndexer
{
    Dictionary<string, object?> ExtractIndexes(string text, string documentType);
}
```

## Implementações sugeridas

Criar em `Doclyn.Infrastructure`:

```txt
Processing/
  DocumentProcessingService.cs

PDF/
  PdfTextExtractor.cs

Classification/
  RuleBasedDocumentClassifier.cs

Extraction/
  RegexDocumentIndexer.cs
```

## Regras de status

Ao iniciar processamento:

```txt
PENDING → PROCESSING
```

Ao finalizar com sucesso:

```txt
PROCESSING → PROCESSED
```

Ao ocorrer erro:

```txt
PROCESSING → FAILED
```

## Logs obrigatórios

Criar logs em `PROCESSING_LOGS` para:

```txt
ProcessingStarted
TextExtracted
DocumentClassified
IndexesExtracted
ProcessingCompleted
ProcessingFailed
```

Exemplo:

```txt
STEP = TextExtracted
STATUS = Success
MESSAGE = Text extracted from PDF successfully.
```

Não salvar o texto completo do PDF nos logs.

## Extração de texto

Nesta task, usar extração simples de texto para PDF digital.

Bibliotecas possíveis:

```txt
UglyToad.PdfPig
iText
PdfSharp
```

Recomendação para MVP:

```txt
UglyToad.PdfPig
```

Se o texto extraído estiver vazio ou muito pequeno, registrar log:

```txt
Text extraction returned insufficient content. OCR may be required.
```

Nesse caso, marcar o documento como `FAILED` ou manter estratégia configurável.

Para o MVP, pode marcar como `FAILED` com mensagem clara.

## Classificação por regra

Classificar como `RELATORIO_TECNICO_PRELIMINAR` se o texto contiver termos como:

```txt
PROCESSO ADMINISTRATIVO
RELATÓRIO TÉCNICO PRELIMINAR
CONTRATO
FISCALIZAÇÃO CONTRATUAL
PREFEITURA MUNICIPAL
PROCURADORIA JURÍDICA
```

Caso não identifique:

```txt
UNKNOWN
```

## Regex iniciais

### Número do processo

```regex
PROCESSO\\s+ADMINISTRATIVO\\s+N[º°]?\\s*([\\d\\/]+)
```

### Número do contrato

```regex
Contrato\\s+n[º°]?\\s*([\\d\\/]+)
```

### CNPJ

```regex
\\d{2}\\.\\d{3}\\.\\d{3}\\/\\d{4}-\\d{2}
```

### CPF

```regex
\\d{3}\\.\\d{3}\\.\\d{3}-\\d{2}
```

### Matrícula funcional

```regex
matr[íi]cula\\s+funcional\\s+(\\d+)
```

### Valores financeiros

```regex
R\\$\\s*\\d{1,3}(?:\\.\\d{3})*,\\d{2}
```

### Datas

```regex
\\d{2}\\/\\d{2}\\/\\d{4}
```

### E-mail

```regex
[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}
```

### Telefone

```regex
\\(\\d{2}\\)\\s*\\d{4,5}-\\d{4}
```

### CEP

```regex
\\d{5}-\\d{3}
```

### Ofício

```regex
Of[ií]cio\\s+n[º°]?\\s*([\\d\\/-]+)
```

### Nota fiscal

```regex
Nota\\s+Fiscal\\s+n[º°]?\\s*([A-Z0-9\\-]+)
```

## Persistência

Salvar o resultado em `EXTRACTED_DATA.DATA_JSON`.

O campo deve ser armazenado como `JSONB`.

Também atualizar em `DOCUMENTS`:

```txt
DOCUMENT_TYPE
DOCUMENT_STATUS
PROCESSED_AT
UPDATED_AT
```

## Endpoint para iniciar processamento

Criar endpoint protegido:

```http
POST /api/documents/{id}/process
```

Regras:

* Usuário `Operator` só pode processar seus próprios documentos.
* Usuário `Admin` pode processar qualquer documento.
* Documento precisa estar com status `PENDING` ou `FAILED`.
* Se já estiver `PROCESSING`, retornar erro.
* Se já estiver `PROCESSED`, retornar erro ou exigir reprocessamento futuro.

## Response

```json
{
  "documentId": "4c7b845f-7da9-4a64-b1d0-4f15f1bb1f21",
  "status": "Processed",
  "documentType": "RelatorioTecnicoPreliminar",
  "processedAt": "2026-06-16T15:00:00Z"
}
```

## Testes unitários

Criar testes para:

* Classificar relatório técnico preliminar corretamente.
* Retornar `Unknown` quando não reconhecer o texto.
* Extrair número do processo.
* Extrair número do contrato.
* Extrair CNPJ.
* Extrair CPF.
* Extrair datas.
* Extrair valores financeiros.
* Extrair e-mail.
* Extrair telefone.
* Extrair CEP.
* Alterar status para `Processing`.
* Alterar status para `Processed`.
* Alterar status para `Failed` em caso de erro.

## Testes de integração

Criar testes para:

* `POST /api/documents/{id}/process` com JWT válido.
* Usuário sem autenticação recebe 401.
* Operator não processa documento de outro usuário.
* Documento processado gera registro em `EXTRACTED_DATA`.
* Documento processado gera logs.
* Documento inválido retorna 404.
* Documento sem texto suficiente fica como `FAILED`.

## Critérios de aceite

A task será considerada concluída quando:

* Endpoint `/api/documents/{id}/process` estiver funcionando.
* Documento sair de `PENDING` para `PROCESSING`.
* Texto for extraído de PDF digital.
* Documento for classificado por regra.
* Indexadores forem extraídos por regex.
* Resultado for salvo em `EXTRACTED_DATA`.
* Status final for `PROCESSED` ou `FAILED`.
* Logs forem registrados em todas as etapas.
* OCR não for implementado ainda, apenas sinalizado quando necessário.
* IA não for implementada ainda.
* Controller continuar sem lógica de negócio.
* Código respeitar Clean Architecture.
# Task — Criar Pipeline Inicial de Processamento Documental

## Contexto

O projeto Doclyn já possui:

* Autenticação JWT
* PostgreSQL
* MinIO
* Upload de documentos PDF
* Registro de metadados em banco
* Status inicial `PENDING`
* Logs de processamento
* Entidades `Document`, `ExtractedData` e `ProcessingLog`

Agora será criado o pipeline inicial de processamento documental.

Nesta etapa, o objetivo é processar documentos PDF digitais usando extração simples de texto, classificação por regra e extração inicial por regex.

## Importante

Nesta task ainda não implementar IA real.

Nesta task ainda não implementar OCR completo.

O OCR será implementado posteriormente como fallback para documentos escaneados ou PDFs sem texto pesquisável.

## Fluxo esperado

```txt
PENDING
↓
PROCESSING
↓
Extração de texto simples do PDF
↓
Classificação mockada ou por regra
↓
Extração inicial por regex
↓
PROCESSED ou FAILED
```

## Objetivo

Implementar um pipeline funcional para processar documentos já enviados ao sistema.

O pipeline deve:

* Buscar documento com status `PENDING`
* Atualizar status para `PROCESSING`
* Baixar o PDF do MinIO
* Extrair texto simples do PDF
* Classificar o tipo documental por regra
* Extrair indexadores iniciais por regex
* Salvar os dados extraídos em `EXTRACTED_DATA`
* Atualizar status para `PROCESSED`
* Registrar logs de cada etapa
* Em caso de erro, atualizar status para `FAILED`

## Fora do escopo

Não implementar nesta task:

* OCR com Tesseract
* OCR em nuvem
* IA generativa
* OpenAI
* Azure Document Intelligence
* Fila externa
* Reprocessamento manual
* Validação humana dos dados extraídos

## Documento modelo inicial

Usar como referência inicial o documento modelo de teste:

```txt
Relatório Técnico Preliminar de Processo Administrativo
```

Tipo esperado:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

Grupo:

```txt
PROCESSO_ADMINISTRATIVO
```

Subgrupo:

```txt
APURACAO_CONTRATUAL
```

## Indexadores iniciais esperados

Extrair, quando existirem:

* Número do processo
* Número do contrato
* Órgão público
* Empresa contratada
* CNPJ
* Pessoas citadas
* CPF
* Matrícula funcional
* Valores financeiros
* Datas
* Nota fiscal
* Ofício
* E-mails
* Telefones
* Endereço
* CEP
* Agência bancária
* Conta bancária
* Documentos relacionados
* Palavras-chave de classificação

## Exemplo de JSON esperado

```json
{
  "tipoDocumento": "RELATORIO_TECNICO_PRELIMINAR",
  "grupo": "PROCESSO_ADMINISTRATIVO",
  "subgrupo": "APURACAO_CONTRATUAL",
  "numeroProcesso": "2026/98765",
  "numeroContrato": "45/2026",
  "orgao": "Prefeitura Municipal de Vale Verde",
  "empresa": "Soluções Integradas do Centro-Oeste LTDA",
  "cnpj": "12.345.678/0001-99",
  "cpfs": ["123.456.789-00"],
  "matriculas": ["445566"],
  "valores": ["12345.67"],
  "datas": ["14/03/2026", "02/04/2026", "12/06/2026", "28/04/2026"],
  "notaFiscal": "NF-2026-000123",
  "oficio": "112/2026",
  "emails": ["contato@solucoesintegradas.com.br"],
  "telefones": ["(65) 99999-9999"],
  "cep": "78000-000",
  "agencia": "1234",
  "contaCorrente": "56789-0",
  "documentosRelacionados": [
    "Contrato",
    "Nota Fiscal",
    "Ofício",
    "Relatório Técnico",
    "Planilhas",
    "Empenhos",
    "Ordens de Serviço"
  ]
}
```

## Interfaces sugeridas

Criar em `Doclyn.Application/Common/Interfaces`:

```csharp
public interface IDocumentProcessingService
{
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
}
```

```csharp
public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
```

```csharp
public interface IDocumentClassifier
{
    DocumentClassificationResult Classify(string text);
}
```

```csharp
public interface IDocumentIndexer
{
    Dictionary<string, object?> ExtractIndexes(string text, string documentType);
}
```

## Implementações sugeridas

Criar em `Doclyn.Infrastructure`:

```txt
Processing/
  DocumentProcessingService.cs

PDF/
  PdfTextExtractor.cs

Classification/
  RuleBasedDocumentClassifier.cs

Extraction/
  RegexDocumentIndexer.cs
```

## Regras de status

Ao iniciar processamento:

```txt
PENDING → PROCESSING
```

Ao finalizar com sucesso:

```txt
PROCESSING → PROCESSED
```

Ao ocorrer erro:

```txt
PROCESSING → FAILED
```

## Logs obrigatórios

Criar logs em `PROCESSING_LOGS` para:

```txt
ProcessingStarted
TextExtracted
DocumentClassified
IndexesExtracted
ProcessingCompleted
ProcessingFailed
```

Exemplo:

```txt
STEP = TextExtracted
STATUS = Success
MESSAGE = Text extracted from PDF successfully.
```

Não salvar o texto completo do PDF nos logs.

## Extração de texto

Nesta task, usar extração simples de texto para PDF digital.

Bibliotecas possíveis:

```txt
UglyToad.PdfPig
iText
PdfSharp
```

Recomendação para MVP:

```txt
UglyToad.PdfPig
```

Se o texto extraído estiver vazio ou muito pequeno, registrar log:

```txt
Text extraction returned insufficient content. OCR may be required.
```

Nesse caso, marcar o documento como `FAILED` ou manter estratégia configurável.

Para o MVP, pode marcar como `FAILED` com mensagem clara.

## Classificação por regra

Classificar como `RELATORIO_TECNICO_PRELIMINAR` se o texto contiver termos como:

```txt
PROCESSO ADMINISTRATIVO
RELATÓRIO TÉCNICO PRELIMINAR
CONTRATO
FISCALIZAÇÃO CONTRATUAL
PREFEITURA MUNICIPAL
PROCURADORIA JURÍDICA
```

Caso não identifique:

```txt
UNKNOWN
```

## Regex iniciais

### Número do processo

```regex
PROCESSO\\s+ADMINISTRATIVO\\s+N[º°]?\\s*([\\d\\/]+)
```

### Número do contrato

```regex
Contrato\\s+n[º°]?\\s*([\\d\\/]+)
```

### CNPJ

```regex
\\d{2}\\.\\d{3}\\.\\d{3}\\/\\d{4}-\\d{2}
```

### CPF

```regex
\\d{3}\\.\\d{3}\\.\\d{3}-\\d{2}
```

### Matrícula funcional

```regex
matr[íi]cula\\s+funcional\\s+(\\d+)
```

### Valores financeiros

```regex
R\\$\\s*\\d{1,3}(?:\\.\\d{3})*,\\d{2}
```

### Datas

```regex
\\d{2}\\/\\d{2}\\/\\d{4}
```

### E-mail

```regex
[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}
```

### Telefone

```regex
\\(\\d{2}\\)\\s*\\d{4,5}-\\d{4}
```

### CEP

```regex
\\d{5}-\\d{3}
```

### Ofício

```regex
Of[ií]cio\\s+n[º°]?\\s*([\\d\\/-]+)
```

### Nota fiscal

```regex
Nota\\s+Fiscal\\s+n[º°]?\\s*([A-Z0-9\\-]+)
```

## Persistência

Salvar o resultado em `EXTRACTED_DATA.DATA_JSON`.

O campo deve ser armazenado como `JSONB`.

Também atualizar em `DOCUMENTS`:

```txt
DOCUMENT_TYPE
DOCUMENT_STATUS
PROCESSED_AT
UPDATED_AT
```

## Endpoint para iniciar processamento

Criar endpoint protegido:

```http
POST /api/documents/{id}/process
```

Regras:

* Usuário `Operator` só pode processar seus próprios documentos.
* Usuário `Admin` pode processar qualquer documento.
* Documento precisa estar com status `PENDING` ou `FAILED`.
* Se já estiver `PROCESSING`, retornar erro.
* Se já estiver `PROCESSED`, retornar erro ou exigir reprocessamento futuro.

## Response

```json
{
  "documentId": "4c7b845f-7da9-4a64-b1d0-4f15f1bb1f21",
  "status": "Processed",
  "documentType": "RelatorioTecnicoPreliminar",
  "processedAt": "2026-06-16T15:00:00Z"
}
```

## Testes unitários

Criar testes para:

* Classificar relatório técnico preliminar corretamente.
* Retornar `Unknown` quando não reconhecer o texto.
* Extrair número do processo.
* Extrair número do contrato.
* Extrair CNPJ.
* Extrair CPF.
* Extrair datas.
* Extrair valores financeiros.
* Extrair e-mail.
* Extrair telefone.
* Extrair CEP.
* Alterar status para `Processing`.
* Alterar status para `Processed`.
* Alterar status para `Failed` em caso de erro.

## Testes de integração

Criar testes para:

* `POST /api/documents/{id}/process` com JWT válido.
* Usuário sem autenticação recebe 401.
* Operator não processa documento de outro usuário.
* Documento processado gera registro em `EXTRACTED_DATA`.
* Documento processado gera logs.
* Documento inválido retorna 404.
* Documento sem texto suficiente fica como `FAILED`.

## Critérios de aceite

A task será considerada concluída quando:

* Endpoint `/api/documents/{id}/process` estiver funcionando.
* Documento sair de `PENDING` para `PROCESSING`.
* Texto for extraído de PDF digital.
* Documento for classificado por regra.
* Indexadores forem extraídos por regex.
* Resultado for salvo em `EXTRACTED_DATA`.
* Status final for `PROCESSED` ou `FAILED`.
* Logs forem registrados em todas as etapas.
* OCR não for implementado ainda, apenas sinalizado quando necessário.
* IA não for implementada ainda.
* Controller continuar sem lógica de negócio.
* Código respeitar Clean Architecture.
