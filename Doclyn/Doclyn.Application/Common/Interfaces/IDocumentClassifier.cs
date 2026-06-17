using Doclyn.Application.Documents.Processing;

namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentClassifier
{
    DocumentClassificationResult Classify(string text);
}
