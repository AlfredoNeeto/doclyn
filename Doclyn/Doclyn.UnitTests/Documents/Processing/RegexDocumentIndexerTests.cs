using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.Extraction;

namespace Doclyn.UnitTests.Documents.Processing;

public sealed class RegexDocumentIndexerTests
{
    private readonly RegexDocumentIndexer _indexer = new();

    [Fact]
    public void Should_Extract_Expected_Indexes_From_Catalog()
    {
        const string text = """
            PROCESSO ADMINISTRATIVO Nº 2026/98765
            Contrato nº 45/2026
            Prefeitura Municipal de Vale Verde
            Soluções Integradas do Centro-Oeste LTDA
            CNPJ 12.345.678/0001-99
            CPF 123.456.789-00
            matrícula funcional 445566
            Valor total R$ 12.345,67
            Data 14/03/2026
            Nota Fiscal nº NF-2026-000123
            Ofício nº 112/2026
            contato@solucoesintegradas.com.br
            (65) 99999-9999
            CEP 78000-000
            agência 1234
            conta corrente 56789-0
            Relatório Técnico
            """;

        var documentClassId = Guid.NewGuid();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(documentClassId, "numeroProcesso", "Número do Processo", "", IndexerDataType.Text, true, false, regexPattern: @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)"),
            DocumentClassIndexer.Create(documentClassId, "numeroContrato", "Número do Contrato", "", IndexerDataType.Text, false, false, regexPattern: @"Contrato\s+n[º°]?\s*([\d\/.-]+)"),
            DocumentClassIndexer.Create(documentClassId, "cnpj", "CNPJ", "", IndexerDataType.Cnpj, false, false, regexPattern: @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            DocumentClassIndexer.Create(documentClassId, "cpfs", "CPFs", "", IndexerDataType.Cpf, false, true, regexPattern: @"\d{3}\.\d{3}\.\d{3}-\d{2}"),
            DocumentClassIndexer.Create(documentClassId, "matriculas", "Matrículas", "", IndexerDataType.Text, false, true, regexPattern: @"matr[íi]cula\s+funcional\s+(\d+)"),
            DocumentClassIndexer.Create(documentClassId, "valores", "Valores", "", IndexerDataType.Currency, false, true, regexPattern: @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            DocumentClassIndexer.Create(documentClassId, "datas", "Datas", "", IndexerDataType.Date, false, true, regexPattern: @"\d{2}\/\d{2}\/\d{4}"),
            DocumentClassIndexer.Create(documentClassId, "emails", "E-mails", "", IndexerDataType.Email, false, true, regexPattern: @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            DocumentClassIndexer.Create(documentClassId, "telefones", "Telefones", "", IndexerDataType.Phone, false, true, regexPattern: @"\(\d{2}\)\s*\d{4,5}-\d{4}"),
            DocumentClassIndexer.Create(documentClassId, "cep", "CEP", "", IndexerDataType.Cep, false, false, regexPattern: @"(?<!\d)\d{5}-\d{3}(?!\d)")
        };

        var result = _indexer.ExtractIndexes(text, indexers);

        Assert.Equal("2026/98765", result["numeroProcesso"].Value);
        Assert.Equal("45/2026", result["numeroContrato"].Value);
        Assert.Equal("12.345.678/0001-99", result["cnpj"].Value);
        Assert.Contains("123.456.789-00", ((object[])result["cpfs"].Value!).Cast<string>());
        Assert.Contains("445566", ((object[])result["matriculas"].Value!).Cast<string>());
        Assert.Contains("12345.67", ((object[])result["valores"].Value!).Cast<string>());
        Assert.Contains("14/03/2026", ((object[])result["datas"].Value!).Cast<string>());
        Assert.Contains("contato@solucoesintegradas.com.br", ((object[])result["emails"].Value!).Cast<string>());
        Assert.Contains("(65) 99999-9999", ((object[])result["telefones"].Value!).Cast<string>());
        Assert.Equal("78000-000", result["cep"].Value);
    }

    [Fact]
    public void Should_Return_Structured_Value_With_Regex_Source_And_Full_Confidence()
    {
        const string text = "PROCESSO ADMINISTRATIVO Nº 2026/98765";
        var documentClassId = Guid.NewGuid();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(documentClassId, "numeroProcesso", "Número do Processo", "", IndexerDataType.Text, true, false, regexPattern: @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)")
        };

        var result = _indexer.ExtractIndexes(text, indexers);

        var value = result["numeroProcesso"];
        Assert.Equal("2026/98765", value.Value);
        Assert.Equal("Regex", value.Source);
        Assert.Equal(1.0, value.Confidence);
    }

    [Fact]
    public void Should_Ignore_Indexers_Without_Regex_Pattern()
    {
        const string text = "Qualquer texto.";
        var documentClassId = Guid.NewGuid();
        var indexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(documentClassId, "assunto", "Assunto", "", IndexerDataType.Text, false, false)
        };

        var result = _indexer.ExtractIndexes(text, indexers);

        Assert.Empty(result);
    }
}
