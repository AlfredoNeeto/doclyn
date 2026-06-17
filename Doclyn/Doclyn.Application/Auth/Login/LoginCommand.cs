using Doclyn.Application.Common.DTOs.Auth;
using MediatR;

namespace Doclyn.Application.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResponse>;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserDto User);
