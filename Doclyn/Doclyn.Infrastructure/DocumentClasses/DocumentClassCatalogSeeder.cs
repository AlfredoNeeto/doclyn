using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Doclyn.Infrastructure.DocumentClasses;

public sealed class DocumentClassCatalogSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentClassCatalogSeeder> _logger;

    private static readonly (string Name, string Group, string SubGroup, string Description)[] SystemClasses =
    [
        ("RELATORIO_TECNICO_PRELIMINAR", "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO", "Relatório técnico preliminar."),
        ("CONTRATO_ADMINISTRATIVO", "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO", "Contrato administrativo."),
        ("OFICIO", "ADMINISTRATIVO", "COMUNICACAO", "Ofício administrativo."),
        ("NOTA_FISCAL", "FISCAL", "TRIBUTARIO", "Nota fiscal."),
        ("PETICAO_JUDICIAL", "JURIDICO", "PROCESSO_JUDICIAL", "Petição judicial."),
        ("DOCUMENTO_DESCONHECIDO", "OUTROS", "NAO_CLASSIFICADO", "Documento sem classificação conhecida.")
    ];

    public DocumentClassCatalogSeeder(
        IServiceProvider serviceProvider,
        ILogger<DocumentClassCatalogSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Seeding document class catalog...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            foreach (var (name, group, subGroup, description) in SystemClasses)
            {
                var normalizedName = DocumentClass.NormalizeName(name);

                var exists = await context.DocumentClasses
                    .AnyAsync(dc => dc.Name == normalizedName, cancellationToken);

                if (exists)
                {
                    _logger.LogInformation("Document class {DocumentClassName} already exists. Skipping.", normalizedName);
                    continue;
                }

                var documentClass = DocumentClass.Create(
                    name,
                    group,
                    subGroup,
                    description,
                    isSystemDefined: true);

                context.DocumentClasses.Add(documentClass);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Document class catalog seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed document class catalog. The application will continue starting.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
