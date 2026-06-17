using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = Doclyn.Domain.Entities.RefreshToken;

namespace Doclyn.Application.Auth.RefreshToken;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public RefreshTokenHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = storedToken.User;
        if (!user.IsActive)
            throw new UnauthorizedAccessException("User is inactive.");

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);

        storedToken.ReplaceBy(newRefreshTokenHash);
        _context.RefreshTokens.Update(storedToken);

        var newTokenEntity = RefreshTokenEntity.Create(
            user.Id,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(newTokenEntity);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new RefreshTokenResponse(
            newAccessToken,
            newRefreshToken,
            ExpiresIn: 900,
            "Bearer");
    }
}
