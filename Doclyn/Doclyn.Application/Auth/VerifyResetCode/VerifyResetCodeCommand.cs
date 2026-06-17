using MediatR;

namespace Doclyn.Application.Auth.VerifyResetCode;

public sealed record VerifyResetCodeCommand(
    string Email,
    string Code) : IRequest<VerifyResetCodeResponse>;

public sealed record VerifyResetCodeResponse(string ResetToken);
