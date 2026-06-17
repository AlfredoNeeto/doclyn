using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public ForgotPasswordHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Sempre busca o usuário, mas nunca revela se existe ou não.
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            // Retorna silenciosamente para evitar enumeração de usuários.
            return;
        }

        // Rate limit por usuário: máximo 3 solicitações por hora
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentRequestsCount = await _context.PasswordResetRequests
            .CountAsync(r =>
                r.UserId == user.Id &&
                r.CreatedAt >= oneHourAgo,
                cancellationToken);

        if (recentRequestsCount >= 3)
        {
            // Silenciosamente ignora para não revelar que o usuário existe.
            return;
        }

        // Gera código numérico de 6 dígitos
        var code = GenerateSixDigitCode();
        var codeHash = _tokenService.HashRefreshToken(code);

        // Gera reset token temporário
        var resetToken = _tokenService.GenerateRefreshToken();
        var resetTokenHash = _tokenService.HashRefreshToken(resetToken);

        // Invalida solicitações anteriores do mesmo usuário
        var existingRequests = await _context.PasswordResetRequests
            .Where(r => r.UserId == user.Id && !r.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingRequests)
        {
            existing.MarkCodeAsUsed();
            _context.PasswordResetRequests.Update(existing);
        }

        var resetRequest = PasswordResetRequest.Create(
            user.Id,
            codeHash);

        _context.PasswordResetRequests.Add(resetRequest);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _emailService.SendPasswordResetCodeAsync(
            user.Email,
            user.Name,
            code,
            cancellationToken);
    }

    private static string GenerateSixDigitCode()
    {
        // Garante 6 dígitos, inclusive com zeros à esquerda
        var code = Random.Shared.Next(0, 1_000_000);
        return code.ToString("D6");
    }
}
