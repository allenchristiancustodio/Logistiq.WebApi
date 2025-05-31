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

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public string? OrganizationId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("org_id")
                                  ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("organization_id");

    public async Task<string?> GetCurrentOrganizationIdAsync()
    {
        return OrganizationId;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        return UserId;
    }
}