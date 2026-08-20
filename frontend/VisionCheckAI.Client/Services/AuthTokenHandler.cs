using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace VisionCheckAI.Client.Services;

/// <summary>
/// Attaches the bearer token to every outgoing request and, on a 401, clears the
/// session and sends the user back to the login page.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly SessionStore _session;
    private readonly NavigationManager _navigation;

    public AuthTokenHandler(SessionStore session, NavigationManager navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _session.Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        request.Options.TryGetValue(ApiRequestOptions.AllowAnonymous, out var allowAnonymous);
        if (allowAnonymous)
        {
            return response;
        }

        await _session.SignOutAsync();

        var returnUrl = Uri.EscapeDataString(
            _navigation.ToBaseRelativePath(_navigation.Uri));

        _navigation.NavigateTo(
            string.IsNullOrWhiteSpace(returnUrl) ? "login" : $"login?returnUrl={returnUrl}",
            forceLoad: false,
            replace: true);

        return response;
    }
}
