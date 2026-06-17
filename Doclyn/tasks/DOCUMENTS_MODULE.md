# DOCUMENTS_MODULE_TASK.md

# Task - Implementar Módulo de Documentos

## Contexto

O projeto **Doclyn** já possui a base inicial funcionando com:

- PostgreSQL em execução.
- MinIO API em execução.
- MinIO Console em execução.
- Seq em execução.
- Mailpit em execução.
- Migrations aplicadas.
- Autenticação JWT implementada.
- Refresh Token implementado.
- Recuperação de senha implementada.
- Serilog configurado.
- Rate limiting configurado.
- MediatR e FluentValidation configurados.
- Entidades documentais já modeladas no Domain.

A próxima etapa é implementar o **módulo de documentos**, responsável por receber arquivos PDF, armazenar o arquivo original no MinIO, registrar os metadados no PostgreSQL e disponibilizar endpoints para consulta.

Nesta task ainda **não** deve ser implementado OCR, IA ou extração de dados.

---

# Objetivo

Criar o módulo inicial de documentos com:

- Upload de PDF.
- Validação do arquivo.
- Cálculo de hash SHA-256.
- Armazenamento do arquivo no MinIO.
- Registro do documento no PostgreSQL.
- Criação de logs iniciais de processamento.
- Listagem de documentos.
- Consulta de detalhes do documento.
- Consulta dos logs do documento.
- Consulta dos dados extraídos, mesmo que inicialmente vazios.

---

# Fora do Escopo

Não implementar nesta task:

- OCR.
- IA.
- Classificação documental.
- Extração de dados.
- Processamento assíncrono.
- Reprocessamento.
- Download do arquivo.
- Preview do PDF.
- Assinatura digital.
- Multi-tenant.

---

# Endpoints

## Upload de Documento

```http
POST /api/documents/upload
```

Endpoint protegido por JWT.

Request: `multipart/form-data`

Campos:

```txt
file: PDF obrigatório
```

Validações:

- Arquivo obrigatório.
- Extensão `.pdf`.
- Content-Type `application/pdf`.
- Tamanho máximo configurável.
- Arquivo não pode estar vazio.

Tamanho máximo sugerido para MVP:

```txt
10 MB
```

Fluxo:

```txt
Usuário autenticado envia PDF
↓
API valida arquivo
↓
Application calcula SHA-256
↓
Infrastructure salva arquivo no MinIO
↓
Application cria registro em DOCUMENTS
↓
Application cria log inicial em PROCESSING_LOGS
↓
API retorna dados do documento criado
```

Response:

```json
{
  "id": "f4d859c4-764d-43e3-9757-86fa83aa6c1a",
  "fileName": "relatorio-tecnico.pdf",
  "fileHash": "sha256-hash",
  "documentType": "Unknown",
  "documentStatus": "Pending",
  "createdAt": "2026-06-15T22:00:00Z"
}
```

---

## Listar Documentos

```http
GET /api/documents
```

Endpoint protegido por JWT.

Regras de acesso:

- `Operator` visualiza apenas os próprios documentos.
- `Admin` visualiza todos os documentos.

Query parameters:

```txt
page
pageSize
status
documentType
from
to
search
```

Response:

```json
{
  "page": 1,
  "pageSize": 10,
  "totalItems": 1,
  "totalPages": 1,
  "items": [
    {
      "id": "f4d859c4-764d-43e3-9757-86fa83aa6c1a",
      "fileName": "relatorio-tecnico.pdf",
      "documentType": "Unknown",
      "documentStatus": "Pending",
      "createdAt": "2026-06-15T22:00:00Z",
      "processedAt": null
    }
  ]
}
```

---

## Detalhar Documento

```http
GET /api/documents/{id}
```

Endpoint protegido por JWT.

Regras de acesso:

- `Operator` só pode visualizar documentos próprios.
- `Admin` pode visualizar qualquer documento.

Response:

