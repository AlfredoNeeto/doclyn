using MediatR;

namespace Doclyn.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    string ResetToken,
    string NewPassword) : IRequest;
