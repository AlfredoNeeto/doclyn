Segue o prompt para implementar a task de storage com MinIO:

# Task — Implementar Storage de Documentos com MinIO

## Contexto

Estou desenvolvendo o **Doclyn**, uma API .NET 10 com Clean Architecture para inteligência documental.

O projeto já possui:

* PostgreSQL rodando
* MinIO API rodando
* MinIO Console rodando
* Seq rodando
* Mailpit rodando
* Migrations aplicadas
* Autenticação JWT funcionando
* Módulo inicial de documentos concluído
* Entidades `Document`, `ExtractedData` e `ProcessingLog` já existentes
* `DoclynDbContext` configurado
* Convenção de banco em `UPPER_SNAKE_CASE`
* Serilog configurado
* MediatR e FluentValidation configurados

Agora preciso implementar corretamente o storage de documentos com **MinIO**.

## Objetivo da task

Implementar o fluxo completo de armazenamento do PDF original:

```txt
validar PDF
↓
calcular hash SHA-256
↓
salvar no MinIO
↓
registrar metadados no PostgreSQL
↓
criar status PENDING
```

## Requisitos Funcionais

### Upload de documento

O endpoint existente:

```http
POST /api/documents/upload
```

deve receber um arquivo PDF via `multipart/form-data`.

O upload deve:

1. Validar se o arquivo foi enviado.
2. Validar se o arquivo não está vazio.
3. Validar se a extensão é `.pdf`.
4. Validar se o `Content-Type` é `application/pdf`.
5. Validar o tamanho máximo configurado.
6. Calcular o hash SHA-256 do arquivo.
7. Criar um `DocumentId`.
8. Gerar o caminho interno do arquivo no MinIO.
9. Salvar o arquivo no bucket privado.
10. Registrar os metadados na tabela `DOCUMENTS`.
11. Criar um registro inicial em `PROCESSING_LOGS`.
12. Retornar os dados básicos do documento criado.

## Requisitos Técnicos

### Storage

Usar MinIO como storage S3-compatible.

Bucket padrão:

```txt
doclyn-documents
```

Object name padrão:

```txt
documents/{userId}/{documentId}/original.pdf
```

Exemplo:

```txt
documents/9c1ff35d-7bc0-4a68-9f16-432df16d40e2/4c7b845f-7da9-4a64-b1d0-4f15f1bb1f21/original.pdf
```

O bucket deve ser criado automaticamente se não existir.

O bucket deve ser privado.

Não expor URL pública nem caminho interno do MinIO no response da API.

### Banco de dados

Ao concluir o upload, criar registro em `DOCUMENTS` com:

```txt
ID = documentId
USER_ID = currentUserId
FILE_NAME = nome original do arquivo
FILE_HASH = hash SHA-256
STORAGE_PATH = object name do MinIO
DOCUMENT_TYPE = UNKNOWN
DOCUMENT_STATUS = PENDING
CREATED_AT = UTC now
UPDATED_AT = null
PROCESSED_AT = null
```

Também criar log inicial em `PROCESSING_LOGS`:

```txt
DOCUMENT_ID = documentId
STEP = Upload
STATUS = Success
MESSAGE = Document uploaded and stored successfully.
CREATED_AT = UTC now
```

## Configuração

Adicionar ou validar as configurações no `appsettings.json`:

```json
{
  "Storage": {
    "Provider": "Minio",
    "BucketName": "doclyn-documents",
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "UseSsl": false
  },
  "Documents": {
    "MaxUploadSizeInMb": 10
  }
}
```

Criar classes de options:

```csharp
public sealed class StorageOptions
{
    public string Provider { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
}
```

```csharp
public sealed class DocumentOptions
{
    public int MaxUploadSizeInMb { get; set; } = 10;
}
```

## Interfaces

