using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SessionSentinel.WebApi;

internal static class JwtTokenReader
{
    public static string ResolveSessionId(ClaimsPrincipal principal, string tokenHash) =>
        principal.FindFirst("jti")?.Value ?? tokenHash;

    public static DateTimeOffset? ResolveExpiry(ClaimsPrincipal principal, string rawToken)
    {
        var expClaim = principal.FindFirst("exp")?.Value;
        if (long.TryParse(expClaim, out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        var parts = rawToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            var mod = payload.Length % 4;
            if (mod > 0)
            {
                payload = payload.PadRight(payload.Length + (4 - mod), '=');
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("exp", out var expProperty))
            {
                return null;
            }

            return expProperty.ValueKind switch
            {
                JsonValueKind.Number when expProperty.TryGetInt64(out var number) => DateTimeOffset.FromUnixTimeSeconds(number),
                JsonValueKind.String when long.TryParse(expProperty.GetString(), out var textValue) => DateTimeOffset.FromUnixTimeSeconds(textValue),
                _ => null
            };
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
