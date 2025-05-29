using MediatR;
using Logistiq.Application.Common.Models;

namespace Logistiq.Application.Users.Queries.GetCurrentUser;

public class GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>
{
}

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool HasActiveCompany { get; set; }
    public Guid? CurrentCompanyId { get; set; }
    public string? CurrentCompanyName { get; set; }
    public List<UserCompanyInfo> Companies { get; set; } = new();
}

public class UserCompanyInfo
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}