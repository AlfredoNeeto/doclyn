# DOCUMENT_CLASSES_TASK.md

# Task — Implementar Catálogo de Classes Documentais

## Contexto

O Doclyn já possui:

* Upload de documentos
* Armazenamento em MinIO
* PostgreSQL
* OCR com Tesseract
* Extração de texto
* Pipeline de processamento
* Classificação documental via IA
* Extração estruturada via IA
* Extração por Regex
* Persistência dos dados extraídos em JSONB
* Logs de processamento
* Reprocessamento

Atualmente a classificação documental é utilizada apenas durante o processamento.

O sistema ainda não possui um catálogo persistido de tipos documentais conhecidos.

---

# Objetivo

Criar uma camada de conhecimento documental que permita ao sistema armazenar, consultar e reutilizar classes documentais conhecidas.

O objetivo é transformar classificações em entidades persistidas.

Exemplo:

```txt
RELATORIO_TECNICO_PRELIMINAR
CONTRATO_ADMINISTRATIVO
OFICIO
NOTA_FISCAL
PETICAO_JUDICIAL
DOCUMENTO_DESCONHECIDO
```

Essa camada será a base para futuras funcionalidades:

* Indexadores por classe.
* Extração orientada por classe.
* Aprendizado incremental.
* Evolução do catálogo documental.

---

# Conceito

Hoje:

```txt
Documento
↓
IA classifica
↓
Resultado descartado
```

Após esta implementação:

```txt
Documento
↓
IA classifica
↓
Classe documental persistida
↓
Reutilização futura
```

---

# Entidades

## DocumentClass

Representa um tipo documental conhecido pelo sistema.

Exemplos:

```txt
RELATORIO_TECNICO_PRELIMINAR
CONTRATO_ADMINISTRATIVO
OFICIO
NOTA_FISCAL
PETICAO_JUDICIAL
DOCUMENTO_DESCONHECIDO
```

### Estrutura

```csharp
public sealed class DocumentClass
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Group { get; private set; } = string.Empty;

    public string SubGroup { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsSystemDefined { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
}
```

---

## DocumentClassExample

Representa um exemplo real utilizado para compor uma classe documental.

Objetivo:

Permitir rastreabilidade e futura evolução da classificação.

### Estrutura

```csharp
public sealed class DocumentClassExample
{
    public Guid Id { get; private set; }

    public Guid DocumentClassId { get; private set; }

    public Guid DocumentId { get; private set; }

    public decimal Confidence { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
```

---

# Banco de Dados

## DOCUMENT_CLASSES

```sql
ID
NAME
DISPLAY_NAME
GROUP_NAME
SUB_GROUP
DESCRIPTION
IS_SYSTEM_DEFINED
IS_ACTIVE
CREATED_AT
UPDATED_AT
```

### Índices

```sql
NAME UNIQUE
GROUP_NAME
SUB_GROUP
IS_ACTIVE
```

---

## DOCUMENT_CLASS_EXAMPLES

```sql
ID
DOCUMENT_CLASS_ID
DOCUMENT_ID
CONFIDENCE
CREATED_AT
```

### Relacionamentos

```txt
DOCUMENT_CLASS 1:N DOCUMENT_CLASS_EXAMPLES
DOCUMENT 1:N DOCUMENT_CLASS_EXAMPLES
```

---

# Seeds Iniciais

Ao iniciar a aplicação, garantir a existência das seguintes classes:

```txt
RELATORIO_TECNICO_PRELIMINAR

CONTRATO_ADMINISTRATIVO

OFICIO

NOTA_FISCAL

PETICAO_JUDICIAL

DOCUMENTO_DESCONHECIDO
```

Exemplo:

```txt
Group:
ADMINISTRATIVO

SubGroup:
PROCESSO_ADMINISTRATIVO
```

---

# Integração com o Pipeline

Após a classificação pela IA:

Fluxo atual:

