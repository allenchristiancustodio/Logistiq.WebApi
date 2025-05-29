using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public Guid CompanyId { get; set; }
        // Stripe Integration
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripePriceId { get; set; }

        // Subscription Details
        public string PlanName { get; set; } = string.Empty; // "Basic", "Pro"
        public decimal MonthlyPrice { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }

        // Plan Limits
        public int MaxUsers { get; set; } = 1;
        public int MaxProducts { get; set; } = 100;
        public bool HasInvoicing { get; set; } = false;
        public bool HasReporting { get; set; } = false;

        // Navigation Properties
        public virtual Company Company { get; set; } = null!;
    }
}
