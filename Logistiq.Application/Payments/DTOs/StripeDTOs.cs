namespace Logistiq.Application.Payments.DTOs;

public class CreateCheckoutSessionRequest
{
    public string PriceId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public bool IsAnnual { get; set; } = false;
    public int? TrialDays { get; set; }
}

public class CreateCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionUrl { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
}

public class CreatePortalSessionRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}

public class CreatePortalSessionResponse
{
    public string SessionUrl { get; set; } = string.Empty;
}

public class CreateCustomerRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CreateCustomerResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class WebhookEventResponse
{
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public string? Message { get; set; }
}

public class StripePriceResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public decimal UnitAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty; // month, year
    public int IntervalCount { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class StripeSubscriptionResponse
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PriceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime? CancelAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

// RENAMED: This was causing the conflict
public class StripeUpdateSubscriptionRequest
{
    public string? PriceId { get; set; }
    public bool? ProrationBehavior { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class StripeWebhookEventData
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Created { get; set; }
}