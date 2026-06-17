# DOCUMENT_CLASS_INDEXERS_TASK.md

# Task — Implementar Catálogo de Indexadores por Classe Documental

## Contexto

O Doclyn já possui ou está evoluindo para possuir um catálogo persistido de classes documentais.

Exemplos de classes documentais:

```txt
RELATORIO_TECNICO_PRELIMINAR
CONTRATO_ADMINISTRATIVO
OFICIO
NOTA_FISCAL
PETICAO_JUDICIAL
DOCUMENTO_DESCONHECIDO
```

Agora será implementado o catálogo de indexadores esperados para cada classe documental.

Essa task é essencial para que o sistema deixe de apenas “extrair dados genericamente” e passe a saber quais campos deve tentar extrair de acordo com o tipo/cunho do documento.

---

# Objetivo

Criar a entidade `DocumentClassIndexer`, responsável por definir quais indexadores devem ser extraídos para cada classe documental.

Exemplo:

```txt
RELATORIO_TECNICO_PRELIMINAR
- numeroProcesso
- numeroContrato
- orgao
- empresa
- cnpj
- cpf
- datas
- valores
```

O objetivo é permitir que o pipeline de processamento carregue a classe documental identificada e saiba exatamente quais campos devem ser extraídos.

---

# Conceito

Hoje:

```txt
Documento
↓
IA classifica
↓
IA extrai dados com schema fixo ou genérico
↓
Resultado salvo em JSONB
```

Após esta implementação:

```txt
Documento
↓
IA classifica
↓
Sistema identifica DocumentClass
↓
Sistema busca DocumentClassIndexers
↓
IA extrai os campos esperados daquela classe
↓
Resultado salvo em JSONB
```

---

# Entidade Principal

## DocumentClassIndexer

Representa um campo/indexador esperado para uma classe documental.

### Exemplo

```txt
DocumentClass: RELATORIO_TECNICO_PRELIMINAR

Indexadores:
- numeroProcesso
- numeroContrato
- orgao
- empresa
- cnpj
- cpfs
- datas
- valores
```

### Estrutura sugerida

```csharp
public sealed class DocumentClassIndexer
{
    public Guid Id { get; private set; }

    public Guid DocumentClassId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public IndexerDataType DataType { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsMultiple { get; private set; }

    public string? ExtractionHint { get; private set; }

    public string? RegexPattern { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
}
```

---

# Enum

Criar enum:

```csharp
public enum IndexerDataType
{
    Text = 1,
    Number = 2,
    Decimal = 3,
    Date = 4,
    Boolean = 5,
    Cpf = 6,
    Cnpj = 7,
    Email = 8,
    Phone = 9,
    Cep = 10,
    Currency = 11,
    Object = 12,
    Array = 13
}
```

---

# Banco de Dados

## DOCUMENT_CLASS_INDEXERS

Tabela:

```sql
ID
DOCUMENT_CLASS_ID
NAME
DISPLAY_NAME
DESCRIPTION
DATA_TYPE
IS_REQUIRED
IS_MULTIPLE
EXTRACTION_HINT
REGEX_PATTERN
IS_ACTIVE
CREATED_AT
UPDATED_AT
```

Relacionamento:

```txt
DOCUMENT_CLASSES 1:N DOCUMENT_CLASS_INDEXERS
```

Índices recomendados:

```sql
DOCUMENT_CLASS_ID
NAME
DOCUMENT_CLASS_ID + NAME UNIQUE
IS_ACTIVE
```

---

# Convenção de Nomes

## Nome interno

Usar camelCase para o nome lógico do indexador:

```txt
numeroProcesso
numeroContrato
orgao
empresa
cnpj
cpfs
datas
valores
```

## Nome de exibição

Usar texto amigável:

```txt
Número do Processo
Número do Contrato
Órgão
Empresa
CNPJ
CPFs
Datas
Valores
```

## Banco

Manter padrão do projeto:

```txt
UPPER_SNAKE_CASE
```

Exemplo:

```sql
DOCUMENT_CLASS_INDEXERS
DOCUMENT_CLASS_ID
DISPLAY_NAME
DATA_TYPE
IS_REQUIRED
```

---

# Seeds Iniciais

Criar seeds de indexadores para a classe:

```txt
RELATORIO_TECNICO_PRELIMINAR
```

## Indexadores sugeridos

