using MediatR;

namespace Doclyn.Application.Auth.Logout;

public sealed record LogoutCommand(
    string RefreshToken) : IRequest;
