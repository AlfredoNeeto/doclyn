using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Auth.ResetPassword;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetTokenHash = _tokenService.HashRefreshToken(request.ResetToken);

        var resetRequest = await _context.PasswordResetRequests
            .Include(r => r.User)
            .Where(r =>
                r.ResetTokenHash == resetTokenHash &&
                r.IsUsed &&
                !r.IsResetTokenUsed)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetRequest is null)
            throw new InvalidOperationException("Invalid reset token.");

        if (resetRequest.IsResetTokenExpired)
            throw new InvalidOperationException("Reset token expired.");

        if (!resetRequest.IsResetTokenValid)
            throw new InvalidOperationException("Invalid reset token.");

        var user = resetRequest.User;

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User is inactive.");

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(newPasswordHash);

        resetRequest.MarkResetTokenAsUsed();

        _context.Users.Update(user);
        _context.PasswordResetRequests.Update(resetRequest);

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
