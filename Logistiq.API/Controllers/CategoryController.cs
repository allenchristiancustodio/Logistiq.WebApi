using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Categories;
using Logistiq.Application.Categories.DTOs;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedCategoryResponse>> GetCategories([FromQuery] CategorySearchRequest request)
    {
        var result = await _categoryService.GetCategoriesAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(Guid id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
            return NotFound();

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);
            return Ok(category);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(Guid id)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}