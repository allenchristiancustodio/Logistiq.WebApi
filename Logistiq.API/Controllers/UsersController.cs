using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Users;
using Logistiq.Application.Users.DTOs;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<UserResponse>> SyncUser([FromBody] SyncUserRequest request)
    {
        try
        {
            var user = await _userService.SyncUserAsync(request);
            return Ok(user);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return NotFound("User not found");

        return Ok(user);
    }
}