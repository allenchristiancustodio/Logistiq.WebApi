using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Logistiq.Application.Common.Behaviours;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;
using Logistiq.Infrastructure.Services;
using Logistiq.Persistence.Data;
using Logistiq.Persistence.Repositories;
using Microsoft.IdentityModel.Tokens;
using Logistiq.API.Middleware;
using System.Security.Claims;
using Logistiq.Application.Users;
using Logistiq.Application.Organizations;
using Logistiq.Application.Products;
using Logistiq.Application.Products.Validation;
using Logistiq.Application.Categories;
using Logistiq.Application.Subscriptions;
using Logistiq.Application.Payments;
using Logistiq.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//Database For SQL :
builder.Services.AddDbContext<LogistiqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// For PostgresSQL
//builder.Services.AddDbContext<LogistiqDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

// Complete Repository Registration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Standard repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

// Organization-aware repositories
builder.Services.AddScoped(typeof(IOrganizationRepository<>), typeof(OrganizationRepository<>));
builder.Services.AddScoped(typeof(IOrganizationRepository<,>), typeof(OrganizationRepository<,>));

// Specific repository registrations
builder.Services.AddScoped<IRepository<ApplicationUser>, Repository<ApplicationUser>>();
builder.Services.AddScoped<IRepository<Organization>, Repository<Organization>>();
builder.Services.AddScoped<IRepository<Subscription>, Repository<Subscription>>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Organization-specific repositories
builder.Services.AddScoped<IOrganizationRepository<Product>, OrganizationRepository<Product>>();
builder.Services.AddScoped<IOrganizationRepository<Category>, OrganizationRepository<Category>>();
builder.Services.AddScoped<IOrganizationRepository<Order>, OrganizationRepository<Order>>();
builder.Services.AddScoped<IOrganizationRepository<Customer>, OrganizationRepository<Customer>>();
builder.Services.AddScoped<IOrganizationRepository<Supplier>, OrganizationRepository<Supplier>>();

// Complete Service Registration For Current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Application Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPlanManagementService, PlanManagementService>();

// Stripe Configuration
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IStripeService, StripeService>();

// JWT Authentication for Clerk
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Clerk:Authority"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ValidIssuer = builder.Configuration["Clerk:Authority"],
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                    var claims = context.Principal?.Claims?.ToList() ?? new List<Claim>();
                    logger.LogInformation("JWT validated with claims: {Claims}",
                        string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

                    // Get organization ID from the org_id claim
                    var organizationId = context.Principal?.FindFirst("org_id")?.Value;

                    if (!string.IsNullOrEmpty(organizationId))
                    {
                        logger.LogInformation("Organization found in JWT: {OrganizationId}", organizationId);

                        var organizationService = context.HttpContext.RequestServices.GetRequiredService<IOrganizationService>();

                        var orgName = context.Principal?.FindFirst("org_name")?.Value
                                   ?? context.Principal?.FindFirst("org_slug")?.Value
                                   ?? "Unknown Organization";

                        await organizationService.SyncOrganizationAsync(new Logistiq.Application.Organizations.DTOs.SyncOrganizationRequest
                        {
                            Name = orgName
                        });

                        logger.LogInformation("Organization synced: {OrganizationId} - {OrgName}", organizationId, orgName);
                    }
                    else
                    {
                        logger.LogInformation("No organization ID found in JWT claims");
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during JWT validation");
                }
            },
        };
    });


builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://logistiq-web-app-five.vercel.app")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .SetIsOriginAllowed(origin => true);
        }
        else
        {
            policy.WithOrigins("https://logistiq-web-app-five.vercel.app")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Logistiq API",
        Version = "v1",
        Description = "Inventory Management SaaS API with Clerk Authentication"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your Clerk token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<ExceptionMiddleware>();
}

app.UseHttpsRedirection();
app.UseCors("AllowVite");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SubscriptionLimitMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Logistiq API starting with Clerk authentication");
logger.LogInformation("Clerk Authority: {Authority}", builder.Configuration["Clerk:Authority"]);
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

// Database Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogistiqDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        var services = scope.ServiceProvider;
        var migrationLogger = services.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogInformation("Database migration completed successfully!");
    }
    catch (Exception ex)
    {
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogError(ex, "An error occurred while migrating the database");
    }
}

app.Run();