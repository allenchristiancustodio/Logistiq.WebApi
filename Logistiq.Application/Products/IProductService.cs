using Logistiq.Application.Products.DTOs;

namespace Logistiq.Application.Products;

public interface IProductService
{
    Task<PagedProductResponse> GetProductsAsync(ProductSearchRequest request);
    Task<ProductResponse?> GetProductByIdAsync(Guid id);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task DeleteProductAsync(Guid id);
    Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeProductId = null);
}