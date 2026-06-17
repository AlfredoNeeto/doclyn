using Doclyn.Application.Common.DTOs.Auth;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = Doclyn.Domain.Entities.RefreshToken;

namespace Doclyn.Application.Auth.Login;

public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User is inactive.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);

        var refreshTokenEntity = RefreshTokenEntity.Create(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new LoginResponse(
            accessToken,
            refreshToken,
            ExpiresIn: 900,
            "Bearer",
            user.ToDto());
    }
}
