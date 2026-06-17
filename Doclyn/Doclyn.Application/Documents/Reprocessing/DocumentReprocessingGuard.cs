using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;

namespace Doclyn.Application.Documents.Reprocessing;

internal static class DocumentReprocessingGuard
{
    public static Guid EnsureAuthenticated(ICurrentUserService currentUserService)
    {
        if (!currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return currentUserService.UserId.Value;
    }

    public static void EnsureDocumentExists(Document? document)
    {
        if (document is null)
        {
            throw new NotFoundException("Document not found.");
        }
    }

    public static void EnsureCanAccess(Document document, ICurrentUserService currentUserService, Guid currentUserId)
    {
        if (currentUserService.Role != UserRole.Admin.ToString() && document.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }
    }

    public static bool CanEnqueue(Document document)
    {
        return document.DocumentStatus != DocumentStatus.Processing;
    }
}