```txt
numeroProcesso
numeroContrato
orgao
empresa
cnpj
pessoasCitadas
cpfs
matriculas
datas
valores
notaFiscal
oficio
emails
telefones
endereco
cep
agencia
contaCorrente
documentosRelacionados
palavrasChave
assunto
resumo
```

---

# Exemplo de seed

```json
[
  {
    "name": "numeroProcesso",
    "displayName": "Número do Processo",
    "description": "Número do processo administrativo ou judicial identificado no documento.",
    "dataType": "Text",
    "isRequired": true,
    "isMultiple": false,
    "extractionHint": "Procure expressões como Processo Administrativo nº, Processo nº ou autos nº.",
    "regexPattern": "PROCESSO\\s+ADMINISTRATIVO\\s+N[º°]?\\s*([\\d\\/]+)"
  },
  {
    "name": "numeroContrato",
    "displayName": "Número do Contrato",
    "description": "Número do contrato relacionado ao documento.",
    "dataType": "Text",
    "isRequired": false,
    "isMultiple": false,
    "extractionHint": "Procure expressões como Contrato nº ou contrato celebrado.",
    "regexPattern": "Contrato\\s+n[º°]?\\s*([\\d\\/]+)"
  },
  {
    "name": "cnpj",
    "displayName": "CNPJ",
    "description": "CNPJ de empresa citada no documento.",
    "dataType": "Cnpj",
    "isRequired": false,
    "isMultiple": false,
    "extractionHint": "Extraia CNPJ no formato 00.000.000/0000-00.",
    "regexPattern": "\\d{2}\\.\\d{3}\\.\\d{3}\\/\\d{4}-\\d{2}"
  }
]
```

---

# Integração com o Pipeline

Após a classificação documental:

```txt
IA classifica documento
↓
Sistema identifica DocumentClass
↓
Sistema carrega DocumentClassIndexers ativos
↓
Pipeline usa indexadores para orientar Regex e IA
↓
Resultado final é salvo em JSONB
```

---

# Extração por Regex

Se o indexador possuir `RegexPattern`, o pipeline deve tentar extrair primeiro por regex.

Exemplo:

```txt
cnpj
cpf
email
telefone
cep
datas
valores
```

Motivo:

```txt
Regex é mais determinístico e barato.
```

---

# Extração por IA

A IA deve receber dinamicamente a lista de indexadores esperados.

Exemplo de prompt gerado:

```txt
Você deve extrair os seguintes indexadores do documento:

- numeroProcesso: Número do processo administrativo ou judicial.
- numeroContrato: Número do contrato relacionado.
- orgao: Órgão público citado.
- empresa: Empresa citada.
- cnpj: CNPJ da empresa.
- datas: Todas as datas relevantes.

Retorne apenas JSON válido.
```

---

# Resultado esperado em JSONB

Salvar resultado estruturado incluindo:

```json
{
  "classification": {
    "documentClassId": "guid",
    "documentType": "RELATORIO_TECNICO_PRELIMINAR",
    "confidence": 0.98
  },
  "indexers": {
    "numeroProcesso": {
      "value": "2026/98765",
      "source": "Regex",
      "confidence": 1.0
    },
    "orgao": {
      "value": "Prefeitura Municipal de Vale Verde",
      "source": "AI",
      "confidence": 0.94
    }
  }
}
```

---

# Fontes de Extração

Criar enum opcional:

```csharp
public enum ExtractionSource
{
    Regex = 1,
    AI = 2,
    Merged = 3,
    Manual = 4
}
```

---

# Serviço de Catálogo

Criar interface em Application:

```csharp
public interface IDocumentClassIndexerCatalogService
{
    Task<IReadOnlyCollection<DocumentClassIndexer>> GetActiveByDocumentClassAsync(
        Guid documentClassId,
        CancellationToken cancellationToken = default);

    Task<DocumentClassIndexer> CreateAsync(
        Guid documentClassId,
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint,
        string? regexPattern,
        CancellationToken cancellationToken = default);
}
```

---

# Estrutura sugerida

## Domain

```txt
Doclyn.Domain/
  Entities/
    DocumentClassIndexer.cs

  Enums/
    IndexerDataType.cs
    ExtractionSource.cs
```

## Application

```txt
Doclyn.Application/
  DocumentClassIndexers/
    GetByDocumentClass/
    Create/
    Update/
    Disable/

  Common/
    Interfaces/
      IDocumentClassIndexerCatalogService.cs
```

