using FluentValidation;
using Microsoft.Extensions.Options;

namespace Doclyn.Application.Documents.Upload;

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator(IOptions<DocumentOptions> options)
    {
        var maxSizeInBytes = options.Value.MaxUploadSizeInMb * 1024L * 1024L;

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .Equal("application/pdf")
            .WithMessage("Only PDF files are allowed.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("File cannot be empty.")
            .LessThanOrEqualTo(maxSizeInBytes)
            .WithMessage($"File size exceeds the allowed limit of {options.Value.MaxUploadSizeInMb} MB.");

        RuleFor(x => x.FileName)
            .Must(fileName =>
                !string.IsNullOrWhiteSpace(fileName) &&
                Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only PDF files are allowed.");
    }
}
