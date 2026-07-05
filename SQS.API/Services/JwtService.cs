using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SQS.API.Models;

namespace SQS.API.Services;

/// <summary>
/// Tạo và validate JWT token.
/// </summary>
public class JwtService
{
    private readonly IConfiguration _config;
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int    _expiryHours;

    public JwtService(IConfiguration config)
    {
        _config      = config;
        _key         = config["Jwt:Key"]      ?? throw new InvalidOperationException("Jwt:Key not configured");
        _issuer      = config["Jwt:Issuer"]   ?? "SQS.API";
        _audience    = config["Jwt:Audience"] ?? "SQS.Clients";
        _expiryHours = int.Parse(config["Jwt:ExpiryHours"] ?? "8");
    }

    /// <summary>
    /// Tạo JWT token cho người dùng đã xác thực.
    /// </summary>
    public (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_expiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name,  user.Name),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               user.Role.ToString()),
            new("role",                        user.Role.ToString()),  // Cho Flutter dễ đọc
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>
    /// Lấy UserId từ claims của HttpContext.
    /// </summary>
    public static int GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new UnauthorizedAccessException("Token không hợp lệ");
        return int.Parse(sub);
    }
}
