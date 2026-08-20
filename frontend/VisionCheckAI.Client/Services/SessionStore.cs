using System.Text.Json;
using VisionCheckAI.Client.Models;

namespace VisionCheckAI.Client.Services;

/// <summary>Holds the JWT and signed-in user, mirrored into localStorage.</summary>
public sealed class SessionStore
{
    private const string TokenKey = "visioncheck.token";
    private const string UserKey = "visioncheck.user";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly BrowserStorage _storage;

    public SessionStore(BrowserStorage storage) => _storage = storage;

    public string? Token { get; private set; }
    public UserSession? User { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && User is not null && !User.IsExpired;

    public event Action? Changed;

    /// <summary>Rehydrates the session before the first render so route guards see the right state.</summary>
    public async Task InitializeAsync()
    {
        var token = await _storage.GetAsync(TokenKey);
        var userJson = await _storage.GetAsync(UserKey);

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userJson))
        {
            return;
        }

        try
        {
            var user = JsonSerializer.Deserialize<UserSession>(userJson, SerializerOptions);
            if (user is null || user.IsExpired)
            {
                await ClearStorageAsync();
                return;
            }

            Token = token;
            User = user;
        }
        catch (JsonException)
        {
            await ClearStorageAsync();
        }
    }

    public async Task SignInAsync(string token, UserSession user)
    {
        Token = token;
        User = user;

        await _storage.SetAsync(TokenKey, token);
        await _storage.SetAsync(UserKey, JsonSerializer.Serialize(user, SerializerOptions));

        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        Token = null;
        User = null;

        await ClearStorageAsync();

        Changed?.Invoke();
    }

    private async Task ClearStorageAsync()
    {
        await _storage.RemoveAsync(TokenKey);
        await _storage.RemoveAsync(UserKey);
    }
}
