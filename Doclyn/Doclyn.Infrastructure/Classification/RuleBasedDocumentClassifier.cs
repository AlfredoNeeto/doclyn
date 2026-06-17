using System.Globalization;
using System.Text;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Constants;

namespace Doclyn.Infrastructure.Classification;

public sealed class RuleBasedDocumentClassifier : IDocumentClassifier
{
    private static readonly (string DocumentType, string Group, string SubGroup, string[] Keywords)[] Rules =
    [
        (DocumentTypes.RelatorioTecnicoPreliminar, "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO",
            ["PROCESSO ADMINISTRATIVO", "RELATORIO TECNICO PRELIMINAR", "CONTRATO", "FISCALIZACAO CONTRATUAL", "PREFEITURA MUNICIPAL", "PROCURADORIA JURIDICA"]),

        (DocumentTypes.ContratoAdministrativo, "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO",
            ["CONTRATO", "CLAUSULA", "VIGENCIA", "CONTRATANTE", "CONTRATADA", "OBJETO DO CONTRATO"]),

        (DocumentTypes.Oficio, "ADMINISTRATIVO", "COMUNICACAO",
            ["OFICIO", "ENCAMINHAMOS", "SOLICITAMOS", "DILIGENCIA", "INFORMACOES COMPLEMENTARES"]),

        (DocumentTypes.NotaFiscal, "FISCAL", "TRIBUTARIO",
            ["NOTA FISCAL", "DANFE", "EMISSAO", "ICMS", "VALOR TOTAL", "CHAVE DE ACESSO"]),

        (DocumentTypes.PeticaoJudicial, "JURIDICO", "PROCESSO_JUDICIAL",
            ["EXCELENTISSIMO", "MERITISSIMO", "REQUER", "AUTOS", "LIMINAR", "TUTELA"])
    ];

    private const int MinimumMatchCount = 3;

    public DocumentClassificationResult Classify(string text)
    {
        var normalizedText = Normalize(text);

        foreach (var (documentType, group, subGroup, keywords) in Rules)
        {
            var matchCount = keywords.Count(normalizedText.Contains);
            if (matchCount >= MinimumMatchCount)
            {
                var confidence = Math.Min(1.0, (double)matchCount / keywords.Length);
                return new DocumentClassificationResult(documentType, group, subGroup, confidence);
            }
        }

        return new DocumentClassificationResult(
            DocumentTypes.DocumentoDesconhecido,
            "UNKNOWN",
            "UNKNOWN",
            0.0);
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().ToUpperInvariant();
    }
}
