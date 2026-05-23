using Microsoft.AspNetCore.Http;

namespace SessionSentinel.WebApi;

public static class ClientIpResolver
{
    public const string ForwardedForHeaderName = "X-Forwarded-For";

    public static string Resolve(HttpContext context)
    {
        var forwardedFor = context.Request.Headers[ForwardedForHeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
