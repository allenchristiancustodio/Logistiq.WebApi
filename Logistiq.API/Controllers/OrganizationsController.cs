using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Organizations;
using Logistiq.Application.Organizations.DTOs;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<OrganizationResponse>> GetCurrentOrganization()
    {
        var organization = await _organizationService.GetCurrentOrganizationAsync();
        if (organization == null)
            return NotFound("Organization not found");

        return Ok(organization);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<OrganizationResponse>> SyncOrganization([FromBody] SyncOrganizationRequest request)
    {
        try
        {
            var organization = await _organizationService.SyncOrganizationAsync(request);
            return Ok(organization);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("complete-setup")]
    public async Task<ActionResult<OrganizationResponse>> CompleteOrganizationSetup([FromBody] CompleteOrganizationSetupRequest request)
    {
        try
        {
            var organization = await _organizationService.CompleteOrganizationSetupAsync(request);
            return Ok(organization);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Organization not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("current")]
    public async Task<ActionResult<OrganizationResponse>> UpdateCurrentOrganization([FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var organization = await _organizationService.UpdateOrganizationAsync(request);
            return Ok(organization);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Organization not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}