namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentProcessingService
{
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
}
