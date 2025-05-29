using MediatR;
using System.Linq.Expressions;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Entities;

namespace Logistiq.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly ITenantRepository<Product> _productRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProductsQueryHandler(
        ITenantRepository<Product> productRepository,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var companyId = await _currentUserService.GetCurrentCompanyIdAsync();
        if (companyId == null)
        {
            return Result<PagedResult<ProductDto>>.Failure("Company not found");
        }

        Expression<Func<Product, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = p => p.Name.ToLower().Contains(searchTerm) ||
                           p.Sku.ToLower().Contains(searchTerm) ||
                           (p.Description != null && p.Description.ToLower().Contains(searchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            var categoryPredicate = (Expression<Func<Product, bool>>)(p => p.CategoryId == request.CategoryId);
            predicate = predicate == null ? categoryPredicate : CombinePredicates(predicate, categoryPredicate);
        }

        var pagedProducts = await _productRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            companyId.Value,
            predicate,
            cancellationToken);

        var productDtos = pagedProducts.Items.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Sku = p.Sku,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            Price = p.Price,
            CostPrice = p.CostPrice,
            StockQuantity = p.StockQuantity,
            MinStockLevel = p.MinStockLevel,
            MaxStockLevel = p.MaxStockLevel,
            Status = p.Status.ToString(),
            CreatedAt = p.CreatedAt
        });

        var result = new PagedResult<ProductDto>
        {
            Items = productDtos,
            TotalCount = pagedProducts.TotalCount,
            Page = pagedProducts.Page,
            PageSize = pagedProducts.PageSize
        };

        return Result<PagedResult<ProductDto>>.Success(result);
    }

    private static Expression<Func<Product, bool>> CombinePredicates(
        Expression<Func<Product, bool>> first,
        Expression<Func<Product, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(Product));
        var body = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter));
        return Expression.Lambda<Func<Product, bool>>(body, parameter);
    }
}