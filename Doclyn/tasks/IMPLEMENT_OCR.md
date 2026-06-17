# Task — Implementar OCR com Tesseract e Reprocessamento em Lote

## Contexto

O Doclyn já possui:

* Upload de PDFs
* Storage no MinIO
* PostgreSQL
* Status de documentos
* Logs de processamento
* Extração simples de texto para PDFs digitais
* Classificação por regra
* Extração por regex
* Persistência dos dados extraídos em JSONB
* Pipeline rastreável de processamento

Agora será implementado OCR para documentos escaneados e reprocessamento em lote.

## Objetivo

Implementar OCR como fallback quando a extração simples do PDF não retornar texto suficiente.

Também implementar reprocessamento em lote para documentos com status `FAILED`, documentos que exigem OCR ou documentos selecionados manualmente.

## Decisão técnica

Usar:

```txt
Tesseract OCR
```

Motivos:

* Open-source
* Sem custo por página
* Compatível com Docker/Linux
* Adequado para MVP
* Bom para demonstrar conhecimento técnico
* Pode ser substituído futuramente por Azure Document Intelligence, AWS Textract ou Google Document AI

## Fluxo OCR

```txt
Baixar PDF do MinIO
↓
Tentar extração simples de texto
↓
Se texto for suficiente, seguir pipeline normal
↓
Se texto for insuficiente, aplicar OCR
↓
Extrair texto via OCR
↓
Classificar documento
↓
Extrair indexadores
↓
Salvar JSONB
↓
Atualizar status
↓
Registrar logs
```

## Critério para acionar OCR

Acionar OCR quando:

```txt
texto extraído for nulo
texto extraído estiver vazio
texto extraído tiver menos de 100 caracteres
texto extraído não possuir palavras-chave mínimas
```

Palavras-chave mínimas para o documento modelo:

```txt
PROCESSO ADMINISTRATIVO
RELATÓRIO TÉCNICO
CONTRATO
PREFEITURA
CNPJ
```

## Interface

Criar ou ajustar em `Doclyn.Application/Common/Interfaces`:

```csharp
public interface IOcrService
{
    Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default);
}
```

Criar também:

```csharp
public interface IPdfToImageConverter
{
    Task<IReadOnlyCollection<OcrPageImage>> ConvertAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed record OcrPageImage(
    int PageNumber,
    byte[] ImageBytes);
```

## Implementações

Criar em `Doclyn.Infrastructure`:

```txt
OCR/
  TesseractOcrService.cs
  PdfToImageConverter.cs
  OcrOptions.cs
```

## OcrOptions

Adicionar configuração:

```json
{
  "Ocr": {
    "Enabled": true,
    "Language": "por",
    "TessDataPath": "./tessdata",
    "MinimumTextLength": 100,
    "MaxPages": 20,
    "Dpi": 300
  }
}
```

## Idioma

Usar inicialmente:

```txt
por
```

para português.

Permitir evolução futura para:

```txt
por+eng
```

## Conversão de PDF para imagem

Como o Tesseract trabalha melhor com imagens, converter cada página do PDF para imagem antes do OCR.

Requisitos:

* Converter páginas em 300 DPI.
* Processar página por página.
* Respeitar limite máximo de páginas.
* Concatenar o texto extraído.
* Registrar log por etapa, não por conteúdo.

## Logs obrigatórios

Registrar em `PROCESSING_LOGS`:

```txt
OcrRequired
OcrStarted
OcrPageProcessed
OcrCompleted
OcrFailed
BatchReprocessStarted
BatchReprocessCompleted
BatchReprocessFailed
```

Não salvar o texto completo do OCR nos logs.

## Status

Manter os status atuais se quiser simplicidade:

```txt
PENDING
PROCESSING
PROCESSED
FAILED
```

Recomendação opcional:

Adicionar status futuro:

```txt
OCR_REQUIRED
```

Para o MVP, pode continuar usando `FAILED` com log claro.

## Atualização do pipeline

Atualizar `DocumentProcessingService`:

```txt
PROCESSING
↓
Extrair texto simples
↓
Validar qualidade do texto
↓
Se insuficiente, chamar IOcrService
↓
Validar texto OCR
↓
Classificar documento
↓
Extrair indexadores
↓
Persistir dados
↓
PROCESSED
```

Se OCR falhar:

```txt
FAILED
```

## Reprocessamento individual

Criar endpoint:

```http
POST /api/documents/{id}/reprocess
```

Regras:

* Endpoint protegido por JWT.
* `Operator` só pode reprocessar seus próprios documentos.
* `Admin` pode reprocessar qualquer documento.
* Documento não pode estar `PROCESSING`.
* Ao reprocessar, limpar ou sobrescrever dados extraídos anteriores.
* Registrar log de reprocessamento.

## Reprocessamento em lote

Criar endpoint:

```http
POST /api/documents/reprocess-batch
```

Request:

```json
{
  "documentIds": [
    "7c77fc12-3be4-4f0c-b791-97a9b73cf0b0",
    "e55398f2-85ed-422a-a3d2-c882161e6991"
  ]
}
```

Response:

```json
{
  "requested": 2,
  "enqueued": 2,
  "skipped": 0
}
```

## Reprocessamento por filtro

Criar endpoint opcional:

```http
POST /api/documents/reprocess-by-filter
```

Request:

```json
{
  "status": "Failed",
  "documentType": "Unknown",
  "from": "2026-06-01",
  "to": "2026-06-30"
}
```

Response:

```json
{
  "matched": 15,
  "enqueued": 15,
  "skipped": 0
}
```

## Fila

Usar Hangfire.

Cada documento deve gerar um job separado:

```txt
ProcessDocumentJob(documentId)
```

Não criar um único job gigante para o lote inteiro.

Motivo:

* Melhor rastreabilidade
* Retry individual
* Falha isolada
* Melhor escalabilidade
* Dashboard mais claro

## Regras do lote

* Não enfileirar documento já `PROCESSING`.
* Não enfileirar documento inexistente.
* `Operator` só pode enfileirar seus próprios documentos.
* `Admin` pode enfileirar qualquer documento.
* Registrar logs por documento.
* Retornar quantidade de documentos enfileirados e ignorados.

## Concorrência

Evitar dois processamentos simultâneos do mesmo documento.

Antes de iniciar job:

```txt
Verificar status atual
Se PROCESSING, abortar
Caso contrário, alterar para PROCESSING
```

Idealmente usar transação no banco.

## Critérios de aceite

A task estará concluída quando:

* OCR com Tesseract estiver implementado.
* PDF sem texto pesquisável acionar OCR automaticamente.
* Texto OCR seguir para classificação e extração.
* Logs de OCR forem registrados.
* Reprocessamento individual funcionar.
* Reprocessamento em lote funcionar.
* Cada documento do lote gerar job Hangfire separado.
* Documento já em processamento não for enfileirado novamente.
* Dados extraídos anteriores forem sobrescritos ou substituídos de forma controlada.
* O pipeline continuar respeitando Clean Architecture.
