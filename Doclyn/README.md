# Doclyn — API

Plataforma de inteligência documental. Recebe documentos PDF, aplica OCR, classifica com IA, extrai dados estruturados, gera insights e disponibiliza dashboard.

## Stack

- .NET 10
- ASP.NET Core (minimal hosting)
- PostgreSQL + EF Core
- MinIO (object storage)
- MediatR + FluentValidation
- JWT + Refresh Token
- Hangfire (background jobs)
- Serilog + Seq
- OpenAPI (Swagger em dev)

## Arquitetura

```
Doclyn.Domain           → Entidades, enums, value objects
Doclyn.Application      → Casos de uso, interfaces, DTOs, MediatR handlers
Doclyn.Infrastructure   → Banco, MinIO, OCR, IA, Jobs
Doclyn.Api              → HTTP host, controllers, middleware
```

## Pré-requisitos

- .NET 10 SDK
- Docker (PostgreSQL, MinIO, Seq, Mailpit)

## Ambiente local

```bash
# Iniciar serviços
docker compose up -d

# Criar bucket no MinIO (http://localhost:9001)
#   Bucket: doclyn-documents
#   Credenciais: minioadmin / minioadmin

# Aplicar migrations
dotnet ef database update \
  --project Doclyn.Infrastructure \
  --startup-project Doclyn.Api

# Rodar API
dotnet run --project Doclyn.Api/Doclyn.Api.csproj
```

| Serviço | URL |
|---------|-----|
| API (HTTPS) | https://localhost:7292 |
| API (HTTP) | http://localhost:5172 |
| Swagger | https://localhost:7292/swagger |
| MinIO Console | http://localhost:9001 |
| Seq | http://localhost:5341 |
| Mailpit | http://localhost:8025 |

## Endpoints

### Auth
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/register` | Criar conta |
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/refresh-token` | Renovar token |
| POST | `/api/auth/logout` | Sair |
| GET | `/api/auth/me` | Dados do usuário |
| POST | `/api/auth/forgot-password` | Solicitar código |
| POST | `/api/auth/verify-reset-code` | Validar código |
| POST | `/api/auth/reset-password` | Redefinir senha |

### Documentos
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/documents/upload` | Upload de PDF |
| GET | `/api/documents` | Listar documentos |
| GET | `/api/documents/{id}` | Detalhar documento |
| GET | `/api/documents/{id}/download` | Baixar PDF original |
| GET | `/api/documents/{id}/extracted-data` | Dados extraídos |
| GET | `/api/documents/{id}/review-fields` | Campos para revisão |
| GET | `/api/documents/{id}/insights` | Insights do documento |
| POST | `/api/documents/{id}/generate-insights` | Gerar insights |
| GET | `/api/documents/{id}/logs` | Logs de processamento |
| POST | `/api/documents/{id}/process` | Iniciar processamento |
| POST | `/api/documents/{id}/reprocess` | Reprocessar |
| POST | `/api/documents/{id}/reclassify` | Reclassificar |
| DELETE | `/api/documents/{id}` | Exclusão lógica |
| POST | `/api/documents/{id}/restore` | Restaurar documento |
| POST | `/api/documents/reprocess-batch` | Reprocessar em lote |
| POST | `/api/documents/reprocess-by-filter` | Reprocessar por filtro |

### Dashboard
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/dashboard/summary` | Resumo do dashboard |

### Classes documentais
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/document-classes` | Listar classes |
| GET | `/api/document-classes/{id}` | Detalhar classe |
| GET | `/api/document-classes/{id}/examples` | Exemplos da classe |
| GET | `/api/document-classes/top` | Classes mais usadas |
| GET | `/api/document-classes/{id}/indexers` | Indexadores da classe |
| POST | `/api/document-classes/{id}/indexers` | Criar indexador (admin) |
| PUT | `/api/document-classes/{id}/indexers/{iid}` | Atualizar indexador (admin) |
| DELETE | `/api/document-classes/{id}/indexers/{iid}` | Desativar indexador (admin) |

### Infra
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | Health check |

## Testes

```bash
dotnet test                                        # unitários
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj  # integração
```
