using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.AI;

namespace Doclyn.UnitTests.AI;

public sealed class PromptBuilderDynamicExtractionTests
{
    private readonly PromptBuilder _builder = new();

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
    public void Should_Include_Class_Information_In_Dynamic_Prompt()
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
                isMultiple: false,
                extractionHint: "Trecho iniciado por 'PROCESSO ADMINISTRATIVO'.")
        };

        var prompt = _builder.BuildDynamicExtractionPrompt(documentClass, indexers);

        Assert.Contains("Document class", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RELATORIO_TECNICO_PRELIMINAR", prompt, StringComparison.Ordinal);
        Assert.Contains("ADMINISTRATIVO", prompt, StringComparison.Ordinal);
        Assert.Contains("PROCESSO_ADMINISTRATIVO", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Include_Field_Names_In_Dynamic_Prompt()
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
                isMultiple: false),
            DocumentClassIndexer.Create(
                documentClass.Id,
                "datas",
                "Datas",
                "Descricao.",
                IndexerDataType.Date,
                isRequired: false,
                isMultiple: true)
        };

        var prompt = _builder.BuildDynamicExtractionPrompt(documentClass, indexers);

        Assert.Contains("numeroProcesso", prompt, StringComparison.Ordinal);
        Assert.Contains("datas", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Include_Required_And_Multiple_Flags_In_Dynamic_Prompt()
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
                isMultiple: false),
            DocumentClassIndexer.Create(
                documentClass.Id,
                "datas",
                "Datas",
                "Descricao.",
                IndexerDataType.Date,
                isRequired: false,
                isMultiple: true)
        };

        var prompt = _builder.BuildDynamicExtractionPrompt(documentClass, indexers);

        Assert.Contains("required", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("multiple", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_Include_Extraction_Hints_In_Dynamic_Prompt()
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
                isMultiple: false,
                extractionHint: "Trecho iniciado por 'PROCESSO ADMINISTRATIVO N'.")
        };

        var prompt = _builder.BuildDynamicExtractionPrompt(documentClass, indexers);

        Assert.Contains("PROCESSO ADMINISTRATIVO", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Include_Null_Instruction_In_Dynamic_Prompt()
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

        var prompt = _builder.BuildDynamicExtractionPrompt(documentClass, indexers);

        Assert.Contains("null", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_Not_Contain_Hardcoded_Document_Type_Routing()
    {
        var documentClass = CreateDocumentClass();
        var indexers = Array.Empty<DocumentClassIndexer>();

        var prompt = _builder.BuildDynamicExtractionPrompt(documentClass, indexers);

        Assert.DoesNotContain("relatorio-tecnico.prompt.md", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contrato.prompt.md", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
