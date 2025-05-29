using Microsoft.AspNetCore.Mvc;
using Logistiq.Application.Users.Commands.CreateOrUpdateUser;
using Logistiq.Application.Users.Queries.GetCurrentUser;

namespace Logistiq.API.Controllers;

public class UsersController : BaseApiController
{
    [HttpPost("create-or-update")]
    public async Task<ActionResult> CreateOrUpdateUser([FromBody] CreateOrUpdateUserCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var query = new GetCurrentUserQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}