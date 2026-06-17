namespace Doclyn.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(
        string email,
        string name,
        string code,
        CancellationToken cancellationToken = default);
}
