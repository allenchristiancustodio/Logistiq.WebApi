// Logistiq.Application/Categories/DTOs/CategoryDTOs.cs
namespace Logistiq.Application.Categories.DTOs;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

public class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<CategoryResponse> SubCategories { get; set; } = new();
    public int ProductCount { get; set; }
}

public class CategorySearchRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

public class PagedCategoryResponse
{
    public List<CategoryResponse> Categories { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPrevPage { get; set; }
}