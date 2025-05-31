using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripePriceId { get; set; }

        public string PlanName { get; set; } = string.Empty; // "Basic", "Pro"
        public decimal MonthlyPrice { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }

        // Plan Limits
        // Plan Limits
        public int MaxUsers { get; set; } = 5;
        public int MaxProducts { get; set; } = 100;
        public int MaxOrders { get; set; } = 1000;
        public int MaxWarehouses { get; set; } = 1;
        public bool HasAdvancedReporting { get; set; } = false;
        public bool HasReporting { get; set; } = false;
        public bool HasInvoicing { get; set; } = false;


        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
    }
}
