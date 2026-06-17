using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Doclyn.Infrastructure.DocumentClassIndexers;

public sealed class DocumentClassIndexerCatalogService : IDocumentClassIndexerCatalogService
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentClassIndexerCatalogService> _logger;

    public DocumentClassIndexerCatalogService(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<DocumentClassIndexerCatalogService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<DocumentClassIndexer>> GetActiveByDocumentClassAsync(
        Guid documentClassId,
        CancellationToken cancellationToken = default)
    {
        var indexers = await _context.DocumentClassIndexers
            .AsNoTracking()
            .Where(dci => dci.DocumentClassId == documentClassId && dci.IsActive)
            .OrderBy(dci => dci.DisplayName)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "DocumentClassIndexersLoaded: {Count} active indexers loaded for class {DocumentClassId}.",
            indexers.Count,
            documentClassId);

        return indexers;
    }

    public async Task<DocumentClassIndexer> CreateAsync(
        Guid documentClassId,
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint = null,
        string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureDocumentClassExistsAsync(documentClassId, cancellationToken);
        await EnsureNameIsUniqueAsync(documentClassId, name, cancellationToken: cancellationToken);

        var indexer = DocumentClassIndexer.Create(
            documentClassId,
            name,
            displayName,
            description,
            dataType,
            isRequired,
            isMultiple,
            extractionHint,
            regexPattern);

        _context.DocumentClassIndexers.Add(indexer);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "DocumentClassIndexerCreated: {IndexerName} for class {DocumentClassId}.",
            indexer.Name,
            documentClassId);

        return indexer;
    }

    public async Task<DocumentClassIndexer> UpdateAsync(
        Guid documentClassId,
        Guid id,
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint = null,
        string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        var indexer = await _context.DocumentClassIndexers
            .FirstOrDefaultAsync(dci => dci.Id == id && dci.DocumentClassId == documentClassId, cancellationToken)
            ?? throw new InvalidOperationException("Indexer not found.");

        if (!indexer.IsActive)
            throw new InvalidOperationException("Cannot update a disabled indexer.");

        await EnsureNameIsUniqueAsync(documentClassId, name, excludeId: id, cancellationToken: cancellationToken);

        indexer.Update(
            name,
            displayName,
            description,
            dataType,
            isRequired,
            isMultiple,
            extractionHint,
            regexPattern);

        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "DocumentClassIndexerUpdated: {IndexerName} for class {DocumentClassId}.",
            indexer.Name,
            documentClassId);

        return indexer;
    }

    public async Task DisableAsync(
        Guid documentClassId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var indexer = await _context.DocumentClassIndexers
            .FirstOrDefaultAsync(dci => dci.Id == id && dci.DocumentClassId == documentClassId, cancellationToken)
            ?? throw new InvalidOperationException("Indexer not found.");

        indexer.Disable();
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "DocumentClassIndexerDisabled: {IndexerName} for class {DocumentClassId}.",
            indexer.Name,
            documentClassId);
    }

    private async Task EnsureDocumentClassExistsAsync(
        Guid documentClassId,
        CancellationToken cancellationToken)
    {
        var exists = await _context.DocumentClasses
            .AnyAsync(dc => dc.Id == documentClassId, cancellationToken);

        if (!exists)
            throw new InvalidOperationException("Document class not found.");
    }

    private async Task EnsureNameIsUniqueAsync(
        Guid documentClassId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = DocumentClassIndexer.NormalizeName(name);

        var query = _context.DocumentClassIndexers
            .AsNoTracking()
            .Where(dci =>
                dci.DocumentClassId == documentClassId &&
                dci.Name == normalizedName &&
                dci.IsActive);

        if (excludeId.HasValue)
            query = query.Where(dci => dci.Id != excludeId.Value);

        var exists = await query.AnyAsync(cancellationToken);

        if (exists)
            throw new InvalidOperationException($"An active indexer with name '{normalizedName}' already exists for this document class.");
    }
}
