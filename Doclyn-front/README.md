# Doclyn — Front-end

Interface web da plataforma Doclyn. Upload de PDFs, acompanhamento de processamento, dashboard, visualização de dados extraídos, insights e gestão documental.

## Stack

| Tecnologia | Versão |
|-----------|--------|
| React | 19 |
| TypeScript | 5.x |
| Vite | 8 |
| Tailwind CSS | 4 |
| shadcn/ui | — (componentes customizados) |
| React Router DOM | 7 |
| TanStack Query | 5 |
| Axios | 1 |
| React Hook Form | 7 |
| Zod | 4 |
| Lucide React | — |

## Pré-requisitos

- Node.js 20+
- API Doclyn rodando (https://localhost:7292)

## Instalação

```bash
npm install
```

## Configuração

Arquivo `.env` na raiz:

```env
VITE_API_BASE_URL=/api
```

O Vite faz proxy de `/api` para `https://localhost:7292` em desenvolvimento.

## Executar

```bash
npm run dev      # Desenvolvimento (http://localhost:5173)
npm run build    # Build de produção
npm run preview  # Preview do build
```

## Estrutura

```
src/
  app/               # Providers (auth, query, theme) + router
  components/
    layout/          # Shell, sidebar, header, user menu
    shared/          # Componentes reutilizáveis
    ui/              # Primitivos (button, input, card, badge, etc.)
  features/
    auth/            # Login, cadastro, recuperação de senha
    dashboard/       # Dashboard com métricas e atenção necessária
    upload/          # Upload drag-and-drop de PDF
    documents/       # Listagem, detalhe, download, exclusão
    document-classes/ # Catálogo, detalhe, indexadores
    settings/        # Preferências de conta e tema
  hooks/             # Hooks compartilhados
  lib/               # Constants, formatters, mappers, download-file
  schemas/           # Validação Zod
  services/          # API client (Axios) + mocks
  types/             # Tipos TypeScript
```

## Módulos

| Módulo | Status |
|--------|--------|
| Login / Cadastro / Recuperação | API real |
| Dashboard | API real (`/api/dashboard/summary`) |
| Upload de documentos | API real |
| Listagem de documentos | API real com polling |
| Detalhe do documento | API real (dados, revisão, insights, logs) |
| Download de documento | API real (`/api/documents/{id}/download`) |
| Exclusão de documento | API real (soft delete) |
| Reprocessamento | API real |
| Classes documentais | API real |
| Indexadores | API real (admin) |
| Configurações | Preferências locais |

## Endpoints consumidos

| Método | Rota |
|--------|------|
| POST | `/api/Auth/login` |
| POST | `/api/Auth/register` |
| POST | `/api/Auth/forgot-password` |
| POST | `/api/Auth/verify-reset-code` |
| POST | `/api/Auth/reset-password` |
| GET | `/api/Auth/me` |
| POST | `/api/Auth/logout` |
| POST | `/api/Auth/refresh-token` |
| GET | `/api/dashboard/summary` |
| POST | `/api/Documents/upload` |
| GET | `/api/Documents` |
| GET | `/api/Documents/{id}` |
| GET | `/api/Documents/{id}/download` |
| GET | `/api/Documents/{id}/extracted-data` |
| GET | `/api/Documents/{id}/review-fields` |
| GET | `/api/Documents/{id}/insights` |
| GET | `/api/Documents/{id}/logs` |
| POST | `/api/Documents/{id}/reprocess` |
| DELETE | `/api/Documents/{id}` |
| GET | `/api/document-classes` |
| GET | `/api/document-classes/{id}` |
| GET | `/api/document-classes/{id}/indexers` |
| POST | `/api/document-classes/{id}/indexers` |
| DELETE | `/api/document-classes/{id}/indexers/{iid}` |
