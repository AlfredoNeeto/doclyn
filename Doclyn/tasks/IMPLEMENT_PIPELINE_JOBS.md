# Task — Implementar Pipeline de Processamento Documental com Hangfire, OCR Fallback e Preparação para IA

## Contexto

O Doclyn já possui upload de documentos, storage no MinIO, PostgreSQL, logs, status de documento e dados extraídos persistidos em JSONB.

Agora será implementado um pipeline rastreável de processamento documental.

## Objetivo

Criar o pipeline de processamento assíncrono usando Hangfire, preparando a aplicação para OCR e IA.

## Fluxo esperado

```txt
Documento enviado
↓
Status PENDING
↓
Job Hangfire iniciado
↓
Status PROCESSING
↓
Baixar PDF do MinIO
↓
Extrair texto simples do PDF
↓
Se texto insuficiente, marcar como OCR_REQUIRED ou FAILED
↓
Classificar documento por regra
↓
Extrair indexadores por regex
↓
Salvar JSONB em EXTRACTED_DATA
↓
Registrar logs
↓
Status PROCESSED ou FAILED
```

## Decisão técnica

Usar **Hangfire** nesta etapa.

Motivos:

* Processamento assíncrono simples.
* Persistência dos jobs.
* Retry automático.
* Dashboard para inspeção.
* Boa integração com .NET.
* Suficiente para o MVP.
* Fácil de mover para um Worker separado futuramente.

## Fora do escopo inicial

Não implementar ainda:

* IA real.
* OpenAI.
* Azure Document Intelligence.
* OCR completo com Tesseract.
* RabbitMQ.
* Reprocessamento em lote.
* Validação manual.

## OCR

Nesta task, o OCR ainda não precisa ser implementado por completo.

Implementar apenas a preparação:

```csharp
public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
```

Criar implementação temporária:

```txt
NotImplementedOcrService
```

Quando a extração simples retornar texto insuficiente, registrar log:

```txt
OCR is required for this document, but OCR is not implemented yet.
```

E definir status:

```txt
FAILED
```

ou, se existir enum específico:

```txt
OCR_REQUIRED
```

Para manter simples, usar `FAILED` inicialmente com mensagem clara.

## IA

A IA será implementada depois do pipeline estar estável.

Nesta task, criar apenas interfaces preparatórias:

```csharp
public interface IAiDocumentClassifier
{
    Task<DocumentClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default);
}
```

```csharp
public interface IAiStructuredDataExtractor
{
    Task<Dictionary<string, object?>> ExtractAsync(
        string text,
        string documentType,
        CancellationToken cancellationToken = default);
}
```

Não criar implementação real ainda.

## Hangfire

Configurar Hangfire usando PostgreSQL.

Criar serviço:

```csharp
public interface IDocumentProcessingQueue
{
    void Enqueue(Guid documentId);
}
```

Implementação:

```txt
HangfireDocumentProcessingQueue
```

Ao concluir upload do documento:

```txt
criar Document com status PENDING
↓
salvar no banco
↓
enfileirar ProcessDocumentJob
```

## Job

Criar:

```txt
ProcessDocumentJob
```

Responsabilidade:

```txt
Receber documentId
↓
Chamar IDocumentProcessingService.ProcessAsync(documentId)
```

O job não deve conter regra de negócio complexa.

## Serviço principal

Criar:

```csharp
public interface IDocumentProcessingService
{
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
}
```

Implementação:

```txt
DocumentProcessingService
```

Responsabilidades:

* Buscar documento.
* Validar status.
* Atualizar status para PROCESSING.
* Baixar arquivo do MinIO.
* Extrair texto simples.
* Verificar qualidade do texto.
* Classificar documento por regra.
* Extrair indexadores por regex.
* Salvar dados em EXTRACTED_DATA.
* Registrar logs.
* Atualizar status final.

## Documento modelo inicial

Focar no tipo:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

Indexadores esperados:

* Número do processo
* Número do contrato
* Órgão
* Empresa
* CNPJ
* CPF
* Matrícula funcional
* Valores
* Datas
* Nota fiscal
* Ofício
* E-mails
* Telefones
* CEP
* Agência
* Conta corrente
* Documentos relacionados

## Critérios de aceite

A task estará concluída quando:

* Hangfire estiver configurado.
* Upload enfileirar processamento automaticamente.
* Documento mudar de `PENDING` para `PROCESSING`.
* PDF for baixado do MinIO.
* Texto simples for extraído.
* Documento modelo for classificado por regra.
* Indexadores forem extraídos por regex.
* Dados forem salvos em `EXTRACTED_DATA.DATA_JSON`.
* Logs forem gravados em cada etapa.
* Documento finalizar como `PROCESSED` ou `FAILED`.
* OCR e IA ficarem preparados por interface, mas sem implementação real.
