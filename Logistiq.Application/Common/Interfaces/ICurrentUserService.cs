namespace Logistiq.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Email { get; }
        string? OrganizationId { get; }
        bool IsAuthenticated { get; }
        Task<string?> GetCurrentOrganizationIdAsync();
        Task<string?> GetCurrentUserIdAsync();
    }
}