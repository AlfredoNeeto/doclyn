namespace Doclyn.Domain.Entities;

public sealed class PasswordResetRequest : AuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string CodeHash { get; private set; } = string.Empty;
    public string ResetTokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ResetTokenExpiresAt { get; private set; }
    public int Attempts { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsResetTokenUsed { get; private set; }

    public const int MaxAttempts = 5;
    public const int CodeExpirationMinutes = 10;
    public const int ResetTokenExpirationMinutes = 5;

    // EF Core requer construtor sem parâmetros
    private PasswordResetRequest()
    {
    }

    public static PasswordResetRequest Create(
        Guid userId,
        string codeHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        var now = DateTime.UtcNow;

        return new PasswordResetRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
            ResetTokenHash = string.Empty,
            ExpiresAt = now.AddMinutes(CodeExpirationMinutes),
            ResetTokenExpiresAt = null,
            Attempts = 0,
            IsUsed = false,
            IsResetTokenUsed = false,
            CreatedAt = now
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsBlocked => Attempts >= MaxAttempts;
    public bool IsCodeValid => !IsUsed && !IsExpired && !IsBlocked;
    public bool IsResetTokenExpired => ResetTokenExpiresAt is not null && DateTime.UtcNow >= ResetTokenExpiresAt;
    public bool IsResetTokenValid => !IsResetTokenUsed && !IsResetTokenExpired && !string.IsNullOrEmpty(ResetTokenHash);

    public void IncrementAttempt()
    {
        Attempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCodeAsUsed()
    {
        if (IsUsed) return;
        IsUsed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetResetToken(string resetTokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resetTokenHash);
        ResetTokenHash = resetTokenHash;
        ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenExpirationMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkResetTokenAsUsed()
    {
        if (IsResetTokenUsed) return;
        IsResetTokenUsed = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
