using MediatR;

namespace Doclyn.Application.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;
