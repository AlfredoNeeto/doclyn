using System.Text.Json;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.AI;

namespace Doclyn.UnitTests.AI;

public sealed class DocumentClassAiSchemaBuilderTests
{
    private readonly DocumentClassAiSchemaBuilder _builder = new();

    private static DocumentClass CreateDocumentClass()
    {
        return DocumentClass.Create(
            "RELATORIO_TECNICO_PRELIMINAR",
            "ADMINISTRATIVO",
            "PROCESSO_ADMINISTRATIVO",
            "Relatorio tecnico preliminar.",
            isSystemDefined: true);
    }

    [Fact]
    public void Should_Generate_Schema_With_Required_Scalar_Field()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "numeroProcesso",
                "Numero do Processo",
                "Descricao.",
                IndexerDataType.Text,
                isRequired: true,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        Assert.Equal("object", json.RootElement.GetProperty("type").GetString());

        var props = json.RootElement.GetProperty("properties");
        var prop = props.GetProperty("numeroProcesso");
        Assert.Equal("object", prop.GetProperty("type").GetString());
        Assert.False(prop.GetProperty("additionalProperties").GetBoolean());

        var required = json.RootElement.GetProperty("required");
        Assert.Contains("numeroProcesso", required.EnumerateArray().Select(e => e.GetString()));

        var valueProp = prop.GetProperty("properties").GetProperty("value");
        Assert.Contains("string", valueProp.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("null", valueProp.GetProperty("type").EnumerateArray().Select(e => e.GetString()));

        var confidenceProp = prop.GetProperty("properties").GetProperty("confidence");
        Assert.Equal("number", confidenceProp.GetProperty("type").GetString());
    }

    [Fact]
    public void Should_Generate_Schema_With_Optional_Scalar_Field()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "orgao",
                "Orgao",
                "Descricao.",
                IndexerDataType.Text,
                isRequired: false,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var required = json.RootElement.GetProperty("required");
        Assert.Contains("orgao", required.EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Should_Generate_Schema_With_Multiple_Field()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "datas",
                "Datas",
                "Descricao.",
                IndexerDataType.Date,
                isRequired: false,
                isMultiple: true)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var prop = json.RootElement.GetProperty("properties").GetProperty("datas");
        Assert.Equal("object", prop.GetProperty("type").GetString());

        var valueProp = prop.GetProperty("properties").GetProperty("value");
        Assert.Equal("array", valueProp.GetProperty("type").GetString());
        var items = valueProp.GetProperty("items");
        Assert.Contains("string", items.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("null", items.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Should_Map_Boolean_Type_Correctly()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "ativo",
                "Ativo",
                "Descricao.",
                IndexerDataType.Boolean,
                isRequired: false,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var prop = json.RootElement.GetProperty("properties").GetProperty("ativo");
        var valueProp = prop.GetProperty("properties").GetProperty("value");
        Assert.Contains("boolean", valueProp.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Should_Map_Number_Type_Correctly()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "quantidade",
                "Quantidade",
                "Descricao.",
                IndexerDataType.Number,
                isRequired: false,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var prop = json.RootElement.GetProperty("properties").GetProperty("quantidade");
        var valueProp = prop.GetProperty("properties").GetProperty("value");
        Assert.Contains("number", valueProp.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Should_Map_Object_Type_With_Additional_Properties()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "metadados",
                "Metadados",
                "Descricao.",
                IndexerDataType.Object,
                isRequired: false,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var prop = json.RootElement.GetProperty("properties").GetProperty("metadados");
        var valueProp = prop.GetProperty("properties").GetProperty("value");
        Assert.Contains("object", valueProp.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
        Assert.True(valueProp.GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void Should_Map_Array_Type_As_String_Array()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "tags",
                "Tags",
                "Descricao.",
                IndexerDataType.Array,
                isRequired: false,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var prop = json.RootElement.GetProperty("properties").GetProperty("tags");
        var valueProp = prop.GetProperty("properties").GetProperty("value");
        Assert.Equal("array", valueProp.GetProperty("type").GetString());
        Assert.Equal("string", valueProp.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Should_Enforce_AdditionalProperties_False()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "orgao",
                "Orgao",
                "Descricao.",
                IndexerDataType.Text,
                isRequired: false,
                isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        Assert.False(json.RootElement.GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void Should_Include_All_Indexers_In_Required_For_Strict_Schema()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(documentClass.Id, "numeroProcesso", "Numero", "Desc", IndexerDataType.Text, isRequired: true, isMultiple: false),
            DocumentClassIndexer.Create(documentClass.Id, "assunto", "Assunto", "Desc", IndexerDataType.Text, isRequired: false, isMultiple: false),
            DocumentClassIndexer.Create(documentClass.Id, "orgao", "Orgao", "Desc", IndexerDataType.Text, isRequired: false, isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var propertyNames = json.RootElement.GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        var requiredNames = json.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(propertyNames, requiredNames);
    }

    [Fact]
    public void Optional_Field_Should_Allow_Null_Value_Even_When_In_Required()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(documentClass.Id, "assunto", "Assunto", "Desc", IndexerDataType.Text, isRequired: false, isMultiple: false)
        };

        var schema = _builder.Build(documentClass, indexers);
        var json = JsonDocument.Parse(schema.ToString());

        var required = json.RootElement.GetProperty("required");
        Assert.Contains("assunto", required.EnumerateArray().Select(e => e.GetString()));

        var valueType = json.RootElement
            .GetProperty("properties")
            .GetProperty("assunto")
            .GetProperty("properties")
            .GetProperty("value")
            .GetProperty("type")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        Assert.Contains("null", valueType);
    }
}
