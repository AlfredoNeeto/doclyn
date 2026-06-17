namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentProcessingQueue
{
    void Enqueue(Guid documentId);
}
