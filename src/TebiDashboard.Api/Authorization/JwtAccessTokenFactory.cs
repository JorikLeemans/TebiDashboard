using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TebiDashboard.Api.Authorization;

/// <summary>
/// Signs a short-lived JWT that carries the user's email and role.
/// The API validates this token; the client-supplied role header is not trusted.
/// </summary>
public sealed class JwtAccessTokenFactory(string signingKey, string issuer, string audience, TimeSpan lifetime)
{
    public string Create(string email, string role)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            // Short "role" claim. JwtBearer is configured with RoleClaimType = "role".
            new Claim("role", role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
