# SOFT_DELETE_BACKEND_TASK.md

# Task — Implementar Exclusão Lógica no Backend

## Contexto

O Doclyn possui entidades importantes como usuários, documentos, classes documentais, indexadores, sugestões e insights.

Como o sistema trabalha com documentos, auditoria e rastreabilidade, não é recomendado excluir registros fisicamente do banco em operações comuns.

O objetivo desta task é implementar **exclusão lógica** no backend.

---

# Objetivo

Adicionar suporte a soft delete nas entidades principais.

Ao excluir um registro, o sistema deve apenas marcar o item como excluído, mantendo o histórico no banco.

Fluxo:

```txt
Usuário solicita exclusão
↓
Sistema marca como deletado
↓
Registro deixa de aparecer nas consultas comuns
↓
Registro continua disponível para auditoria
```

---

# Entidades Afetadas

Aplicar inicialmente em:

```txt
Document
DocumentClass
DocumentClassIndexer
DocumentClassIndexerSuggestion
DocumentInsight
ProcessingLog
```

Opcionalmente:

```txt
User
```

Para `User`, avaliar com cuidado, pois pode impactar autenticação.

---

# Modelo Base

Criar ou ajustar uma entidade base:

```csharp
public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public void Delete(Guid deletedByUserId)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = deletedByUserId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
    }
}
```

---

# Banco de Dados

Adicionar colunas:

```sql
IS_DELETED
DELETED_AT
DELETED_BY_USER_ID
```

Nas tabelas aplicáveis.

Manter padrão do projeto:

```txt
UPPER_SNAKE_CASE
```

---

# Entity Framework

Configurar filtro global:

```csharp
builder.Entity<Document>()
    .HasQueryFilter(x => !x.IsDeleted);
```

Aplicar para todas as entidades soft deletable.

O filtro deve impedir que registros deletados apareçam em consultas comuns.

---

# Consultas Administrativas

Criar forma explícita de consultar deletados quando necessário:

```csharp
IgnoreQueryFilters()
```

Usar apenas em casos administrativos ou auditoria.

---

# Endpoints

## Excluir documento

```http
DELETE /api/documents/{id}
```

Comportamento:

```txt
Marca documento como deletado
Não remove arquivo do MinIO inicialmente
Não remove ExtractedData
Não remove Logs
```

Response:

```http
204 No Content
```

---

## Restaurar documento

```http
POST /api/documents/{id}/restore
```

Response:

```http
204 No Content
```

Restrito a:

```txt
Admin
```

---

# Regras de Negócio

## Documento

Ao excluir documento:

```txt
IS_DELETED = true
DELETED_AT = agora
DELETED_BY_USER_ID = usuário atual
```

Não excluir fisicamente:

```txt
Arquivo no MinIO
Dados extraídos
Logs
Insights
```

Motivo:

```txt
Auditoria
Rastreabilidade
Histórico
```

---

## Classes documentais

Não permitir excluir classe documental se houver documentos ativos vinculados.

Opções:

```txt
1. Bloquear exclusão
2. Permitir apenas desativação
```

Para o MVP, preferir:

```txt
Desativação lógica
```

---

## Indexadores

Ao excluir indexador:

```txt
IS_DELETED = true
IS_ACTIVE = false
```

Não remover do banco para preservar histórico de extrações antigas.

---

# Segurança

Regras:

```txt
Operator:
- Pode excluir apenas seus próprios documentos

Admin:
- Pode excluir qualquer documento
- Pode restaurar documentos
- Pode excluir/desativar classes e indexadores
```

---

# Logs

Registrar logs de domínio/processamento:

```txt
DocumentDeleted
DocumentRestored
DocumentClassDeleted
IndexerDeleted
```

Não apagar logs antigos.

---

# Impacto nas Consultas

As seguintes listagens devem ignorar registros deletados:

```txt
GET /api/documents
GET /api/document-classes
GET /api/document-classes/{id}/indexers
GET /api/suggestions
```

Detalhes por ID devem retornar:

```http
404 Not Found
```

quando o registro estiver deletado, exceto em endpoints administrativos.

---

# Impacto no Front-end

O front deve tratar exclusão como:

```txt
Remover da listagem
Mostrar toast de sucesso
```

Mensagem:

```txt
Documento excluído com sucesso.
```

---

# Migration

Criar migration:

```txt
AddSoftDeleteColumns
```

Adicionar colunas com valores padrão:

```sql
IS_DELETED BOOLEAN NOT NULL DEFAULT FALSE
DELETED_AT TIMESTAMP NULL
DELETED_BY_USER_ID UUID NULL
```

---

# Testes Unitários

Criar testes para:

* Excluir entidade marca `IsDeleted`.
* Excluir entidade define `DeletedAt`.
* Excluir entidade define `DeletedByUserId`.
* Restaurar entidade limpa flags.
* Excluir entidade já excluída não quebra.

---

# Testes de Integração

Criar testes para:

* Documento excluído não aparece na listagem.
* Documento excluído retorna 404 no detalhe.
* Admin consegue restaurar documento.
* Operator não exclui documento de outro usuário.
* Admin exclui documento de qualquer usuário.
* Filtro global funciona.

---

# Critérios de Aceite

A task será considerada concluída quando:

* Entidades principais suportarem soft delete.
* Migration for criada.
* EF Core aplicar filtros globais.
* Endpoint DELETE de documento funcionar.
* Endpoint restore funcionar para Admin.
* Registros deletados não aparecerem nas consultas comuns.
* Logs não forem apagados.
* Arquivos no MinIO não forem removidos fisicamente.
* Regras de autorização forem respeitadas.
* Testes principais estiverem implementados.
