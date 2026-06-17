using Doclyn.Application.Common.Interfaces;
using Doclyn.Infrastructure.Jobs;
using Hangfire;

namespace Doclyn.Infrastructure.Processing;

public sealed class HangfireDocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireDocumentProcessingQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid documentId)
    {
        _backgroundJobClient.Enqueue<ProcessDocumentJob>(
            job => job.RunAsync(documentId, CancellationToken.None));
    }
}
