using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace Doclyn.IntegrationTests.Common;

public static class TestAuthHelper
{
    private const string Secret = "your-dev-secret-key-must-be-at-least-32-characters-long";

    public static string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "Doclyn",
            audience: "Doclyn",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static User CreateOperator(string email = "operator@doclyn.local")
    {
        return User.Create("Operator", email, "hash", UserRole.Operator);
    }

    public static User CreateAdmin(string email = "admin@doclyn.local")
    {
        return User.Create("Admin", email, "hash", UserRole.Admin);
    }
}