```json
{
  "id": "f4d859c4-764d-43e3-9757-86fa83aa6c1a",
  "userId": "7b5a2f15-d5b5-4e1f-b8d2-5ad0bca040fd",
  "fileName": "relatorio-tecnico.pdf",
  "fileHash": "sha256-hash",
  "documentType": "Unknown",
  "documentStatus": "Pending",
  "createdAt": "2026-06-15T22:00:00Z",
  "updatedAt": null,
  "processedAt": null
}
```

Importante: não retornar `StoragePath` diretamente para o front-end.

---

## Consultar Dados Extraídos

```http
GET /api/documents/{id}/extracted-data
```

Endpoint protegido por JWT.

Regras de acesso:

- `Operator` só pode visualizar dados de documentos próprios.
- `Admin` pode visualizar dados de qualquer documento.

Response quando não houver dados:

```json
{
  "documentId": "f4d859c4-764d-43e3-9757-86fa83aa6c1a",
  "data": null,
  "createdAt": null
}
```

Response futura esperada:

```json
{
  "documentId": "f4d859c4-764d-43e3-9757-86fa83aa6c1a",
  "data": {
    "numeroProcesso": "2026/98765",
    "numeroContrato": "45/2026"
  },
  "createdAt": "2026-06-15T22:10:00Z"
}
```

---

## Consultar Logs de Processamento

```http
GET /api/documents/{id}/logs
```

Endpoint protegido por JWT.

Regras de acesso:

- `Operator` só pode visualizar logs de documentos próprios.
- `Admin` pode visualizar logs de qualquer documento.

Response:

```json
[
  {
    "id": "6c2a1c58-5035-456f-bca4-713f2b8b8a9e",
    "step": "Upload",
    "status": "Success",
    "message": "Document uploaded and stored successfully.",
    "createdAt": "2026-06-15T22:00:00Z"
  }
]
```

---

# Estrutura Recomendada

## Application

```txt
Doclyn.Application/
  Documents/
    Upload/
      UploadDocumentCommand.cs
      UploadDocumentHandler.cs
      UploadDocumentValidator.cs
      UploadDocumentResponse.cs

    GetAll/
      GetDocumentsQuery.cs
      GetDocumentsHandler.cs
      GetDocumentsResponse.cs
      DocumentListItemResponse.cs

    GetById/
      GetDocumentByIdQuery.cs
      GetDocumentByIdHandler.cs
      GetDocumentByIdResponse.cs

    GetExtractedData/
      GetExtractedDataQuery.cs
      GetExtractedDataHandler.cs
      GetExtractedDataResponse.cs

    GetLogs/
      GetDocumentLogsQuery.cs
      GetDocumentLogsHandler.cs
      GetDocumentLogResponse.cs
```

## Application Common Interfaces

Criar ou ajustar interfaces em:

```txt
Doclyn.Application/Common/Interfaces
```

Interfaces sugeridas:

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default);
}