Criar ou ajustar no projeto `Doclyn.Application`:

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IFileHashService
{
    Task<string> ComputeSha256Async(
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
```

## Implementações

Criar no projeto `Doclyn.Infrastructure`:

```txt
Storage/
  MinioFileStorageService.cs
  FileHashService.cs
  StorageOptions.cs
  DocumentOptions.cs
```

### MinioFileStorageService

Responsabilidades:

* Conectar no MinIO.
* Criar bucket se não existir.
* Fazer upload do arquivo.
* Retornar o object name salvo.
* Não retornar URL pública.
* Lançar exceção controlada em caso de falha.

### FileHashService

Responsabilidades:

* Calcular SHA-256 do stream.
* Garantir que o stream volte para posição inicial após o cálculo, quando possível.
* Retornar hash em hexadecimal lowercase.

## Handler

Ajustar o `UploadDocumentHandler` para orquestrar:

```txt
validar usuário atual
↓
validar arquivo
↓
calcular hash
↓
gerar documentId
↓
gerar objectName
↓
salvar no MinIO
↓
criar Document
↓
criar ProcessingLog
↓
salvar alterações no banco
↓
retornar response
```

## Controller

O `DocumentsController` deve continuar fino.

Ele deve:

* Receber `IFormFile`.
* Encaminhar para o command.
* Retornar response adequada.
* Não conter lógica de storage.
* Não conter lógica de banco.
* Não conter cálculo de hash.

## Response esperada

```json
{
  "id": "4c7b845f-7da9-4a64-b1d0-4f15f1bb1f21",
  "fileName": "documento.pdf",
  "fileHash": "hash-sha256",
  "documentType": "Unknown",
  "documentStatus": "Pending",
  "createdAt": "2026-06-16T12:00:00Z"
}
```

Não retornar:

```txt
storagePath
minioUrl
bucketName
accessKey
secretKey
```

## Logs

Adicionar logs com Serilog para:

* Início do upload.
* Arquivo validado.
* Hash calculado.
* Upload enviado ao MinIO.
* Documento registrado no banco.
* Falha ao salvar no MinIO.
* Falha ao registrar no banco.

Não logar:

* Conteúdo do PDF.
* Dados sensíveis.
* AccessKey.
* SecretKey.
* JWT.
* Refresh Token.

## Validações

O validator deve rejeitar:

* Arquivo nulo.
* Arquivo vazio.
* Arquivo sem nome.
* Arquivo que não seja `.pdf`.
* Arquivo com content-type diferente de `application/pdf`.
* Arquivo acima do limite configurado.

Mensagens sugeridas:

```txt
File is required.
File cannot be empty.
Only PDF files are allowed.
File size exceeds the allowed limit.
```

## Tratamento de erro

Caso falhe no upload do MinIO:

```http
500 Internal Server Error
```

Mensagem sanitizada:

```json
{
  "message": "Could not store the document."
}
```

Caso falhe validação:

```http
400 Bad Request
```

Caso usuário não esteja autenticado:

```http
401 Unauthorized
```

## Cuidados Importantes

Ao calcular o hash, o stream será lido.

Antes de enviar para o MinIO, garantir:

```csharp
if (fileStream.CanSeek)
{
    fileStream.Position = 0;
}
```

Caso contrário, copiar para um `MemoryStream` ou stream temporário.

Evitar carregar arquivos grandes na memória futuramente, mas para o MVP com limite de 10 MB é aceitável.

## Testes Unitários

Criar testes para:

* Hash SHA-256 é gerado corretamente.
* Stream volta para posição inicial após cálculo.
* Upload rejeita arquivo vazio.
* Upload rejeita arquivo não PDF.
* Upload rejeita arquivo acima do limite.
* Handler cria documento com status `Pending`.
* Handler cria documento com tipo `Unknown`.
* Handler chama `IFileStorageService`.
* Handler cria `ProcessingLog` inicial.

## Testes de Integração

Criar ou preparar testes para:

* Upload com JWT válido.
* Upload sem JWT retorna 401.
* Upload de PDF válido cria registro em `DOCUMENTS`.
* Upload de PDF válido cria log em `PROCESSING_LOGS`.
* Upload de arquivo inválido retorna 400.

Caso MinIO real ainda não seja usado nos testes de integração, usar mock/fake de `IFileStorageService`.

## Critérios de Aceite

A task será considerada concluída quando:

* PDF válido for salvo no MinIO.
* Bucket for criado automaticamente se não existir.
* Hash SHA-256 for calculado e salvo.
* Registro for criado em `DOCUMENTS`.
* Status inicial for `PENDING`.
* Tipo inicial for `UNKNOWN`.
* Log inicial for criado em `PROCESSING_LOGS`.
* API não expuser `StoragePath`.
* Endpoint exigir autenticação JWT.
* Validações de arquivo estiverem funcionando.
* Logs estiverem claros e sem dados sensíveis.
* Código respeitar Clean Architecture.
* Controller continuar sem lógica de negócio.
