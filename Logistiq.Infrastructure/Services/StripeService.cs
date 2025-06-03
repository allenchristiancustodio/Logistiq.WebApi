// Logistiq.Infrastructure/Services/StripeService.cs - Fixed without circular dependency
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;
using Logistiq.Application.Payments;
using Logistiq.Application.Payments.DTOs;
using Logistiq.Application.Subscriptions.DTOs;
using Logistiq.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Logistiq.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;
    private readonly LogistiqDbContext _context; // Use DbContext directly instead of ISubscriptionService
    private readonly string _secretKey;
    private readonly string _webhookSecret;

    public StripeService(
        IConfiguration configuration,
        ILogger<StripeService> logger,
        LogistiqDbContext context) // Remove ISubscriptionService dependency
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
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

            var options = new Stripe.Checkout.SessionCreateOptions
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

            var service = new Stripe.Checkout.SessionService();
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
            // FIXED: Allow API version mismatch to prevent the exception
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _webhookSecret,
                tolerance: 300, // 5 minutes tolerance
                throwOnApiVersionMismatch: false // This prevents the version mismatch exception
            );

            _logger.LogInformation("Processing Stripe webhook event {EventType} with ID {EventId} (API Version: {ApiVersion})",
                stripeEvent.Type, stripeEvent.Id, stripeEvent.ApiVersion);

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
                    response.Processed = true; // Mark as processed to avoid retries
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
                        IntervalCount = (int)(price.Recurring?.IntervalCount ?? 1),
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
            var service = new Stripe.SubscriptionService();
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
            var service = new Stripe.SubscriptionService();

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
            var service = new Stripe.SubscriptionService();
            var options = new SubscriptionUpdateOptions();

            // Check if we need to update the price
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

                // Set proration behavior
                if (request.ProrationBehavior == true)
                {
                    options.ProrationBehavior = "create_prorations";
                }
                else
                {
                    options.ProrationBehavior = "none";
                }
            }

            // Update metadata if provided
            if (request.Metadata != null)
            {
                options.Metadata = request.Metadata;
            }

            var subscription = await service.UpdateAsync(subscriptionId, options);

            _logger.LogInformation("Updated Stripe subscription {SubscriptionId}", subscriptionId);

            return MapToSubscriptionResponse(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<StripeSubscriptionResponse> ChangePlanAsync(string subscriptionId, ChangePlanRequest request)
    {
        try
        {
            var service = new Stripe.SubscriptionService();

            // Get current subscription
            var currentSub = await service.GetAsync(subscriptionId);
            var currentItem = currentSub.Items.Data.FirstOrDefault();

            if (currentItem == null)
            {
                throw new InvalidOperationException("No subscription items found");
            }

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new()
                    {
                        Id = currentItem.Id,
                        Price = request.StripePriceId
                    }
                },
                ProrationBehavior = request.ProrateBilling ? "create_prorations" : "none",
                Metadata = new Dictionary<string, string>
                {
                    {"plan_id", request.NewPlanId},
                    {"is_annual", request.IsAnnual.ToString()},
                    {"changed_at", DateTime.UtcNow.ToString("O")}
                }
            };

            var subscription = await service.UpdateAsync(subscriptionId, options);

            _logger.LogInformation("Changed plan for subscription {SubscriptionId} to {NewPlan}",
                subscriptionId, request.NewPlanId);

            return MapToSubscriptionResponse(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change plan for subscription {SubscriptionId}", subscriptionId);
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

    // Webhook handlers - Use DbContext directly instead of ISubscriptionService
    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

        if (session?.Mode == "subscription" && session.SubscriptionId != null)
        {
            _logger.LogInformation("Checkout session completed for subscription {SubscriptionId}", session.SubscriptionId);
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
                // Update subscription directly in database
                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

                if (dbSubscription != null)
                {
                    dbSubscription.StripeCustomerId = subscription.CustomerId;
                    dbSubscription.StripeSubscriptionId = subscription.Id;
                    dbSubscription.StripePriceId = priceId;
                    dbSubscription.PlanName = GetPlanNameFromPriceId(priceId);
                    dbSubscription.MonthlyPrice = (subscription.Items.Data.FirstOrDefault()?.Price.UnitAmount ?? 0) / 100m;
                    dbSubscription.Status = Domain.Enums.SubscriptionStatus.Active;
                    dbSubscription.TrialEndDate = subscription.TrialEnd;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated subscription for organization {OrganizationId} with Stripe subscription {SubscriptionId}",
                        organizationId, subscription.Id);
                }
            }
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription?.Metadata?.ContainsKey("organization_id") == true)
        {
            var organizationId = subscription.Metadata["organization_id"];

            var dbSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

            if (dbSubscription != null)
            {
                // Update subscription status and other relevant fields
                dbSubscription.Status = subscription.Status switch
                {
                    "active" => Domain.Enums.SubscriptionStatus.Active,
                    "past_due" => Domain.Enums.SubscriptionStatus.PastDue,
                    "canceled" => Domain.Enums.SubscriptionStatus.Cancelled,
                    _ => dbSubscription.Status
                };

                dbSubscription.EndDate = subscription.CurrentPeriodEnd;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription status for organization {OrganizationId}: {Status}",
                    organizationId, subscription.Status);
            }
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription?.Metadata?.ContainsKey("organization_id") == true)
        {
            var organizationId = subscription.Metadata["organization_id"];

            var dbSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

            if (dbSubscription != null)
            {
                dbSubscription.Status = Domain.Enums.SubscriptionStatus.Cancelled;
                await _context.SaveChangesAsync();
            }

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
        return priceId switch
        {
            var id when id.Contains("starter") => "Starter",
            var id when id.Contains("professional") => "Professional",
            var id when id.Contains("enterprise") => "Enterprise",
            _ => "Professional" // Default
        };
    }
}