using MediatR;
using Logistiq.Application.Common.Models;

namespace Logistiq.Application.Products.Queries.GetProducts;

public class GetProductsQuery : IRequest<Result<PagedResult<ProductDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
}