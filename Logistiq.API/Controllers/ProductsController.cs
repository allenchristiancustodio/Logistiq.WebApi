using Microsoft.AspNetCore.Mvc;
using Logistiq.Application.Products.Commands.CreateProduct;
using Logistiq.Application.Products.Queries.GetProducts;

namespace Logistiq.API.Controllers;

public class ProductsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult> GetProducts([FromQuery] GetProductsQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetProduct(Guid id)
    {
        // TODO: Implement GetProductQuery
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
    /*
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Product ID mismatch");
        }

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        // TODO: Implement DeleteProductCommand
        return Ok();
    }
    */
}