using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Doclyn.Infrastructure.DocumentClassIndexers;

public sealed class DocumentClassIndexerSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentClassIndexerSeeder> _logger;

    private static readonly (string ClassName, (string Name, string DisplayName, string Description, IndexerDataType DataType, bool IsRequired, bool IsMultiple, string? ExtractionHint, string? RegexPattern)[] Indexers)[] ClassIndexers =
    [
        ("RELATORIO_TECNICO_PRELIMINAR",
        [
            ("numeroProcesso", "Número do Processo", "Número do processo administrativo.", IndexerDataType.Text, true, false, null, @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)"),
            ("numeroContrato", "Número do Contrato", "Número do contrato citado.", IndexerDataType.Text, false, false, null, @"Contrato\s+n[º°]?\s*([\d\/.-]+)"),
            ("orgao", "Órgão", "Órgão público mencionado.", IndexerDataType.Text, false, false, null, null),
            ("empresa", "Empresa", "Razão social da empresa.", IndexerDataType.Text, false, false, null, null),
            ("cnpj", "CNPJ", "CNPJ encontrado.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            ("cpfs", "CPFs", "CPFs encontrados.", IndexerDataType.Cpf, false, true, null, @"\d{3}\.\d{3}\.\d{3}-\d{2}"),
            ("matriculas", "Matrículas", "Matrículas funcionais.", IndexerDataType.Text, false, true, null, @"matr[íi]cula\s+funcional\s+(\d+)"),
            ("datas", "Datas", "Datas DD/MM/AAAA.", IndexerDataType.Date, false, true, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("valores", "Valores", "Valores monetários.", IndexerDataType.Currency, false, true, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            ("notaFiscal", "Nota Fiscal", "Número da nota fiscal.", IndexerDataType.Text, false, false, null, @"Nota\s+Fiscal\s+n[º°]?\s*([A-Z0-9\-]+)"),
            ("oficio", "Ofício", "Número do ofício.", IndexerDataType.Text, false, false, null, @"Of[ií]cio\s+n[º°]?\s*([\d\/-]+)"),
            ("emails", "E-mails", "Endereços de e-mail.", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            ("telefones", "Telefones", "Números de telefone.", IndexerDataType.Phone, false, true, null, @"\(\d{2}\)\s*\d{4,5}-\d{4}"),
            ("endereco", "Endereço", "Endereço completo.", IndexerDataType.Text, false, false, null, null),
            ("cep", "CEP", "CEP encontrado.", IndexerDataType.Cep, false, false, null, @"(?<!\d)\d{5}-\d{3}(?!\d)"),
            ("agencia", "Agência", "Agência bancária.", IndexerDataType.Text, false, false, null, @"ag[eê]ncia\s+(\d+)"),
            ("contaCorrente", "Conta Corrente", "Conta corrente.", IndexerDataType.Text, false, false, null, @"conta\s+(?:corrente\s+)?([\d-]+)"),
            ("pessoasCitadas", "Pessoas Citadas", "Nomes de pessoas.", IndexerDataType.Array, false, true, "IA.", null),
            ("palavrasChave", "Palavras-chave", "Termos chave.", IndexerDataType.Array, false, true, "IA.", null),
            ("assunto", "Assunto", "Assunto principal.", IndexerDataType.Text, false, false, "IA.", null),
            ("resumo", "Resumo", "Resumo do conteúdo.", IndexerDataType.Text, false, false, "IA.", null)
        ]),
        ("CONTRATO_ADMINISTRATIVO",
        [
            ("numeroContrato", "Número do Contrato", "Número do contrato administrativo.", IndexerDataType.Text, true, false, null, @"Contrato\s+n[º°]?\s*([\d\/.-]+)"),
            ("contratante", "Contratante", "Órgão ou empresa contratante.", IndexerDataType.Text, false, false, null, null),
            ("contratada", "Contratada", "Empresa contratada.", IndexerDataType.Text, false, false, null, null),
            ("objeto", "Objeto", "Objeto do contrato.", IndexerDataType.Text, false, false, null, null),
            ("valor", "Valor", "Valor do contrato.", IndexerDataType.Currency, false, false, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            ("vigenciaInicio", "Início da Vigência", "Data de início da vigência.", IndexerDataType.Date, false, false, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("vigenciaFim", "Fim da Vigência", "Data de fim da vigência.", IndexerDataType.Date, false, false, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("cnpj", "CNPJ", "CNPJ das partes.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            ("datas", "Datas", "Datas relevantes.", IndexerDataType.Date, false, true, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("valores", "Valores", "Valores monetários.", IndexerDataType.Currency, false, true, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            ("emails", "E-mails", "E-mails.", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            ("telefones", "Telefones", "Telefones.", IndexerDataType.Phone, false, true, null, @"\(\d{2}\)\s*\d{4,5}-\d{4}"),
            ("assunto", "Assunto", "Assunto do contrato.", IndexerDataType.Text, false, false, "IA.", null)
        ]),
        ("OFICIO",
        [
            ("numeroOficio", "Número do Ofício", "Número do ofício.", IndexerDataType.Text, true, false, null, @"Of[ií]cio\s+n[º°]?\s*([\d\/-]+)"),
            ("destinatario", "Destinatário", "Destinatário do ofício.", IndexerDataType.Text, false, false, null, null),
            ("remetente", "Remetente", "Remetente do ofício.", IndexerDataType.Text, false, false, null, null),
            ("assunto", "Assunto", "Assunto do ofício.", IndexerDataType.Text, false, false, null, null),
            ("data", "Data", "Data do ofício.", IndexerDataType.Date, false, false, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("orgao", "Órgão", "Órgão relacionado.", IndexerDataType.Text, false, false, null, null),
            ("cnpj", "CNPJ", "CNPJ mencionado.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            ("emails", "E-mails", "E-mails.", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            ("telefones", "Telefones", "Telefones.", IndexerDataType.Phone, false, true, null, @"\(\d{2}\)\s*\d{4,5}-\d{4}")
        ]),
        ("NOTA_FISCAL",
        [
            ("numeroNota", "Número da Nota", "Número da nota fiscal.", IndexerDataType.Text, true, false, null, @"Nota\s+Fiscal\s+n[º°]?\s*([A-Z0-9\-]+)"),
            ("emitente", "Emitente", "Emitente da nota.", IndexerDataType.Text, false, false, null, null),
            ("destinatario", "Destinatário", "Destinatário da nota.", IndexerDataType.Text, false, false, null, null),
            ("valor", "Valor", "Valor total.", IndexerDataType.Currency, false, false, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            ("dataEmissao", "Data de Emissão", "Data de emissão.", IndexerDataType.Date, false, false, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("cnpj", "CNPJ", "CNPJ do emitente.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            ("chaveAcesso", "Chave de Acesso", "Chave de acesso da NF-e.", IndexerDataType.Text, false, false, null, @"\d{44}")
        ]),
        ("PETICAO_JUDICIAL",
        [
            ("numeroProcesso", "Número do Processo", "Número do processo judicial.", IndexerDataType.Text, true, false, null, @"\d{7}-\d{2}\.\d{4}\.\d{1}\.\d{2}\.\d{4}"),
            ("requerente", "Requerente", "Parte requerente.", IndexerDataType.Text, false, false, null, null),
            ("requerido", "Requerido", "Parte requerida.", IndexerDataType.Text, false, false, null, null),
            ("vara", "Vara", "Vara do processo.", IndexerDataType.Text, false, false, null, null),
            ("tribunal", "Tribunal", "Tribunal do processo.", IndexerDataType.Text, false, false, null, null),
            ("data", "Data", "Data da petição.", IndexerDataType.Date, false, false, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("cnpj", "CNPJ", "CNPJ das partes.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            ("cpfs", "CPFs", "CPFs.", IndexerDataType.Cpf, false, true, null, @"\d{3}\.\d{3}\.\d{3}-\d{2}"),
            ("datas", "Datas", "Datas relevantes.", IndexerDataType.Date, false, true, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("emails", "E-mails", "E-mails.", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")
        ]),
        ("DOCUMENTO_DESCONHECIDO",
        [
            ("assunto", "Assunto", "Assunto principal.", IndexerDataType.Text, false, false, "IA.", null),
            ("resumo", "Resumo", "Resumo do conteúdo.", IndexerDataType.Text, false, false, "IA.", null),
            ("orgao", "Órgão", "Órgão público.", IndexerDataType.Text, false, false, null, null),
            ("empresa", "Empresa", "Razão social.", IndexerDataType.Text, false, false, null, null),
            ("cnpj", "CNPJ", "CNPJ.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            ("datas", "Datas", "Datas.", IndexerDataType.Date, false, true, null, @"\d{2}\/\d{2}\/\d{4}"),
            ("valores", "Valores", "Valores monetários.", IndexerDataType.Currency, false, true, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            ("emails", "E-mails", "E-mails.", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            ("telefones", "Telefones", "Telefones.", IndexerDataType.Phone, false, true, null, @"\(\d{2}\)\s*\d{4,5}-\d{4}"),
            ("palavrasChave", "Palavras-chave", "Termos relevantes.", IndexerDataType.Array, false, true, "IA.", null)
        ])
    ];

    private static readonly (string Name, string DisplayName, string Description, IndexerDataType DataType, bool IsRequired, bool IsMultiple, string? ExtractionHint, string? RegexPattern)[] NewClassIndexers =
    [
        ("assunto", "Assunto", "Assunto principal do documento.", IndexerDataType.Text, false, false, "IA.", null),
        ("resumo", "Resumo", "Resumo do conteúdo.", IndexerDataType.Text, false, false, "IA.", null),
        ("orgao", "Órgão", "Órgão público.", IndexerDataType.Text, false, false, null, null),
        ("empresa", "Empresa", "Razão social.", IndexerDataType.Text, false, false, null, null),
        ("cnpj", "CNPJ", "CNPJ.", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
        ("datas", "Datas", "Datas.", IndexerDataType.Date, false, true, null, @"\d{2}\/\d{2}\/\d{4}"),
        ("valores", "Valores", "Valores monetários.", IndexerDataType.Currency, false, true, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
        ("emails", "E-mails", "E-mails.", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
        ("telefones", "Telefones", "Telefones.", IndexerDataType.Phone, false, true, null, @"\(\d{2}\)\s*\d{4,5}-\d{4}"),
        ("palavrasChave", "Palavras-chave", "Termos relevantes.", IndexerDataType.Array, false, true, "IA.", null)
    ];

    public DocumentClassIndexerSeeder(
        IServiceProvider serviceProvider,
        ILogger<DocumentClassIndexerSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Seeding document class indexers...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            foreach (var (className, indexers) in ClassIndexers)
            {
                await SeedIndexersForClassAsync(context, unitOfWork, className, indexers, cancellationToken);
            }

            _logger.LogInformation("Document class indexers seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed document class indexers. The application will continue starting.");
        }
    }

    public static async Task SeedGenericIndexersForNewClassAsync(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        Guid documentClassId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var existingNames = await context.DocumentClassIndexers
            .AsNoTracking()
            .Where(dci => dci.DocumentClassId == documentClassId)
            .Select(dci => dci.Name)
            .ToHashSetAsync(cancellationToken);

        foreach (var (name, displayName, description, dataType, isRequired, isMultiple, extractionHint, regexPattern) in NewClassIndexers)
        {
            var normalizedName = DocumentClassIndexer.NormalizeName(name);

            if (existingNames.Contains(normalizedName))
                continue;

            context.DocumentClassIndexers.Add(DocumentClassIndexer.Create(
                documentClassId, name, displayName, description,
                dataType, isRequired, isMultiple, extractionHint, regexPattern));

            existingNames.Add(normalizedName);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        logger.LogInformation("Generic indexers seeded for new document class {DocumentClassId}.", documentClassId);
    }

    private async Task SeedIndexersForClassAsync(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        string normalizedClassName,
        (string Name, string DisplayName, string Description, IndexerDataType DataType, bool IsRequired, bool IsMultiple, string? ExtractionHint, string? RegexPattern)[] indexers,
        CancellationToken cancellationToken)
    {
        var documentClass = await context.DocumentClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                dc => dc.Name == DocumentClass.NormalizeName(normalizedClassName),
                cancellationToken);

        if (documentClass is null)
        {
            _logger.LogWarning("Document class {ClassName} not found. Skipping indexer seeding.", normalizedClassName);
            return;
        }

        var existingNames = await context.DocumentClassIndexers
            .AsNoTracking()
            .Where(dci => dci.DocumentClassId == documentClass.Id)
            .Select(dci => dci.Name)
            .ToHashSetAsync(cancellationToken);

        foreach (var (name, displayName, description, dataType, isRequired, isMultiple, extractionHint, regexPattern) in indexers)
        {
            var normalizedName = DocumentClassIndexer.NormalizeName(name);

            if (existingNames.Contains(normalizedName))
                continue;

            context.DocumentClassIndexers.Add(DocumentClassIndexer.Create(
                documentClass.Id, name, displayName, description,
                dataType, isRequired, isMultiple, extractionHint, regexPattern));

            existingNames.Add(normalizedName);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        _logger.LogInformation("Indexers seeded for document class {ClassName}.", normalizedClassName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
