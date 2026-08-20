namespace VisionCheckAI.Client.Services;

public static class ApiRequestOptions
{
    /// <summary>Marks a request that must not trigger the global 401 sign-out (e.g. login itself).</summary>
    public static readonly HttpRequestOptionsKey<bool> AllowAnonymous = new("VisionCheck.AllowAnonymous");
}
