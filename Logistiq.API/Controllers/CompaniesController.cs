using Microsoft.AspNetCore.Mvc;
using Logistiq.Application.Companies.Commands.CreateCompany;

namespace Logistiq.API.Controllers;

public class CompaniesController : BaseApiController
{
    [HttpGet("current")]
    public async Task<ActionResult> GetCurrentCompany()
    {
        // TODO: Implement GetCurrentCompanyQuery
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> CreateCompany([FromBody] CreateCompanyCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /*
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        // TODO: Implement UpdateCompanyCommand
        return Ok();
    }
    */
}