public interface IFileHashService
{
    Task<string> ComputeSha256Async(
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
```

Caso já exista algum serviço equivalente, reutilizar e adaptar.

## Infrastructure

```txt
Doclyn.Infrastructure/
  Storage/
    MinioOptions.cs
    MinioFileStorageService.cs
    FileHashService.cs
```

## Api

```txt
Doclyn.Api/
  Controllers/
    DocumentsController.cs
```

---

# Storage no MinIO

## Bucket

Usar bucket privado:

```txt
doclyn-documents
```

## Object Name

Padrão sugerido:

```txt
documents/{userId}/{documentId}/original.pdf
```

Exemplo:

```txt
documents/7b5a2f15-d5b5-4e1f-b8d2-5ad0bca040fd/f4d859c4-764d-43e3-9757-86fa83aa6c1a/original.pdf
```

O caminho salvo em banco deve ser interno e não deve ser exposto diretamente ao front-end.

---

# Banco de Dados

## DOCUMENTS

Ao fazer upload, criar registro com:

```txt
ID
USER_ID
FILE_NAME
FILE_HASH
STORAGE_PATH
DOCUMENT_TYPE = UNKNOWN
DOCUMENT_STATUS = PENDING
CREATED_AT
UPDATED_AT = NULL
PROCESSED_AT = NULL
```

## PROCESSING_LOGS

Ao fazer upload, criar pelo menos um log:

```txt
DOCUMENT_ID
STEP = Upload
STATUS = Success
MESSAGE = Document uploaded and stored successfully.
CREATED_AT
```

Em caso de erro, registrar quando possível:

```txt
STEP = Upload
STATUS = Failed
MESSAGE = Error message sanitized.
```

---

# Regras de Negócio

## RN01 - Apenas PDF

O sistema deve aceitar apenas arquivos PDF.

## RN02 - Documento inicia como pendente

Todo documento enviado deve iniciar com status:

```txt
Pending
```

## RN03 - Tipo inicial desconhecido

Todo documento enviado deve iniciar com tipo:

```txt
Unknown
```

A classificação será feita em task futura.

## RN04 - Storage privado

O arquivo deve ser salvo em storage privado.

O front-end não deve receber URL pública nem caminho interno do MinIO nesta task.

## RN05 - Usuário comum só acessa seus documentos

Usuários com role `Operator` só podem acessar documentos enviados por eles.

Usuários com role `Admin` podem acessar todos os documentos.

## RN06 - Hash obrigatório

Todo arquivo salvo deve possuir hash SHA-256 calculado e persistido.

---

# Validações

## UploadDocumentValidator

Validar:

- Arquivo não nulo.
- Nome do arquivo não vazio.
- Extensão `.pdf`.
- Content-Type `application/pdf`.
- Tamanho maior que zero.
- Tamanho menor ou igual ao limite configurado.

---

# Configurações

Adicionar ao `appsettings.json`:

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

Em produção, `AccessKey` e `SecretKey` devem vir de variáveis de ambiente ou secret manager.

---

# Logs

Usar Serilog para registrar:

- Início do upload.
- Validação rejeitada.
- Upload salvo no MinIO.
- Registro salvo no banco.
- Erros inesperados.

Não registrar conteúdo do PDF.

Não registrar dados sensíveis extraídos.

---

# Erros Esperados

## Arquivo inválido

```http
400 Bad Request
```

```json
{
  "message": "Only PDF files are allowed."
}
```

## Arquivo acima do limite

```http
400 Bad Request
```

```json
{
  "message": "File size exceeds the allowed limit."
}
```

## Documento não encontrado

```http
404 Not Found
```

```json
{
  "message": "Document not found."
}
```

## Acesso negado

```http
403 Forbidden
```

```json
{
  "message": "Access denied."
}
```

---

# Testes Recomendados

## UnitTests

Criar testes para:

- Upload rejeita arquivo nulo.
- Upload rejeita arquivo que não é PDF.
- Upload rejeita arquivo vazio.
- Upload rejeita arquivo acima do limite.
- Upload cria documento com status `Pending`.
- Upload cria documento com tipo `Unknown`.
- Operator não pode acessar documento de outro usuário.
- Admin pode acessar documento de qualquer usuário.

## IntegrationTests

Criar testes para:

- `POST /api/documents/upload` com JWT válido.
- `POST /api/documents/upload` sem JWT retorna 401.
- `GET /api/documents` retorna apenas documentos do usuário Operator.
- `GET /api/documents/{id}` respeita autorização.
- `GET /api/documents/{id}/logs` retorna logs.
- `GET /api/documents/{id}/extracted-data` retorna null quando não há extração.

---

# Critérios de Aceite

A task será considerada concluída quando:

- Endpoint de upload estiver funcionando.
- Arquivo PDF for salvo no MinIO.
- Registro for criado em `DOCUMENTS`.
- Log inicial for criado em `PROCESSING_LOGS`.
- Hash SHA-256 for salvo.
- Documento iniciar com status `Pending`.
- Documento iniciar com tipo `Unknown`.
- Endpoints de listagem, detalhe, logs e dados extraídos existirem.
- Regras de acesso por role estiverem funcionando.
- Nenhum caminho interno do MinIO for exposto no retorno da API.
- Testes principais estiverem implementados.
- OpenAPI refletir os endpoints do módulo.
