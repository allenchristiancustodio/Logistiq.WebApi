// Logistiq.Application/Subscriptions/DTOs/SubscriptionDTOs.cs - Updated with PriceId
using Logistiq.Domain.Enums;

namespace Logistiq.Application.Subscriptions.DTOs;

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public string ClerkOrganizationId { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsTrialActive { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired { get; set; }

    // Plan Limits
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int MaxOrders { get; set; }
    public int MaxWarehouses { get; set; }
    public bool HasAdvancedReporting { get; set; }
    public bool HasReporting { get; set; }
    public bool HasInvoicing { get; set; }

    // Usage Tracking
    public int CurrentUsers { get; set; }
    public int CurrentProducts { get; set; }
    public int CurrentOrders { get; set; }
    public int CurrentWarehouses { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTrialSubscriptionRequest
{
    public string PlanName { get; set; } = "Trial";
    public int TrialDays { get; set; } = 14;
}

public class CreatePaidSubscriptionRequest
{
    public string PlanName { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public DateTime? TrialEndDate { get; set; }
}

// Updated to include PriceId
public class UpdateSubscriptionRequest
{
    public string? PlanName { get; set; }
    public string? PriceId { get; set; } // Added for Stripe price changes
    public decimal? MonthlyPrice { get; set; }
    public SubscriptionStatus? Status { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxUsers { get; set; }
    public int? MaxProducts { get; set; }
    public int? MaxOrders { get; set; }
    public int? MaxWarehouses { get; set; }
    public bool? HasAdvancedReporting { get; set; }
    public bool? HasReporting { get; set; }
    public bool? HasInvoicing { get; set; }
    public bool? ProrationBehavior { get; set; } = true; // For Stripe proration
    public Dictionary<string, string>? Metadata { get; set; } // For Stripe metadata
}

public class CancelSubscriptionRequest
{
    public string? CancellationReason { get; set; }
    public bool CancelImmediately { get; set; } = false;
}

public class SubscriptionPlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public bool IsPopular { get; set; }
    public List<string> Features { get; set; } = new();
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int MaxOrders { get; set; }
    public int MaxWarehouses { get; set; }
    public bool HasAdvancedReporting { get; set; }
    public bool HasReporting { get; set; }
    public bool HasInvoicing { get; set; }
    public string StripePriceIdMonthly { get; set; } = string.Empty;
    public string StripePriceIdAnnual { get; set; } = string.Empty;
}

public class SubscriptionLimitsResponse
{
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int MaxOrders { get; set; }
    public int MaxWarehouses { get; set; }
    public bool HasAdvancedReporting { get; set; }
    public bool HasReporting { get; set; }
    public bool HasInvoicing { get; set; }
}

public class SubscriptionUsageResponse
{
    public int CurrentUsers { get; set; }
    public int CurrentProducts { get; set; }
    public int CurrentOrders { get; set; }
    public int CurrentWarehouses { get; set; }
    public SubscriptionLimitsResponse Limits { get; set; } = new();

    public Dictionary<string, UsageMetric> UsageMetrics { get; set; } = new();
}

public class UsageMetric
{
    public int Current { get; set; }
    public int Limit { get; set; }
    public double PercentageUsed => Limit > 0 ? (double)Current / Limit * 100 : 0;
    public bool IsAtLimit => Current >= Limit;
    public bool IsNearLimit => PercentageUsed >= 80;
}

// New DTO for Stripe-specific subscription updates
public class UpdateStripeSubscriptionRequest
{
    public string? PriceId { get; set; }
    public bool? ProrationBehavior { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
}

// New DTO for plan changes that affect both local DB and Stripe
public class ChangePlanRequest
{
    public string NewPlanId { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public bool IsAnnual { get; set; } = false;
    public bool ProrateBilling { get; set; } = true;
}

public enum SubscriptionLimitType
{
    Users,
    Products,
    Orders,
    Warehouses
}