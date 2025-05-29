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
        var kindeUserId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;
        var companyId = User.FindFirst("company_id")?.Value;

        return Ok(new
        {
            Message = "Authentication successful!",
            KindeUserId = kindeUserId,
            Email = email,
            CompanyId = companyId,
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