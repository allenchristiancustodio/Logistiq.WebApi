using Logistiq.Application.Categories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Application.Categories
{
    public interface ICategoryService
    {
        Task<PagedCategoryResponse> GetCategoriesAsync(CategorySearchRequest request);
        Task<CategoryResponse?> GetCategoryByIdAsync(Guid id);
        Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request);

        Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request);
        Task DeleteCategoryAsync(Guid id);
    }
}
