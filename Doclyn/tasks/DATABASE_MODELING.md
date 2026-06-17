# DATABASE_MODELING.md

# Modelagem de Banco de Dados - Doclyn

## Objetivo

Implementar a camada de persistência do Doclyn utilizando PostgreSQL e Entity Framework Core.

O projeto deverá utilizar convenções específicas para nomenclatura de banco de dados sem impactar os padrões adotados no código C#.

---

# Convenções

## Código C#

Todo o código deverá seguir o padrão nativo do ecossistema .NET.

### Classes

Utilizar:

```txt
PascalCase
```

Exemplos:

```csharp
User
Document
RefreshToken
ExtractedData
ProcessingLog
PasswordResetRequest
```

### Propriedades

Utilizar:

```txt
PascalCase
```

Exemplos:

```csharp
Id
Name
Email
PasswordHash
CreatedAt
UpdatedAt
DocumentType
DocumentStatus
StoragePath
FileHash
```

---

# Convenções da API

Os contratos JSON deverão utilizar:

```txt
camelCase
```

Exemplo:

```json
{
  "fileName": "contracheque.pdf",
  "documentType": "Payslip",
  "documentStatus": "Processed"
}
```

---

# Convenções do Banco

Todas as tabelas deverão utilizar:

```txt
UPPER_SNAKE_CASE
```

Exemplos:

```sql
USERS
DOCUMENTS
EXTRACTED_DATA
PROCESSING_LOGS
REFRESH_TOKENS
PASSWORD_RESET_REQUESTS
```

Todas as colunas deverão utilizar:

```sql
ID
NAME
EMAIL
PASSWORD_HASH
CREATED_AT
UPDATED_AT
DOCUMENT_TYPE
DOCUMENT_STATUS
FILE_HASH
STORAGE_PATH
```

---

# Estratégia

O Entity Framework deve permitir que os desenvolvedores trabalhem exclusivamente com PascalCase.

Exemplo:

```csharp
user.PasswordHash
```

Banco:

```sql
PASSWORD_HASH
```

A conversão deve ocorrer automaticamente.

Não utilizar:

```csharp
[Column("PASSWORD_HASH")]
```

em todas as propriedades.

A convenção deve ser aplicada globalmente.

---

# Estrutura

## DbContext

Criar:

```csharp
ApplicationDbContext
```

Local:

```txt
Doclyn.Infrastructure/Database
```

---

# Configuração das Entidades

Utilizar:

```csharp
IEntityTypeConfiguration<T>
```

Exemplo:

```txt
UserConfiguration
DocumentConfiguration
RefreshTokenConfiguration
ExtractedDataConfiguration
ProcessingLogConfiguration
PasswordResetRequestConfiguration
```

Não configurar entidades diretamente no DbContext.

---

# Convenção Global

Criar uma extensão responsável por converter automaticamente:

```txt
PascalCase
```

para:

```txt
UPPER_SNAKE_CASE
```

Exemplos:

```txt
User
↓
USERS

RefreshToken
↓
REFRESH_TOKENS

PasswordHash
↓
PASSWORD_HASH

CreatedAt
↓
CREATED_AT
```

A convenção deve ser aplicada em:

- Tabelas
- Colunas
- Chaves primárias
- Chaves estrangeiras
- Índices
- Constraints

---

# Entidades Iniciais

## USERS

Representa os usuários do sistema.

```sql
ID
NAME
EMAIL
PASSWORD_HASH
ROLE
IS_ACTIVE
CREATED_AT
UPDATED_AT
```

Índices:

```sql
EMAIL UNIQUE
```

---

## REFRESH_TOKENS

Representa sessões autenticadas.

```sql
ID
USER_ID
TOKEN_HASH
EXPIRES_AT
REVOKED_AT
REPLACED_BY_TOKEN_HASH
CREATED_AT
UPDATED_AT
```

Relacionamentos:

```txt
USER 1:N REFRESH_TOKENS
```

---

## DOCUMENTS

Representa documentos enviados para processamento.

```sql
ID
USER_ID
FILE_NAME
FILE_HASH
STORAGE_PATH
DOCUMENT_TYPE
DOCUMENT_STATUS
CREATED_AT
UPDATED_AT
PROCESSED_AT
```

Relacionamentos:

