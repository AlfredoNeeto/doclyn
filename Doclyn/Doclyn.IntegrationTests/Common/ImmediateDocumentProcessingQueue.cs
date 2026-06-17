using Doclyn.Application.Common.Interfaces;
using Doclyn.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Doclyn.IntegrationTests.Common;

public sealed class ImmediateDocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ImmediateDocumentProcessingQueue(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Enqueue(Guid documentId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ProcessDocumentJob>();
        job.RunAsync(documentId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
