using Doclyn.Application.Common.Interfaces;
using Hangfire;

namespace Doclyn.Infrastructure.Jobs;

public sealed class ProcessDocumentJob
{
    private readonly IDocumentProcessingService _documentProcessingService;

    public ProcessDocumentJob(IDocumentProcessingService documentProcessingService)
    {
        _documentProcessingService = documentProcessingService;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task RunAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _documentProcessingService.ProcessAsync(documentId, cancellationToken);
    }
}