```txt
Documento
↓
IA
↓
Tipo documental
↓
Processamento continua
```

Novo fluxo:

```txt
Documento
↓
IA
↓
Tipo documental
↓
Buscar classe documental
↓
Se existir:
    reutilizar
↓
Se não existir:
    criar nova classe
↓
Salvar exemplo
↓
Continuar processamento
```

---

# Serviço de Catálogo

Criar interface:

```csharp
public interface IDocumentClassCatalogService
{
    Task<DocumentClass?> FindByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<DocumentClass> GetOrCreateAsync(
        string name,
        string group,
        string subGroup,
        string description,
        CancellationToken cancellationToken = default);

    Task RegisterExampleAsync(
        Guid documentClassId,
        Guid documentId,
        decimal confidence,
        CancellationToken cancellationToken = default);
}
```

---

# Implementação

Criar:

```txt
Doclyn.Application/
  DocumentClasses/

Doclyn.Infrastructure/
  DocumentClasses/
```

Arquivos sugeridos:

```txt
DocumentClassCatalogService.cs

DocumentClassRepository.cs
```

---

# Atualização da IA

A IA continuará classificando normalmente.

Exemplo:

```json
{
  "documentType": "RELATORIO_TECNICO_PRELIMINAR",
  "group": "ADMINISTRATIVO",
  "subGroup": "PROCESSO_ADMINISTRATIVO",
  "confidence": 0.98
}
```

Após isso:

```txt
Buscar classe documental
↓
Persistir se necessário
↓
Registrar exemplo
```

---

# Atualização do JSONB

Adicionar metadados de classificação.

Exemplo:

```json
{
  "classification": {
    "documentClassId": "guid",
    "documentType": "RELATORIO_TECNICO_PRELIMINAR",
    "group": "ADMINISTRATIVO",
    "subGroup": "PROCESSO_ADMINISTRATIVO",
    "confidence": 0.98
  }
}
```

---

# Logs

Criar logs:

```txt
DocumentClassFound

DocumentClassCreated

DocumentClassExampleRegistered
```

Exemplo:

```txt
Class RELATORIO_TECNICO_PRELIMINAR reused.
```

```txt
New document class created.
```

---

# Endpoints

## Listar Classes

```http
GET /api/document-classes
```

Response:

```json
[
  {
    "id": "guid",
    "name": "RELATORIO_TECNICO_PRELIMINAR",
    "displayName": "Relatório Técnico Preliminar",
    "group": "ADMINISTRATIVO",
    "subGroup": "PROCESSO_ADMINISTRATIVO"
  }
]
```

---

## Detalhar Classe

```http
GET /api/document-classes/{id}
```

---

## Exemplos da Classe

```http
GET /api/document-classes/{id}/examples
```

---

# Testes Unitários

Criar testes para:

* Criar nova classe documental.
* Reutilizar classe existente.
* Registrar exemplo.
* Impedir duplicidade de nome.
* Recuperar classe por nome.

---

# Testes de Integração

Criar testes para:

* Seed inicial.
* Criação automática de nova classe.
* Registro de exemplos.
* Endpoints do catálogo.

---

# Critérios de Aceite

A task será considerada concluída quando:

* A entidade DocumentClass existir.
* A entidade DocumentClassExample existir.
* As migrations forem criadas.
* As classes padrão forem seedadas.
* O pipeline reutilizar classes existentes.
* O pipeline criar classes desconhecidas automaticamente.
* O pipeline registrar exemplos.
* Os endpoints de consulta existirem.
* Os testes estiverem implementados.
* A solução continuar respeitando Clean Architecture.

---

# Resultado Esperado

Ao final desta task o Doclyn deixará de apenas classificar documentos e passará a possuir um catálogo persistido de conhecimento documental.

Esse catálogo servirá como base para a próxima etapa:

```txt
DocumentClassIndexer
```

que permitirá ao sistema saber quais indexadores devem ser extraídos para cada tipo documental.
