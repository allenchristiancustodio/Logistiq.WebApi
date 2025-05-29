
namespace Logistiq.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Email { get; }
        Guid? CompanyId { get; }
        bool IsAuthenticated { get; }
        Task<Guid?> GetCurrentCompanyIdAsync();
        Task<string?> GetCurrentUserIdAsync();
    }
}
