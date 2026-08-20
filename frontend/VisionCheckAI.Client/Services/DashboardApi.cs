using VisionCheckAI.Client.Models;

namespace VisionCheckAI.Client.Services;

public interface IDashboardApi
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardApi : ApiServiceBase, IDashboardApi
{
    public DashboardApi(IHttpClientFactory httpClientFactory, ApiSettings settings)
        : base(httpClientFactory, settings)
    {
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard/summary");
        return await SendAsync<DashboardSummaryDto>(request, cancellationToken);
    }
}
