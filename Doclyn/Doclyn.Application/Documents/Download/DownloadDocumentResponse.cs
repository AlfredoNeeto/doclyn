namespace Doclyn.Application.Documents.Download;

public sealed record DownloadDocumentResponse(
    Stream FileStream,
    string FileName,
    string ContentType);
