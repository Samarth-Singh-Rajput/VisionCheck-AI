using System.Net;

namespace VisionCheckAI.Client.Services;

/// <summary>A failed API call, carrying a message safe to show in the UI.</summary>
public sealed class ApiException : Exception
{
    public ApiException(string message, HttpStatusCode? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}
