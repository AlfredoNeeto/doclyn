using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Auth.VerifyResetCode;

public sealed class VerifyResetCodeHandler : IRequestHandler<VerifyResetCodeCommand, VerifyResetCodeResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public VerifyResetCodeHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<VerifyResetCodeResponse> Handle(
        VerifyResetCodeCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var codeHash = _tokenService.HashRefreshToken(request.Code);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("Invalid verification code.");

        var resetRequest = await _context.PasswordResetRequests
            .Where(r =>
                r.UserId == user.Id &&
                r.CodeHash == codeHash &&
                !r.IsUsed)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetRequest is null)
            throw new InvalidOperationException("Invalid verification code.");

        if (resetRequest.IsExpired)
            throw new InvalidOperationException("Verification code expired.");

        if (resetRequest.IsBlocked)
            throw new InvalidOperationException("Too many attempts.");

        if (!resetRequest.IsCodeValid)
            throw new InvalidOperationException("Invalid verification code.");

        // Validação bem-sucedida: incrementa tentativas e marca código como usado
        resetRequest.IncrementAttempt();
        resetRequest.MarkCodeAsUsed();

        // Gera reset token temporário e armazena seu hash
        var resetToken = _tokenService.GenerateRefreshToken();
        var resetTokenHash = _tokenService.HashRefreshToken(resetToken);

        resetRequest.SetResetToken(resetTokenHash);

        _context.PasswordResetRequests.Update(resetRequest);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new VerifyResetCodeResponse(resetToken);
    }
}
