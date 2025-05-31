using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Products;
using Logistiq.Application.Products.DTOs;
using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;
using Logistiq.Persistence.Data;

namespace Logistiq.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly LogistiqDbContext _context;

    public ProductService(LogistiqDbContext context)
    {
        _context = context;
    }

    public async Task<PagedProductResponse> GetProductsAsync(ProductSearchRequest request)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        // Apply search filters
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                p.Sku.ToLower().Contains(searchTerm) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProductStatus>(request.Status, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var products = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedProductResponse
        {
            Products = products.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPrevPage = request.Page > 1
        };
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid id)
    {
        // Organization filtering happens automatically!
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        // Check if SKU is unique within organization
        if (await IsSkuTakenAsync(request.Sku))
        {
            throw new InvalidOperationException($"SKU '{request.Sku}' already exists in your organization");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            CategoryId = request.CategoryId,
            Price = request.Price,
            CostPrice = request.CostPrice,
            StockQuantity = request.StockQuantity,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            Unit = request.Unit,
            Status = ProductStatus.Active
            // ClerkOrganizationId will be set automatically by DbContext!
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Return the created product with navigation properties
        return await GetProductByIdAsync(product.Id) ?? MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        // Update properties
        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CostPrice = request.CostPrice;
        product.StockQuantity = request.StockQuantity;
        product.MinStockLevel = request.MinStockLevel;
        product.MaxStockLevel = request.MaxStockLevel;
        product.Unit = request.Unit;

        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        // Soft delete
        product.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeProductId = null)
    {
        var query = _context.Products.Where(p => p.Sku == sku);

        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return !await query.AnyAsync();
    }

    private async Task<bool> IsSkuTakenAsync(string sku)
    {
        return await _context.Products.AnyAsync(p => p.Sku == sku);
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            Price = product.Price,
            CostPrice = product.CostPrice,
            StockQuantity = product.StockQuantity,
            MinStockLevel = product.MinStockLevel,
            MaxStockLevel = product.MaxStockLevel,
            Unit = product.Unit,
            Status = product.Status.ToString(),
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy
        };
    }
}