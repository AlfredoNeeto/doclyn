using System.Text.Json;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.GetReviewFields;

public sealed class GetReviewFieldsHandler : IRequestHandler<GetReviewFieldsQuery, GetReviewFieldsResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetReviewFieldsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetReviewFieldsResponse> Handle(
        GetReviewFieldsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
        {
            throw new NotFoundException("Document not found.");
        }

        if (_currentUserService.Role != UserRole.Admin.ToString() &&
            document.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        var extractedData = await _context.ExtractedData
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DocumentId == request.DocumentId, cancellationToken);

        if (extractedData is null)
        {
            return new GetReviewFieldsResponse(request.DocumentId, []);
        }

        var fields = new List<ReviewFieldItem>();

        if (extractedData.Data.RootElement.TryGetProperty("fields", out var fieldsElement))
        {
            foreach (var property in fieldsElement.EnumerateObject())
            {
                if (property.Value.TryGetProperty("validationStatus", out var statusElement)
                    && statusElement.GetString() == "NeedsReview")
                {
                    var value = property.Value.TryGetProperty("value", out var valueElement)
                        ? ParseJsonValue(valueElement)
                        : null;

                    var confidence = property.Value.TryGetProperty("confidence", out var confElement)
                        ? confElement.GetDecimal()
                        : 0m;

                    var source = property.Value.TryGetProperty("source", out var srcElement)
                        ? srcElement.GetString() ?? string.Empty
                        : string.Empty;

                    fields.Add(new ReviewFieldItem(
                        property.Name,
                        value,
                        confidence,
                        source,
                        "NeedsReview"));
                }
            }
        }

        return new GetReviewFieldsResponse(request.DocumentId, fields);
    }

    private static object? ParseJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonValue).ToArray(),
            JsonValueKind.Object => element.ToString(),
            _ => element.ToString()
        };
    }
}
