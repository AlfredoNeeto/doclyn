using Doclyn.Application.Common.DTOs.Auth;
using Doclyn.Domain.Enums;
using MediatR;

namespace Doclyn.Application.Auth.Register;

public sealed record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    UserRole Role = UserRole.Operator) : IRequest<RegisterUserResponse>;

public sealed record RegisterUserResponse(UserDto User);
