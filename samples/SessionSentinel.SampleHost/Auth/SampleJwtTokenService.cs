using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SessionSentinel.SampleHost.Auth;

public sealed class SampleJwtTokenService
{
    private readonly SampleJwtOptions _options;

    public SampleJwtTokenService(IOptions<SampleJwtOptions> options)
    {
        _options = options.Value;
    }

    public SampleTokenResult CreateToken(SampleUser user)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim("sub", user.UserId),
            new Claim("unique_name", user.UserName),
            new Claim("jti", sessionId)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new SampleTokenResult(new JwtSecurityTokenHandler().WriteToken(token), sessionId, expiresAtUtc);
    }
}
