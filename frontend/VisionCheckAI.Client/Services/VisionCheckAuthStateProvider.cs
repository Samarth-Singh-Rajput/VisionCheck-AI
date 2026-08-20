using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace VisionCheckAI.Client.Services;

/// <summary>Projects the stored session into a ClaimsPrincipal for route and UI authorization.</summary>
public sealed class VisionCheckAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly SessionStore _session;

    public VisionCheckAuthStateProvider(SessionStore session)
    {
        _session = session;
        _session.Changed += OnSessionChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        if (!_session.IsAuthenticated || _session.User is null)
        {
            return Anonymous;
        }

        var user = _session.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("display_name", user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "visioncheck-jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void OnSessionChanged() => NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));

    public void Dispose() => _session.Changed -= OnSessionChanged;
}
