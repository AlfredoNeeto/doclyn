using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.AI;

namespace Doclyn.UnitTests.AI;

public sealed class DynamicExtractionPromptBuilderTests
{
    private readonly IExtractionPromptBuilder _builder = new DynamicExtractionPromptBuilder();

    private static DocumentClass CreateDocumentClass()
    {
        return DocumentClass.Create(
            "RELATORIO_TECNICO_PRELIMINAR",
            "ADMINISTRATIVO",
            "PROCESSO_ADMINISTRATIVO",
            "Classe de relatorio tecnico preliminar.",
            isSystemDefined: true);
    }

    [Fact]
    public void Should_Include_Class_Metadata()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>();

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("RELATORIO_TECNICO_PRELIMINAR", prompt, StringComparison.Ordinal);
        Assert.Contains("ADMINISTRATIVO", prompt, StringComparison.Ordinal);
        Assert.Contains("PROCESSO_ADMINISTRATIVO", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Include_Class_Description()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>();

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("relatorio tecnico preliminar", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_Include_Indexer_Names()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "numeroProcesso",
                "Numero do Processo",
                "Numero do processo administrativo.",
                IndexerDataType.Text,
                isRequired: true,
                isMultiple: false)
        };

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("numeroProcesso", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Include_Indexer_Type_And_Flags()
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

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("required", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("single", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Text", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Include_Indexer_Description_And_Hint()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                documentClass.Id,
                "orgao",
                "Orgao",
                "Nome do orgao publico responsavel.",
                IndexerDataType.Text,
                isRequired: false,
                isMultiple: false,
                extractionHint: "Buscar por 'Prefeitura Municipal de' seguido do nome.")
        };

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("orgao publico", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Prefeitura Municipal de", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Instruct_Json_Only()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>();

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("JSON", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Instruct_Null_When_Not_Found()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>();

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.Contains("null", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_Not_Contain_Hardcoded_Class_Routing()
    {
        var documentClass = CreateDocumentClass();
        var indexers = new List<DocumentClassIndexer>();

        var prompt = _builder.Build(documentClass, indexers, "sample text");

        Assert.DoesNotContain("relatorio-tecnico.prompt.md", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
