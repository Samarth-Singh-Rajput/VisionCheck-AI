using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VisionCheckAI.Client.Models;

namespace VisionCheckAI.Client.Services;

public interface IInspectionApi
{
    Task<InspectionDto> UploadAsync(
        string productId,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<InspectionDto> ReviewAsync(
        string inspectionId,
        ReviewRequest review,
        CancellationToken cancellationToken = default);

    Task<PagedResult<InspectionDto>> SearchAsync(
        InspectionQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class InspectionApi : ApiServiceBase, IInspectionApi
{
    public InspectionApi(IHttpClientFactory httpClientFactory, ApiSettings settings)
        : base(httpClientFactory, settings)
    {
    }

    public async Task<InspectionDto> UploadAsync(
        string productId,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(productId), "productId");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/inspections/upload")
        {
            Content = form
        };

        return await SendAsync<InspectionDto>(request, cancellationToken);
    }

    public async Task<InspectionDto> ReviewAsync(
        string inspectionId,
        ReviewRequest review,
        CancellationToken cancellationToken = default)
    {
        var path = $"api/inspections/{Uri.EscapeDataString(inspectionId)}/review";

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(review, options: JsonOptions)
        };

        return await SendAsync<InspectionDto>(request, cancellationToken);
    }

    public async Task<PagedResult<InspectionDto>> SearchAsync(
        InspectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string?>>
        {
            new("productId", query.ProductId),
            new("fromUtc", FormatDate(query.FromUtc)),
            new("toUtc", FormatDate(query.ToUtc)),
            new("defectCategory", query.DefectCategory),
            new("severity", query.Severity),
            new("result", query.Result),
            new("page", query.Page.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture))
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/inspections" + BuildQuery(parameters));

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

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParsePage(body, query);
        }
    }

    /// <summary>
    /// Accepts either a paged envelope ({ items, totalCount, ... }) or a bare array,
    /// so the client works whichever shape the API settles on.
    /// </summary>
    private static PagedResult<InspectionDto> ParsePage(string body, InspectionQuery query)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new PagedResult<InspectionDto> { Page = query.Page, PageSize = query.PageSize };
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var items = JsonSerializer.Deserialize<List<InspectionDto>>(body, JsonOptions) ?? new List<InspectionDto>();

                return new PagedResult<InspectionDto>
                {
                    Items = items,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = items.Count
                };
            }

            var page = JsonSerializer.Deserialize<PagedResult<InspectionDto>>(body, JsonOptions)
                       ?? new PagedResult<InspectionDto>();

            if (page.Page <= 0) page.Page = query.Page;
            if (page.PageSize <= 0) page.PageSize = query.PageSize;

            return page;
        }
        catch (JsonException ex)
        {
            throw new ApiException("The API returned an inspection list this client could not read.", inner: ex);
        }
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}
