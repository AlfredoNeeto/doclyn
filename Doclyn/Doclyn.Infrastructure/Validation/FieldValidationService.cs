using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Doclyn.Infrastructure.Validation;

public sealed class FieldValidationService : IFieldValidationService
{
    private readonly FieldConfidenceOptions _options;

    public FieldValidationService(IOptions<FieldConfidenceOptions> options)
    {
        _options = options.Value;
    }

    public ValidationStatus DetermineStatus(decimal confidence)
    {
        if (confidence >= _options.ValidatedThreshold)
            return ValidationStatus.Validated;

        if (confidence >= _options.ReviewThreshold)
            return ValidationStatus.NeedsReview;

        return ValidationStatus.Rejected;
    }
}
