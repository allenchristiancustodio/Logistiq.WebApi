using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet("ping")]
    [AllowAnonymous]
    public ActionResult Ping()
    {
        return Ok(new
        {
            Message = "Pong! API is working",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    [HttpGet("auth-test")]
    [Authorize]
    public ActionResult AuthTest()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var clerkUserId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;

        // Try to get organization ID from different possible claim locations
        var organizationId = User.FindFirst("org_id")?.Value
                          ?? User.FindFirst("organization_id")?.Value;

        // If not found in direct claims, try parsing from 'o' claim
        if (string.IsNullOrEmpty(organizationId))
        {
            var orgClaim = User.FindFirst("o")?.Value;
            if (!string.IsNullOrEmpty(orgClaim))
            {
                try
                {
                    var orgData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(orgClaim);
                    if (orgData.TryGetProperty("id", out var idElement))
                    {
                        organizationId = idElement.GetString();
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // If parsing fails, organizationId remains null
                }
            }
        }

        // Try to get organization name
        var organizationName = User.FindFirst("org_name")?.Value;
        if (string.IsNullOrEmpty(organizationName))
        {
            var orgClaim = User.FindFirst("o")?.Value;
            if (!string.IsNullOrEmpty(orgClaim))
            {
                try
                {
                    var orgData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(orgClaim);
                    if (orgData.TryGetProperty("slg", out var slugElement)) // 'slg' seems to be the slug
                    {
                        organizationName = slugElement.GetString();
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // If parsing fails, organizationName remains null
                }
            }
        }

        return Ok(new
        {
            Message = "Authentication successful!",
            ClerkUserId = clerkUserId,
            Email = email,
            OrganizationId = organizationId,
            OrganizationName = organizationName,
            AllClaims = claims,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("echo")]
    [AllowAnonymous]
    public ActionResult Echo([FromBody] object data)
    {
        return Ok(new
        {
            Message = "Echo successful",
            ReceivedData = data,
            Timestamp = DateTime.UtcNow
        });
    }
}