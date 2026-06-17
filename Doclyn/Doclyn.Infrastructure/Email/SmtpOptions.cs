namespace Doclyn.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; init; } = "smtp.hostinger.com";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string From { get; init; } = "contato@doclyn.com.br";
    public bool EnableSsl { get; init; } = true;
}
