using VisionCheckAI.Client.Models;

namespace VisionCheckAI.Client.Services;

public interface IProductApi
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
}

public sealed class ProductApi : ApiServiceBase, IProductApi
{
    public ProductApi(IHttpClientFactory httpClientFactory, ApiSettings settings)
        : base(httpClientFactory, settings)
    {
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/products");
        var products = await SendAsync<List<ProductDto>>(request, cancellationToken);
        return products;
    }
}
