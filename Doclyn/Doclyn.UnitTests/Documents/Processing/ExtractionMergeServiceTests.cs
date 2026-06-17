using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Constants;
using Doclyn.Infrastructure.AI;

namespace Doclyn.UnitTests.Documents.Processing;

public sealed class ExtractionMergeServiceTests
{
    private readonly ExtractionMergeService _service = new();

    [Fact]
    public void Should_Prioritize_Regex_Values_When_Merging()
    {
        var classification = new DocumentClassificationResult(
            DocumentTypes.RelatorioTecnicoPreliminar,
            "PROCESSO_ADMINISTRATIVO",
            "APURACAO_CONTRATUAL",
            0.97);

        var regexExtraction = new Dictionary<string, object?>
        {
            ["cnpj"] = "12.345.678/0001-99",
            ["orgao"] = "Prefeitura Municipal de Vale Verde"
        };

        var aiExtraction = new Dictionary<string, object?>
        {
            ["cnpj"] = "00.000.000/0000-00",
            ["summary"] = "Resumo IA"
        };

        var result = _service.Merge(classification, regexExtraction, aiExtraction);

        Assert.Equal("12.345.678/0001-99", result["cnpj"]);
        Assert.Equal("Resumo IA", result["summary"]);
        Assert.Equal(0.97, result["confidence"]);
    }
}
