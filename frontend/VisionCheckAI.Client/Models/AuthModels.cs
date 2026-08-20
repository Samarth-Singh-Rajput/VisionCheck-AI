namespace VisionCheckAI.Client.Models;

/// <summary>Payload for POST /api/auth/login.</summary>
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response from POST /api/auth/login. The backend contract only guarantees a token;
/// user details are read from the payload when present, otherwise from the JWT claims.
/// </summary>
public sealed class LoginResponse
{
    public string? Token { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public UserDto? User { get; set; }

    /// <summary>Some APIs return the user fields flat rather than nested.</summary>
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Role { get; set; }

    public string? ResolveToken() => !string.IsNullOrWhiteSpace(Token) ? Token : AccessToken;
}

public sealed class UserDto
{
    public string? Id { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
}

/// <summary>The signed-in user as held in browser storage.</summary>
public sealed class UserSession
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Inspector;
    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsExpired => ExpiresAtUtc is not null && ExpiresAtUtc.Value <= DateTime.UtcNow.AddSeconds(30);

    public string Initials
    {
        get
        {
            var source = string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;
            if (string.IsNullOrWhiteSpace(source)) return "?";

            var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}

public static class UserRoles
{
    public const string Inspector = "Inspector";
    public const string Supervisor = "Supervisor";
    public const string Administrator = "Administrator";

    /// <summary>Roles permitted to override an AI verdict.</summary>
    public const string CanOverride = "Supervisor,Administrator";

    public static bool CanOverrideResult(string? role) =>
        string.Equals(role, Supervisor, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Administrator, StringComparison.OrdinalIgnoreCase);
}
