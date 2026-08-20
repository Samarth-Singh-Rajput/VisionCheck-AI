using System.Net;
using System.Net.Http.Json;
using VisionCheckAI.Client.Models;

namespace VisionCheckAI.Client.Services;

public interface IAuthApi
{
    Task SignInAsync(string username, string password, CancellationToken cancellationToken = default);
    Task SignOutAsync();
}

public sealed class AuthApi : ApiServiceBase, IAuthApi
{
    private readonly SessionStore _session;

    public AuthApi(IHttpClientFactory httpClientFactory, ApiSettings settings, SessionStore session)
        : base(httpClientFactory, settings)
    {
        _session = session;
    }

    public async Task SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(
                new LoginRequest { Username = username, Password = password },
                options: JsonOptions)
        };

        request.Options.Set(ApiRequestOptions.AllowAnonymous, true);

        LoginResponse payload;

        try
        {
            payload = await SendAsync<LoginResponse>(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized ||
                                      ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ApiException("Username or password is incorrect.", ex.StatusCode, ex);
        }

        var token = payload.ResolveToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException("The API did not return an access token.");
        }

        await _session.SignInAsync(token, BuildSession(payload, token, username));
    }

    public Task SignOutAsync() => _session.SignOutAsync();

    private static UserSession BuildSession(LoginResponse payload, string token, string fallbackUsername)
    {
        var claims = JwtReader.ReadClaims(token);

        string? FromClaims(params string[] names)
        {
            foreach (var name in names)
            {
                if (claims.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        var role =
            payload.User?.Role
            ?? payload.Role
            ?? FromClaims("role", "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            ?? UserRoles.Inspector;

        var username =
            payload.User?.Username
            ?? payload.Username
            ?? FromClaims("unique_name", "preferred_username", "sub",
                          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
            ?? fallbackUsername;

        var displayName =
            payload.User?.DisplayName
            ?? payload.DisplayName
            ?? FromClaims("name", "given_name")
            ?? username;

        var id =
            payload.User?.Id
            ?? FromClaims("sub", "nameid",
                          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            ?? username;

        return new UserSession
        {
            Id = id,
            Username = username,
            DisplayName = displayName,
            Role = NormaliseRole(role),
            ExpiresAtUtc = payload.ExpiresAtUtc ?? JwtReader.ReadExpiry(token)
        };
    }

    private static string NormaliseRole(string role)
    {
        if (string.Equals(role, UserRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return UserRoles.Administrator;
        }

        if (string.Equals(role, UserRoles.Supervisor, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoles.Supervisor;
        }

        return string.Equals(role, UserRoles.Inspector, StringComparison.OrdinalIgnoreCase)
            ? UserRoles.Inspector
            : role;
    }
}
