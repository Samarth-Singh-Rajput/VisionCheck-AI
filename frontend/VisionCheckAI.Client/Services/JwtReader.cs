using System.Text.Json;

namespace VisionCheckAI.Client.Services;

/// <summary>Reads claims out of a JWT payload without validating the signature.</summary>
public static class JwtReader
{
    public static IReadOnlyDictionary<string, string> ReadClaims(string? token)
    {
        var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(token)) return empty;

        var parts = token.Split('.');
        if (parts.Length < 2) return empty;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(FromBase64Url(parts[1]));
            using var document = JsonDocument.Parse(json);

            var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                claims[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Array => property.Value.EnumerateArray().FirstOrDefault().ToString(),
                    _ => property.Value.ToString()
                };
            }

            return claims;
        }
        catch
        {
            return empty;
        }
    }

    public static DateTime? ReadExpiry(string? token)
    {
        var claims = ReadClaims(token);
        if (claims.TryGetValue("exp", out var exp) && long.TryParse(exp, out var seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        return null;
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };

        return Convert.FromBase64String(padded);
    }
}
