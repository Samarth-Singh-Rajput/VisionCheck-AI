using Microsoft.JSInterop;

namespace VisionCheckAI.Client.Services;

/// <summary>Thin localStorage wrapper over the window.visionCheck helpers in js/app.js.</summary>
public sealed class BrowserStorage
{
    private readonly IJSRuntime _js;

    public BrowserStorage(IJSRuntime js) => _js = js;

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("visionCheck.storage.get", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("visionCheck.storage.set", key, value);
        }
        catch (JSException)
        {
            // Storage can be unavailable (private mode / blocked cookies); the app
            // still works for the current session, it just will not be remembered.
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("visionCheck.storage.remove", key);
        }
        catch (JSException)
        {
        }
    }
}
