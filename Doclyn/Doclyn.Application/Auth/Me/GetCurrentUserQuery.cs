using Doclyn.Application.Common.DTOs.Auth;
using MediatR;

namespace Doclyn.Application.Auth.Me;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserResponse>;

public sealed record CurrentUserResponse(UserDto User);