```txt
USER 1:N DOCUMENTS
```

Índices:

```sql
USER_ID
DOCUMENT_STATUS
DOCUMENT_TYPE
CREATED_AT
```

---

## EXTRACTED_DATA

Armazena os dados estruturados extraídos do documento.

```sql
ID
DOCUMENT_ID
DATA_JSON
CREATED_AT
UPDATED_AT
```

Tipo:

```sql
JSONB
```

Relacionamentos:

```txt
DOCUMENT 1:1 EXTRACTED_DATA
```

---

## PROCESSING_LOGS

Armazena eventos do processamento.

```sql
ID
DOCUMENT_ID
STEP
MESSAGE
STATUS
CREATED_AT
```

Relacionamentos:

```txt
DOCUMENT 1:N PROCESSING_LOGS
```

---

## PASSWORD_RESET_REQUESTS

Responsável pela recuperação de senha.

```sql
ID
USER_ID
CODE_HASH
ATTEMPTS
IS_USED
EXPIRES_AT
CREATED_AT
```

Relacionamentos:

```txt
USER 1:N PASSWORD_RESET_REQUESTS
```

---

# Relacionamentos

```txt
USERS
 ├── REFRESH_TOKENS
 ├── DOCUMENTS
 └── PASSWORD_RESET_REQUESTS

DOCUMENTS
 ├── EXTRACTED_DATA
 └── PROCESSING_LOGS
```

---

# Auditoria

Todas as entidades principais devem possuir:

```sql
CREATED_AT
UPDATED_AT
```

Objetivo:

- Auditoria
- Histórico
- Rastreabilidade

---

# Enumerações

## USER_ROLE

```txt
ADMIN
OPERATOR
```

---

## DOCUMENT_STATUS

```txt
PENDING
PROCESSING
PROCESSED
FAILED
```

---

## DOCUMENT_TYPE

Inicialmente:

```txt
UNKNOWN
RG
CPF
CNH
PAYSLIP
PROOF_OF_ADDRESS
```

Novos tipos poderão ser adicionados futuramente.

---

# JSONB

O PostgreSQL deverá utilizar JSONB para armazenamento flexível dos dados extraídos.

Exemplo:

```json
{
  "name": "Alfredo Neto",
  "cpf": "12345678900",
  "documentNumber": "99999999"
}
```

Motivos:

- Flexibilidade
- Diferentes tipos de documentos
- Melhor evolução do sistema
- Indexação futura

---

# Migrations

As migrations geradas pelo Entity Framework devem respeitar automaticamente:

```sql
CREATE TABLE USERS
(
    ID UUID PRIMARY KEY,
    EMAIL VARCHAR(255) NOT NULL
)
```

Não permitir:

```sql
Users
Email
CreatedAt
```

---

# PostgreSQL

Tecnologias:

```txt
PostgreSQL
Entity Framework Core
Npgsql
```

---

# Configuração do Entity Framework

Implementar:

```txt
ApplicationDbContext
Entity Configurations
Migrations
UPPER_SNAKE_CASE Convention
``

Não utilizar atributos de mapeamento em massa.

Preferir Fluent API.

---

# Performance

Criar índices para:

```sql
USERS.EMAIL

DOCUMENTS.USER_ID
DOCUMENTS.DOCUMENT_STATUS
DOCUMENTS.DOCUMENT_TYPE
DOCUMENTS.CREATED_AT

PROCESSING_LOGS.DOCUMENT_ID

REFRESH_TOKENS.USER_ID
REFRESH_TOKENS.EXPIRES_AT
```

---

# Segurança

Nunca armazenar:

```txt
Senha em texto puro
Refresh Token em texto puro
Código de recuperação em texto puro
```

Salvar apenas:

```txt
PASSWORD_HASH
TOKEN_HASH
CODE_HASH
```

---

# Critérios de Aceite

- Código permanece em PascalCase.
- Banco utiliza UPPER_SNAKE_CASE.
- JSON utiliza camelCase.
- Conversão automática via convenção global.
- Nenhum atributo [Column] necessário.
- Todas as entidades configuradas via Fluent API.
- Migrations geradas corretamente.
- Compatível com PostgreSQL.
- Compatível com futuras entidades do Doclyn.
- Compatível com crescimento do sistema.
