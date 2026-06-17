using MediatR;

namespace Doclyn.Application.Documents.Upload;

public sealed record UploadDocumentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long Length) : IRequest<UploadDocumentResponse>;
