using MediatR;

namespace Doclyn.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType);
