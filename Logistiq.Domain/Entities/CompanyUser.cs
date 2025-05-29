using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class CompanyUser : BaseEntity
    {
        public Guid ApplicationUserId { get; set; }
        public Guid CompanyId { get; set; }
        public CompanyUserRole Role { get; set; } = CompanyUserRole.User;
        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;
        public virtual Company Company { get; set; } = null!;
    }
}
