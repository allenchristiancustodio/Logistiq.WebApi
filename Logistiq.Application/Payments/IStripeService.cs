// Logistiq.Application/Payments/IStripeService.cs
using Logistiq.Application.Payments.DTOs;
using Logistiq.Application.Subscriptions.DTOs;

namespace Logistiq.Application.Payments;

public interface IStripeService
{
    Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request);
    Task<CreatePortalSessionResponse> CreatePortalSessionAsync(CreatePortalSessionRequest request);
    Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
    Task<WebhookEventResponse> HandleWebhookAsync(string payload, string signature);
    Task<List<StripePriceResponse>> GetProductPricesAsync();
    Task<StripeSubscriptionResponse?> GetSubscriptionAsync(string subscriptionId);
    Task<StripeSubscriptionResponse> CancelSubscriptionAsync(string subscriptionId, bool immediately = false);
    Task<StripeSubscriptionResponse> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionRequest request);
}