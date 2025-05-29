using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Logistiq.Application.Common.Interfaces;

namespace Logistiq.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub"); // Use "sub" claim from Kinde

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? CompanyId
    {
        get
        {
            var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("company_id");
            return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : null;
        }
    }

    public async Task<Guid?> GetCurrentCompanyIdAsync()
    {
        return CompanyId; // Already available from claims
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        return UserId; // Already available from claims
    }
}