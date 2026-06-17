using MediatR;

namespace Doclyn.Application.Documents.Restore;

public sealed record RestoreDocumentCommand(Guid DocumentId) : IRequest;
