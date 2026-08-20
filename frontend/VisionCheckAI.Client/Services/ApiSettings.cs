namespace VisionCheckAI.Client.Services;

/// <summary>API host configuration, sourced from wwwroot/appsettings*.json (never hardcoded).</summary>
public sealed class ApiSettings
{
    public const string HttpClientName = "VisionCheckApi";

    private string _baseUrl = "/";

    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = string.IsNullOrWhiteSpace(value)
            ? "/"
            : (value.EndsWith('/') ? value : value + "/");
    }

    /// <summary>Turns a possibly-relative media path from the API into an absolute URL.</summary>
    public string ResolveMediaUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return path;

        // Only http(s) counts as absolute: a leading-slash path such as "/media/a.jpg"
        // otherwise parses as an absolute file:// URI and would never load.
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return path;
        }

        return BaseUrl + path.TrimStart('/');
    }
}