## Infrastructure

```txt
Doclyn.Infrastructure/
  Database/
    Configurations/
      DocumentClassIndexerConfiguration.cs

  DocumentClassIndexers/
    DocumentClassIndexerCatalogService.cs
```

## Api

```txt
Doclyn.Api/
  Controllers/
    DocumentClassIndexersController.cs
```

---

# Endpoints

## Listar indexadores por classe documental

```http
GET /api/document-classes/{documentClassId}/indexers
```

Response:

```json
[
  {
    "id": "guid",
    "name": "numeroProcesso",
    "displayName": "Número do Processo",
    "description": "Número do processo administrativo ou judicial.",
    "dataType": "Text",
    "isRequired": true,
    "isMultiple": false,
    "extractionHint": "Procure expressões como Processo Administrativo nº.",
    "hasRegexPattern": true,
    "isActive": true
  }
]
```

---

## Criar indexador

```http
POST /api/document-classes/{documentClassId}/indexers
```

Request:

```json
{
  "name": "numeroProcesso",
  "displayName": "Número do Processo",
  "description": "Número do processo administrativo ou judicial.",
  "dataType": "Text",
  "isRequired": true,
  "isMultiple": false,
  "extractionHint": "Procure expressões como Processo Administrativo nº.",
  "regexPattern": "PROCESSO\\s+ADMINISTRATIVO\\s+N[º°]?\\s*([\\d\\/]+)"
}
```

---

## Atualizar indexador

```http
PUT /api/document-classes/{documentClassId}/indexers/{id}
```

---

## Desativar indexador

```http
DELETE /api/document-classes/{documentClassId}/indexers/{id}
```

Não excluir fisicamente. Apenas marcar:

```txt
IS_ACTIVE = false
```

---

# Validações

## Name

Obrigatório.

Formato:

```txt
camelCase
```

Exemplo válido:

```txt
numeroProcesso
```

Exemplo inválido:

```txt
Numero Processo
numero_processo
NUMERO_PROCESSO
```

## DisplayName

Obrigatório.

## DataType

Obrigatório.

## RegexPattern

Opcional.

Se informado, validar se o padrão regex é compilável.

## Duplicidade

Não permitir dois indexadores ativos com o mesmo `Name` na mesma `DocumentClass`.

---

# Segurança

Endpoints de escrita devem ser restritos a:

```txt
Admin
```

Endpoints de leitura podem ser acessados por:

```txt
Admin
Operator
```

---

# Logs

Registrar logs para:

```txt
DocumentClassIndexerCreated
DocumentClassIndexerUpdated
DocumentClassIndexerDisabled
DocumentClassIndexersLoaded
```

---

# Testes Unitários

Criar testes para:

- Criar indexador válido.
- Rejeitar nome inválido.
- Rejeitar duplicidade.
- Rejeitar regex inválida.
- Desativar indexador.
- Buscar apenas indexadores ativos.
- Garantir relacionamento com DocumentClass.

---

# Testes de Integração

Criar testes para:

- Criar migration corretamente.
- Criar indexador via API.
- Listar indexadores por classe.
- Impedir criação duplicada.
- Impedir escrita por usuário Operator.
- Permitir leitura por usuário Operator.
- Desativar indexador.

---

# Integração futura com IA

Esta task prepara a próxima etapa:

```txt
Extração orientada por classe documental
```

Nessa etapa, o pipeline vai usar os indexadores cadastrados para montar dinamicamente o prompt da IA e extrair exatamente os campos esperados para cada classe documental.

---

# Critérios de Aceite

A task será considerada concluída quando:

- Entidade `DocumentClassIndexer` existir.
- Enum `IndexerDataType` existir.
- Migration for criada.
- Tabela `DOCUMENT_CLASS_INDEXERS` existir.
- Relacionamento com `DOCUMENT_CLASSES` estiver configurado.
- Seeds iniciais para `RELATORIO_TECNICO_PRELIMINAR` forem criadas.
- Endpoints de leitura e escrita estiverem disponíveis.
- Escrita for restrita a Admin.
- Leitura for permitida para Admin e Operator.
- Pipeline conseguir carregar indexadores ativos da classe documental.
- Código respeitar Clean Architecture.
- Testes principais forem implementados.
