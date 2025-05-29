using Microsoft.AspNetCore.Mvc;

namespace Logistiq.API.Controllers;

public class CompaniesController : BaseApiController
{
    [HttpGet("current")]
    public async Task<ActionResult> GetCurrentCompany()
    {
        // TODO: Implement GetCurrentCompanyQuery
        return Ok();
    }
    /*
    [HttpPost]
    public async Task<ActionResult> CreateCompany([FromBody] CreateCompanyCommand command)
    {
        // TODO: Implement CreateCompanyCommand
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        // TODO: Implement UpdateCompanyCommand
        return Ok();

    }
    */
}