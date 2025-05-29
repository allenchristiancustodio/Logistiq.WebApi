using MediatR;
using Logistiq.Application.Common.Models;

namespace Logistiq.Application.Users.Commands.CreateOrUpdateUser;

public class CreateOrUpdateUserCommand : IRequest<Result<UserResult>>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UserResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
    public bool HasActiveCompany { get; set; }
    public Guid? CurrentCompanyId { get; set; }
    public string? CurrentCompanyName { get; set; }
}