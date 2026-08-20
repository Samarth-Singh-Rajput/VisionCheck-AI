using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace VisionCheckAI.Client.Services;

public abstract class ApiServiceBase
{
    protected ApiServiceBase(IHttpClientFactory httpClientFactory, ApiSettings settings)
    {
        HttpClientFactory = httpClientFactory;
        Settings = settings;
    }

    protected IHttpClientFactory HttpClientFactory { get; }

    protected ApiSettings Settings { get; }

    protected HttpClient Client => HttpClientFactory.CreateClient(ApiSettings.HttpClientName);

    protected static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new FlexibleStringConverter() }
    };

    /// <summary>Sends a request and deserialises the body, turning failures into ApiException.</summary>
    protected async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        try
        {
            response = await Client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                "Cannot reach the inspection API. Check that the service is running and that Api:BaseUrl is correct.",
                inner: ex);
        }

        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            if (payload is null)
            {
                throw new ApiException("The API returned an empty response.", response.StatusCode);
            }

            return payload;
        }
    }

    protected async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        try
        {
            response = await Client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                "Cannot reach the inspection API. Check that the service is running and that Api:BaseUrl is correct.",
                inner: ex);
        }

        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken);
        }
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await ReadErrorMessageAsync(response, cancellationToken);
        throw new ApiException(message, response.StatusCode);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var fallback = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Sign in again.",
            HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested record was not found.",
            HttpStatusCode.RequestEntityTooLarge => "That image is larger than the API accepts.",
            HttpStatusCode.BadRequest => "The API rejected the request.",
            _ => $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}."
        };

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                return fallback;
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            foreach (var name in new[] { "detail", "title", "message", "error" })
            {
                if (root.TryGetProperty(name, out var property) &&
                    property.ValueKind == JsonValueKind.String)
                {
                    var value = property.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value!;
                    }
                }
            }

            return fallback;
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>Builds a query string, skipping empty values.</summary>
    protected static string BuildQuery(IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        var pairs = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToArray();

        return pairs.Length == 0 ? string.Empty : "?" + string.Join("&", pairs);
    }
}
