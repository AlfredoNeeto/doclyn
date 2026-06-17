# Doclyn — Inteligência Documental

Plataforma SaaS de análise documental inteligente. Recebe documentos PDF (inclusive digitalizados), aplica OCR, classifica o tipo com IA, extrai dados estruturados, gera insights e disponibiliza dashboard.

## Projetos

| Projeto | Stack | Descrição |
|---------|-------|-----------|
| [Doclyn](./Doclyn/) | .NET 10, PostgreSQL, MinIO, Hangfire | API REST com Clean Architecture |
| [Doclyn-front](./Doclyn-front/) | React 19, TypeScript, Vite, Tailwind | Interface web SPA |

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Início rápido

### 1. Subir serviços de infraestrutura

```bash
cd Doclyn
docker compose up -d
```

Serviços disponíveis:

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| API (HTTPS) | https://localhost:7292 | — |
| Swagger | https://localhost:7292/swagger | — |
| MinIO Console | http://localhost:9001 | minioadmin / minioadmin |
| Seq | http://localhost:5341 | — |
| Mailpit | http://localhost:8025 | — |

### 2. Configurar MinIO

Acesse http://localhost:9001 com `minioadmin` / `minioadmin` e crie o bucket `doclyn-documents`.

### 3. Rodar a API

```bash
cd Doclyn
dotnet ef database update --project Doclyn.Infrastructure --startup-project Doclyn.Api
dotnet run --project Doclyn.Api/Doclyn.Api.csproj
```

### 4. Rodar o front-end

```bash
cd Doclyn-front
npm install
npm run dev
```

Front-end disponível em http://localhost:5173.

## Funcionalidades

- Cadastro, login, recuperação de senha (JWT + refresh token)
- Upload drag-and-drop de PDF
- OCR para documentos digitalizados
- Classificação documental automática com IA
- Extração de dados estruturados orientada por classe
- Geração de insights e alertas (contrato vencido, CNPJ inválido, etc.)
- Dashboard com métricas de processamento e qualidade
- Download do documento original
- Exclusão lógica
- Reprocessamento e reclassificação
- Catálogo de classes documentais e indexadores

## Testes

```bash
# API — unitários
cd Doclyn
dotnet test

# API — integração
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj

# Front-end — build
cd Doclyn-front
npm run build
```
