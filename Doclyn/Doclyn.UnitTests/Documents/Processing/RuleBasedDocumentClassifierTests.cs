using Doclyn.Domain.Constants;
using Doclyn.Infrastructure.Classification;

namespace Doclyn.UnitTests.Documents.Processing;

public sealed class RuleBasedDocumentClassifierTests
{
    private readonly RuleBasedDocumentClassifier _classifier = new();

    [Fact]
    public void Should_Classify_Relatorio_Tecnico_Preliminar()
    {
        const string text = """
            RELATÓRIO TÉCNICO PRELIMINAR
            PROCESSO ADMINISTRATIVO Nº 2026/98765
            CONTRATO nº 45/2026
            FISCALIZAÇÃO CONTRATUAL
            PREFEITURA MUNICIPAL DE VALE VERDE
            PROCURADORIA JURÍDICA
            """;

        var result = _classifier.Classify(text);

        Assert.Equal(DocumentTypes.RelatorioTecnicoPreliminar, result.DocumentType);
        Assert.Equal("ADMINISTRATIVO", result.Group);
        Assert.Equal("PROCESSO_ADMINISTRATIVO", result.Subgroup);
        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Should_Return_Desconhecido_When_Text_Is_Not_Recognized()
    {
        var result = _classifier.Classify("generic content without classification keywords");

        Assert.Equal(DocumentTypes.DocumentoDesconhecido, result.DocumentType);
        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public void Should_Classify_Contrato_Administrativo()
    {
        const string text = """
            CONTRATO ADMINISTRATIVO Nº 45/2026
            CLÁUSULA PRIMEIRA - DO OBJETO DO CONTRATO
            VIGÊNCIA DE 12 MESES
            CONTRATANTE E CONTRATADA
            """;

        var result = _classifier.Classify(text);

        Assert.Equal(DocumentTypes.ContratoAdministrativo, result.DocumentType);
        Assert.Equal("ADMINISTRATIVO", result.Group);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void Should_Classify_Oficio()
    {
        const string text = """
            OFÍCIO Nº 123/2026
            ENCAMINHAMOS INFORMAÇÕES COMPLEMENTARES
            SOLICITAMOS DILIGÊNCIA
            """;

        var result = _classifier.Classify(text);

        Assert.Equal(DocumentTypes.Oficio, result.DocumentType);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void Should_Classify_Nota_Fiscal()
    {
        const string text = """
            NOTA FISCAL ELETRÔNICA
            DANFE
            EMISSÃO 14/03/2026
            ICMS DESTACADO
            VALOR TOTAL R$ 12.345,67
            CHAVE DE ACESSO 1234
            """;

        var result = _classifier.Classify(text);

        Assert.Equal(DocumentTypes.NotaFiscal, result.DocumentType);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void Should_Classify_Peticao_Judicial()
    {
        const string text = """
            EXCELENTÍSSIMO SENHOR JUIZ
            MERITÍSSIMO
            REQUER A CONCESSÃO DE TUTELA LIMINAR
            AUTOS DO PROCESSO
            """;

        var result = _classifier.Classify(text);

        Assert.Equal(DocumentTypes.PeticaoJudicial, result.DocumentType);
        Assert.True(result.Confidence > 0);
    }
}
