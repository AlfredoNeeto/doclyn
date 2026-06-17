using System.Text.RegularExpressions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Insights;
using Doclyn.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doclyn.Infrastructure.Insights;

public sealed class RuleBasedInsightGenerator : IRuleBasedInsightGenerator
{
    private readonly InsightOptions _options;
    private readonly ILogger<RuleBasedInsightGenerator> _logger;

    public RuleBasedInsightGenerator(
        IOptions<InsightOptions> options,
        ILogger<RuleBasedInsightGenerator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyCollection<DocumentInsightResult> Generate(ExtractedDocumentData extractedData)
    {
        var results = new List<DocumentInsightResult>();

        GenerateContractExpirationInsights(extractedData, results);
        GenerateMissingRequiredFieldInsights(extractedData, results);
        GenerateLowConfidenceInsights(extractedData, results);
        GenerateInvalidIdentifierInsights(extractedData, results);
        GenerateHighValueInsights(extractedData, results);

        _logger.LogInformation(
            "RuleInsightsGenerated: {Count} rule-based insights for document {DocumentId}.",
            results.Count,
            extractedData.DocumentId);

        return results;
    }

    private void GenerateContractExpirationInsights(ExtractedDocumentData data, List<DocumentInsightResult> results)
    {
        var fieldNames = new[] { "dataFimVigencia", "dataVencimento", "fimVigencia", "vigenciaFim" };

        foreach (var fieldName in fieldNames)
        {
            if (data.Fields.TryGetValue(fieldName, out var field)
                && field.Value is string dateStr
                && DateTime.TryParse(dateStr, out var date))
            {
                var daysUntilExpiration = (date.Date - DateTime.UtcNow.Date).Days;

                if (daysUntilExpiration < 0)
                {
                    results.Add(new DocumentInsightResult(
                        DocumentInsightType.ContractExpired,
                        DocumentInsightSeverity.Warning,
                        "Contrato vencido",
                        $"O contrato encontra-se vencido desde {date:dd/MM/yyyy}.",
                        1.0m,
                        DocumentInsightSource.Rule,
                        fieldName));
                }
                else if (daysUntilExpiration <= _options.ContractExpiringSoonDays)
                {
                    var dias = daysUntilExpiration == 0 ? "hoje" : $"em {daysUntilExpiration} dias";
                    results.Add(new DocumentInsightResult(
                        DocumentInsightType.ContractExpiringSoon,
                        DocumentInsightSeverity.Warning,
                        "Contrato proximo do vencimento",
                        $"O contrato vence {dias} ({date:dd/MM/yyyy}).",
                        1.0m,
                        DocumentInsightSource.Rule,
                        fieldName));
                }
            }
        }
    }

    private void GenerateMissingRequiredFieldInsights(ExtractedDocumentData data, List<DocumentInsightResult> results)
    {
        foreach (var (fieldName, field) in data.Fields)
        {
            if (field.ValidationStatus == ValidationStatus.Rejected
                && (field.Value is null || (field.Value is string s && string.IsNullOrWhiteSpace(s))))
            {
                results.Add(new DocumentInsightResult(
                    DocumentInsightType.MissingRequiredField,
                    DocumentInsightSeverity.Warning,
                    "Campo obrigatorio ausente",
                    $"O campo \"{fieldName}\" nao foi encontrado no documento.",
                    1.0m,
                    DocumentInsightSource.Rule,
                    fieldName));
            }
        }
    }

    private void GenerateLowConfidenceInsights(ExtractedDocumentData data, List<DocumentInsightResult> results)
    {
        foreach (var (fieldName, field) in data.Fields)
        {
            if (field.Confidence < _options.LowConfidenceThreshold && field.Confidence > 0)
            {
                results.Add(new DocumentInsightResult(
                    DocumentInsightType.LowConfidenceField,
                    DocumentInsightSeverity.Info,
                    "Campo com baixa confianca",
                    $"O campo \"{fieldName}\" foi extraido com baixa confianca ({field.Confidence:P0}).",
                    field.Confidence,
                    DocumentInsightSource.Rule,
                    fieldName));
            }
        }
    }

    private void GenerateInvalidIdentifierInsights(ExtractedDocumentData data, List<DocumentInsightResult> results)
    {
        var cnpjFields = new[] { "cnpj", "cnpjContratada", "cnpjContratante" };
        foreach (var fieldName in cnpjFields)
        {
            if (data.Fields.TryGetValue(fieldName, out var field)
                && field.Value is string cnpj
                && !string.IsNullOrWhiteSpace(cnpj))
            {
                var digits = Regex.Replace(cnpj, @"\D", string.Empty);
                if (digits.Length != 14 || !IsValidCnpj(digits))
                {
                    results.Add(new DocumentInsightResult(
                        DocumentInsightType.InvalidIdentifier,
                        DocumentInsightSeverity.Critical,
                        "CNPJ invalido",
                        "O CNPJ identificado no documento nao passou na validacao.",
                        1.0m,
                        DocumentInsightSource.Rule,
                        fieldName));
                }
            }
        }
    }

    private void GenerateHighValueInsights(ExtractedDocumentData data, List<DocumentInsightResult> results)
    {
        var valueFields = new[] { "valor", "valorContrato", "valorTotal" };
        foreach (var fieldName in valueFields)
        {
            if (data.Fields.TryGetValue(fieldName, out var field)
                && field.Value is not null)
            {
                var valueStr = field.Value.ToString() ?? string.Empty;
                var digitsOnly = Regex.Replace(valueStr, @"[^\d,\.]", string.Empty);

                if (decimal.TryParse(digitsOnly,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),
                    out var value)
                    && value >= _options.HighValueThreshold)
                {
                    results.Add(new DocumentInsightResult(
                        DocumentInsightType.HighValueDocument,
                        DocumentInsightSeverity.Info,
                        "Documento de alto valor",
                        $"O documento menciona valor acima do limite configurado ({value:C}).",
                        1.0m,
                        DocumentInsightSource.Rule,
                        fieldName));
                }
            }
        }
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
            return false;

        var multiplier1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += int.Parse(cnpj[i].ToString()) * multiplier1[i];
        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if (int.Parse(cnpj[12].ToString()) != digit1)
            return false;

        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += int.Parse(cnpj[i].ToString()) * multiplier2[i];
        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return int.Parse(cnpj[13].ToString()) == digit2;
    }
}
