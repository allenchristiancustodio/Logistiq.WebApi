using MediatR;
using Logistiq.Application.Common.Models;

namespace Logistiq.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommand : IRequest<Result<CreateCompanyResult>>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}

public class CreateCompanyResult
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string NewToken { get; set; } = string.Empty;
}