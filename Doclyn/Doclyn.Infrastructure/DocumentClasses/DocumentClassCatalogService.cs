using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.DocumentClassIndexers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Doclyn.Infrastructure.DocumentClasses;

public sealed class DocumentClassCatalogService : IDocumentClassCatalogService
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentClassCatalogService> _logger;

    public DocumentClassCatalogService(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<DocumentClassCatalogService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DocumentClass?> FindByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = DocumentClass.NormalizeName(name);

        return await _context.DocumentClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                dc => dc.Name == normalizedName,
                cancellationToken);
    }

    public async Task<DocumentClass> GetOrCreateAsync(
        string name,
        string group,
        string subGroup,
        string description,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Document class {DocumentClassName} reused.",
                existing.Name);

            return existing;
        }

        var documentClass = DocumentClass.Create(
            name,
            group,
            subGroup,
            description,
            isSystemDefined: false);

        _context.DocumentClasses.Add(documentClass);
        await _unitOfWork.CommitAsync(cancellationToken);

        await DocumentClassIndexerSeeder.SeedGenericIndexersForNewClassAsync(
            _context, _unitOfWork, documentClass.Id, _logger, cancellationToken);

        _logger.LogInformation(
            "New document class created: {DocumentClassName}.",
            documentClass.Name);

        return documentClass;
    }

    public async Task RegisterExampleAsync(
        Guid documentClassId,
        Guid documentId,
        decimal confidence,
        CancellationToken cancellationToken = default)
    {
        var example = DocumentClassExample.Create(documentClassId, documentId, confidence);

        _context.DocumentClassExamples.Add(example);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Document class example registered: DocumentClassId={DocumentClassId}, DocumentId={DocumentId}, Confidence={Confidence}.",
            documentClassId,
            documentId,
            confidence);
    }

    public async Task<IReadOnlyList<DocumentClass>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentClasses
            .AsNoTracking()
            .Where(dc => dc.IsActive)
            .OrderBy(dc => dc.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentClass>> GetTopUsedAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentClassExamples
            .AsNoTracking()
            .GroupBy(dce => dce.DocumentClassId)
            .OrderByDescending(g => g.Count())
            .Take(take)
            .Select(g => g.First().DocumentClass)
            .ToListAsync(cancellationToken);
    }
}
