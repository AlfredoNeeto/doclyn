using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Auth.Logout;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public LogoutHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
            return;

        storedToken.Revoke();
        _context.RefreshTokens.Update(storedToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
