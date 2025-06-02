// Logistiq.Infrastructure/Services/StripeService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;
using Logistiq.Application.Payments;
using Logistiq.Application.Payments.DTOs;
using Logistiq.Application.Subscriptions;
using Logistiq.Domain.Entities;

namespace Logistiq.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly string _secretKey;
    private readonly string _webhookSecret;

    public StripeService(
        IConfiguration configuration,
        ILogger<StripeService> logger,
        ISubscriptionService subscriptionService)
    {
        _configuration = configuration;
        _logger = logger;
        _subscriptionService = subscriptionService;
        _secretKey = _configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe:SecretKey");
        _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? throw new ArgumentNullException("Stripe:WebhookSecret");

        StripeConfiguration.ApiKey = _secretKey;
    }

    public async Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request)
    {
        try
        {
            // Create or get customer
            var customer = await GetOrCreateCustomerAsync(request.CustomerEmail, request.CustomerName, request.OrganizationId, request.OrganizationName);

            var options = new SessionCreateOptions
            {
                Customer = customer.Id,
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = request.PriceId,
                        Quantity = 1,
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    {"organization_id", request.OrganizationId},
                    {"organization_name", request.OrganizationName},
                    {"is_annual", request.IsAnnual.ToString()},
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        {"organization_id", request.OrganizationId},
                        {"organization_name", request.OrganizationName},
                    },
                    TrialPeriodDays = request.TrialDays,
                },
                AllowPromotionCodes = true,
                BillingAddressCollection = "auto",
                CustomerUpdate = new SessionCustomerUpdateOptions
                {
                    Address = "auto",
                    Name = "auto",
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for organization {OrganizationId}",
                session.Id, request.OrganizationId);

            return new CreateCheckoutSessionResponse
            {
                SessionId = session.Id,
                SessionUrl = session.Url,
                CustomerId = customer.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe checkout session for organization {OrganizationId}", request.OrganizationId);
            throw;
        }
    }

    public async Task<CreatePortalSessionResponse> CreatePortalSessionAsync(CreatePortalSessionRequest request)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = request.CustomerId,
                ReturnUrl = request.ReturnUrl,
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe portal session for customer {CustomerId}", request.CustomerId);

            return new CreatePortalSessionResponse
            {
                SessionUrl = session.Url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe portal session for customer {CustomerId}", request.CustomerId);
            throw;
        }
    }

    public async Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = request.Email,
                Name = request.Name,
                Metadata = new Dictionary<string, string>
                {
                    {"organization_id", request.OrganizationId},
                    {"organization_name", request.OrganizationName},
                }
            };

            if (request.Metadata != null)
            {
                foreach (var item in request.Metadata)
                {
                    options.Metadata[item.Key] = item.Value;
                }
            }

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe customer {CustomerId} for organization {OrganizationId}",
                customer.Id, request.OrganizationId);

            return new CreateCustomerResponse
            {
                CustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for organization {OrganizationId}", request.OrganizationId);
            throw;
        }
    }

    public async Task<WebhookEventResponse> HandleWebhookAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);

            _logger.LogInformation("Processing Stripe webhook event {EventType} with ID {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            var response = new WebhookEventResponse
            {
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id,
                Processed = false
            };

            switch (stripeEvent.Type)
            {
                case Events.CheckoutSessionCompleted:
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    response.Processed = true;
                    response.Message = "Checkout session completed";
                    break;

                case Events.CustomerSubscriptionCreated:
                    await HandleSubscriptionCreated(stripeEvent);
                    response.Processed = true;
                    response.Message = "Subscription created";
                    break;

                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdated(stripeEvent);
                    response.Processed = true;
                    response.Message = "Subscription updated";
                    break;

                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent);
                    response.Processed = true;
                    response.Message = "Subscription deleted";
                    break;

                case Events.InvoicePaymentSucceeded:
                    await HandleInvoicePaymentSucceeded(stripeEvent);
                    response.Processed = true;
                    response.Message = "Payment succeeded";
                    break;

                case Events.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailed(stripeEvent);
                    response.Processed = true;
                    response.Message = "Payment failed";
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe webhook event type: {EventType}", stripeEvent.Type);
                    response.Message = "Event type not handled";
                    break;
            }

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            throw new UnauthorizedAccessException("Invalid webhook signature");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Stripe webhook");
            throw;
        }
    }

    public async Task<List<StripePriceResponse>> GetProductPricesAsync()
    {
        try
        {
            var priceService = new PriceService();
            var productService = new ProductService();

            var prices = await priceService.ListAsync(new PriceListOptions
            {
                Active = true,
                Type = "recurring",
                Limit = 100,
                Expand = new List<string> { "data.product" }
            });

            var result = new List<StripePriceResponse>();

            foreach (var price in prices)
            {
                if (price.Product is Stripe.Product product)
                {
                    result.Add(new StripePriceResponse
                    {
                        Id = price.Id,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductDescription = product.Description ?? "",
                        UnitAmount = price.UnitAmount ?? 0,
                        Currency = price.Currency,
                        Interval = price.Recurring?.Interval ?? "",
                        IntervalCount = price.Recurring?.IntervalCount ?? 1,
                        IsActive = price.Active,
                        Metadata = price.Metadata
                    });
                }
            }

            return result.OrderBy(p => p.UnitAmount).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Stripe product prices");
            throw;
        }
    }

    public async Task<StripeSubscriptionResponse?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var service = new SubscriptionService();
            var subscription = await service.GetAsync(subscriptionId, new SubscriptionGetOptions
            {
                Expand = new List<string> { "latest_invoice", "customer" }
            });

            return MapToSubscriptionResponse(subscription);
        }
        catch (StripeException ex) when (ex.StripeError.Type == "invalid_request_error")
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<StripeSubscriptionResponse> CancelSubscriptionAsync(string subscriptionId, bool immediately = false)
    {
        try
        {
            var service = new SubscriptionService();

            if (immediately)
            {
                var subscription = await service.CancelAsync(subscriptionId);
                return MapToSubscriptionResponse(subscription);
            }
            else
            {
                var subscription = await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                });
                return MapToSubscriptionResponse(subscription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<StripeSubscriptionResponse> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionRequest request)
    {
        try
        {
            var service = new SubscriptionService();
            var options = new SubscriptionUpdateOptions();

            if (!string.IsNullOrEmpty(request.PriceId))
            {
                // Get current subscription to update the price
                var currentSub = await service.GetAsync(subscriptionId);
                var currentItem = currentSub.Items.Data.FirstOrDefault();

                if (currentItem != null)
                {
                    options.Items = new List<SubscriptionItemOptions>
                    {
                        new()
                        {
                            Id = currentItem.Id,
                            Price = request.PriceId
                        }
                    };
                }

                if (request.ProrationBehavior == true)
                {
                    options.ProrationBehavior = "create_prorations";
                }
            }

            if (request.Metadata != null)
            {
                options.Metadata = request.Metadata;
            }

            var subscription = await service.UpdateAsync(subscriptionId, options);
            return MapToSubscriptionResponse(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    // Private helper methods
    private async Task<Customer> GetOrCreateCustomerAsync(string email, string name, string organizationId, string organizationName)
    {
        var customerService = new CustomerService();

        // Try to find existing customer by email and organization
        var existingCustomers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = email,
            Limit = 1
        });

        var existingCustomer = existingCustomers.Data.FirstOrDefault(c =>
            c.Metadata.ContainsKey("organization_id") &&
            c.Metadata["organization_id"] == organizationId);

        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        // Create new customer
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Metadata = new Dictionary<string, string>
            {
                {"organization_id", organizationId},
                {"organization_name", organizationName}
            }
        };

        return await customerService.CreateAsync(options);
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.Mode == "subscription" && session.SubscriptionId != null)
        {
            _logger.LogInformation("Checkout session completed for subscription {SubscriptionId}", session.SubscriptionId);
            // The subscription webhook will handle the actual subscription creation
        }
    }

    private async Task HandleSubscriptionCreated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription?.Metadata?.ContainsKey("organization_id") == true)
        {
            var organizationId = subscription.Metadata["organization_id"];
            var priceId = subscription.Items.Data.FirstOrDefault()?.Price.Id;

            if (!string.IsNullOrEmpty(priceId))
            {
                await _subscriptionService.CreatePaidSubscriptionAsync(new Application.Subscriptions.DTOs.CreatePaidSubscriptionRequest
                {
                    PlanName = GetPlanNameFromPriceId(priceId),
                    StripeCustomerId = subscription.CustomerId,
                    StripeSubscriptionId = subscription.Id,
                    StripePriceId = priceId,
                    MonthlyPrice = (subscription.Items.Data.FirstOrDefault()?.Price.UnitAmount ?? 0) / 100m,
                    TrialEndDate = subscription.TrialEnd?.DateTime
                });

                _logger.LogInformation("Created subscription for organization {OrganizationId} with Stripe subscription {SubscriptionId}",
                    organizationId, subscription.Id);
            }
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription?.Metadata?.ContainsKey("organization_id") == true)
        {
            // Handle subscription changes like plan upgrades, cancellations, etc.
            _logger.LogInformation("Subscription updated: {SubscriptionId}, Status: {Status}",
                subscription.Id, subscription.Status);
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription?.Metadata?.ContainsKey("organization_id") == true)
        {
            _logger.LogInformation("Subscription deleted: {SubscriptionId}", subscription.Id);
        }
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.SubscriptionId != null)
        {
            _logger.LogInformation("Payment succeeded for subscription {SubscriptionId}", invoice.SubscriptionId);
        }
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.SubscriptionId != null)
        {
            _logger.LogWarning("Payment failed for subscription {SubscriptionId}", invoice.SubscriptionId);
        }
    }

    private static StripeSubscriptionResponse MapToSubscriptionResponse(Subscription subscription)
    {
        return new StripeSubscriptionResponse
        {
            Id = subscription.Id,
            CustomerId = subscription.CustomerId,
            Status = subscription.Status,
            PriceId = subscription.Items.Data.FirstOrDefault()?.Price.Id ?? "",
            Amount = (subscription.Items.Data.FirstOrDefault()?.Price.UnitAmount ?? 0) / 100m,
            Currency = subscription.Items.Data.FirstOrDefault()?.Price.Currency ?? "usd",
            Interval = subscription.Items.Data.FirstOrDefault()?.Price.Recurring?.Interval ?? "",
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            TrialStart = subscription.TrialStart,
            TrialEnd = subscription.TrialEnd,
            CancelAt = subscription.CancelAt,
            CanceledAt = subscription.CanceledAt,
            CreatedAt = subscription.Created,
            Metadata = subscription.Metadata
        };
    }

    private static string GetPlanNameFromPriceId(string priceId)
    {
        // Map your Stripe price IDs to plan names
        return priceId switch
        {
            var id when id.Contains("starter") => "Starter",
            var id when id.Contains("professional") => "Professional",
            var id when id.Contains("enterprise") => "Enterprise",
            _ => "Professional" // Default
        };
    }
}