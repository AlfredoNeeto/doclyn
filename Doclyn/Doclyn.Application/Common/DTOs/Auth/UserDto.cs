using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;

namespace Doclyn.Application.Common.DTOs.Auth;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role);

public static class UserDtoExtensions
{
    public static UserDto ToDto(this User user)
        => new(user.Id, user.Name, user.Email, user.Role);
}
