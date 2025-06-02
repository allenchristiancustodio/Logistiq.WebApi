using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Categories;
using Logistiq.Application.Categories.DTOs;
using Logistiq.Domain.Entities;
using Logistiq.Persistence.Data;

namespace Logistiq.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly LogistiqDbContext _context;

    public CategoryService(LogistiqDbContext context)
    {
        _context = context;
    }

    public async Task<PagedCategoryResponse> GetCategoriesAsync(CategorySearchRequest request)
    {
        var query = _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                   (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
        }

        if (request.ParentCategoryId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryId == request.ParentCategoryId);
        }

        var totalCount = await query.CountAsync();

        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedCategoryResponse
        {
            Categories = categories.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPrevPage = request.Page > 1
        };
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        return category == null ? null : MapToResponse(category);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
    {
        // Check if parent category exists and belongs to same organization
        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _context.Categories
                .AnyAsync(c => c.Id == request.ParentCategoryId.Value);

            if (!parentExists)
                throw new ArgumentException("Parent category not found");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(category.Id) ?? MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        // Prevent circular references
        if (request.ParentCategoryId.HasValue && request.ParentCategoryId.Value == id)
            throw new ArgumentException("Category cannot be its own parent");

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;

        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        // Check if category has products
        if (category.Products.Any())
            throw new InvalidOperationException("Cannot delete category that contains products");

        // Check if category has subcategories
        if (category.SubCategories.Any())
            throw new InvalidOperationException("Cannot delete category that has subcategories");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }

    public async Task<List<CategoryResponse>> GetCategoryHierarchyAsync()
    {
        var categories = await _context.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapToResponse).ToList();
    }

    private CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            CreatedAt = category.CreatedAt,
            CreatedBy = category.CreatedBy,
            SubCategories = category.SubCategories?.Select(MapToResponse).ToList() ?? new(),
            ProductCount = category.Products?.Count ?? 0
        };
    }
}