using Microsoft.JSInterop;

namespace VisionCheckAI.Client.Services;

/// <summary>Dark by default, with the choice persisted in localStorage.</summary>
public sealed class ThemeService
{
    private const string StorageKey = "visioncheck.theme";
    public const string Dark = "dark";
    public const string Light = "light";

    private readonly BrowserStorage _storage;
    private readonly IJSRuntime _js;

    public ThemeService(BrowserStorage storage, IJSRuntime js)
    {
        _storage = storage;
        _js = js;
    }

    public string Current { get; private set; } = Dark;

    public bool IsDark => Current == Dark;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var stored = await _storage.GetAsync(StorageKey);
        Current = stored == Light ? Light : Dark;
        await ApplyAsync();
    }

    public async Task ToggleAsync()
    {
        Current = IsDark ? Light : Dark;
        await _storage.SetAsync(StorageKey, Current);
        await ApplyAsync();
        Changed?.Invoke();
    }

    private async Task ApplyAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("visionCheck.theme.apply", Current);
        }
        catch (JSException)
        {
        }
    }
}
