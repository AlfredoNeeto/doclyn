using FluentValidation;

namespace Doclyn.Application.Auth.VerifyResetCode;

public sealed class VerifyResetCodeValidator : AbstractValidator<VerifyResetCodeCommand>
{
    public VerifyResetCodeValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Matches(@"^\d{6}$").WithMessage("Code must be 6 digits.");
    }
}
