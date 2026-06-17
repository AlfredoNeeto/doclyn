namespace Doclyn.Application.Documents.GetLogs;

public sealed record GetDocumentLogResponse(
    Guid Id,
    string Step,
    string Status,
    string Message,
    DateTime CreatedAt);
