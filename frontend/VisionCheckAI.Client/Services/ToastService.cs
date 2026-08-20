namespace VisionCheckAI.Client.Services;

public enum ToastLevel
{
    Success,
    Error,
    Info
}

public sealed class Toast
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastLevel Level { get; init; } = ToastLevel.Info;
    public string Title { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

/// <summary>Small corner notifications, auto-dismissed.</summary>
public sealed class ToastService
{
    private const int DefaultDurationMs = 4500;

    private readonly List<Toast> _toasts = new();

    public IReadOnlyList<Toast> Toasts => _toasts;

    public event Action? Changed;

    public void ShowSuccess(string title, string? detail = null) => Show(ToastLevel.Success, title, detail);

    public void ShowError(string title, string? detail = null) => Show(ToastLevel.Error, title, detail);

    public void ShowInfo(string title, string? detail = null) => Show(ToastLevel.Info, title, detail);

    public void Show(ToastLevel level, string title, string? detail = null, int durationMs = DefaultDurationMs)
    {
        var toast = new Toast { Level = level, Title = title, Detail = detail };
        _toasts.Add(toast);
        Changed?.Invoke();

        _ = DismissAfterAsync(toast, durationMs);
    }

    public void Dismiss(Guid id)
    {
        var removed = _toasts.RemoveAll(t => t.Id == id);
        if (removed > 0)
        {
            Changed?.Invoke();
        }
    }

    private async Task DismissAfterAsync(Toast toast, int durationMs)
    {
        await Task.Delay(durationMs);
        Dismiss(toast.Id);
    }
}
