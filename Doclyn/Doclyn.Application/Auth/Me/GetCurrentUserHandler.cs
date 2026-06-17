using Doclyn.Application.Common.DTOs.Auth;
using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Auth.Me;

public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserResponse> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User not authenticated.");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        return new CurrentUserResponse(user.ToDto());
    }
}
