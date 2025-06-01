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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<LogistiqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Organization-specific repositories
builder.Services.AddScoped<IOrganizationRepository<Product>, OrganizationRepository<Product>>();
builder.Services.AddScoped<IOrganizationRepository<Category>, OrganizationRepository<Category>>();
builder.Services.AddScoped<IOrganizationRepository<Order>, OrganizationRepository<Order>>();
builder.Services.AddScoped<IOrganizationRepository<Customer>, OrganizationRepository<Customer>>();
builder.Services.AddScoped<IOrganizationRepository<Supplier>, OrganizationRepository<Supplier>>();

// Complete Service Registration
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Application Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();

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
                    var organizationService = context.HttpContext.RequestServices.GetRequiredService<IOrganizationService>();

                    var claims = context.Principal?.Claims?.ToList() ?? new List<Claim>();
                    logger.LogInformation("JWT validated with claims: {Claims}",
                        string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

                    // Extract organization from JWT - try multiple claim locations
                    var organizationId = context.Principal?.FindFirst("org_id")?.Value
                                      ?? context.Principal?.FindFirst("organization_id")?.Value;

                    // If not found in direct claims, try parsing from 'o' claim
                    if (string.IsNullOrEmpty(organizationId))
                    {
                        var orgClaim = context.Principal?.FindFirst("o")?.Value;
                        if (!string.IsNullOrEmpty(orgClaim))
                        {
                            try
                            {
                                var orgData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(orgClaim);
                                if (orgData.TryGetProperty("id", out var idElement))
                                {
                                    organizationId = idElement.GetString();
                                }
                            }
                            catch (System.Text.Json.JsonException ex)
                            {
                                logger.LogWarning("Failed to parse organization claim: {Error}", ex.Message);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(organizationId))
                    {
                        // Auto-sync organization from Clerk context
                        var organizationName = "Unknown Organization"; // Default fallback

                        // Try to get org name from claims
                        var orgNameFromClaim = context.Principal?.FindFirst("org_name")?.Value;
                        if (!string.IsNullOrEmpty(orgNameFromClaim))
                        {
                            organizationName = orgNameFromClaim;
                        }
                        else
                        {
                            // Try to get from 'o' claim
                            var orgClaim = context.Principal?.FindFirst("o")?.Value;
                            if (!string.IsNullOrEmpty(orgClaim))
                            {
                                try
                                {
                                    var orgData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(orgClaim);
                                    if (orgData.TryGetProperty("id", out var idElement))
                                    {
                                        organizationName = idElement.GetString() ?? "Unknown Organization";
                                    }
                                }
                                catch (System.Text.Json.JsonException)
                                {
                                    // Use default name if parsing fails
                                }
                            }
                        }

                        await organizationService.SyncOrganizationAsync(new Logistiq.Application.Organizations.DTOs.SyncOrganizationRequest
                        {
                            Name = organizationName
                        });

                        logger.LogInformation("Organization synced: {OrganizationId}", organizationId);
                    }
                    else
                    {
                        logger.LogWarning("No organization ID found in JWT claims");
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during JWT validation");
                }
            },
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var organizationService = context.HttpContext.RequestServices.GetRequiredService<IOrganizationService>();

                    var claims = context.Principal?.Claims?.ToList() ?? new List<Claim>();
                    logger.LogInformation("JWT validated with claims: {Claims}",
                        string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

                    // Extract organization from JWT
                    var organizationId = context.Principal?.FindFirst("org_id")?.Value
                                      ?? context.Principal?.FindFirst("organization_id")?.Value;

                    if (!string.IsNullOrEmpty(organizationId))
                    {
                        // Auto-sync organization from Clerk context
                        var organizationName = context.Principal?.FindFirst("org_name")?.Value
                                            ?? context.Principal?.FindFirst("organization_name")?.Value
                                            ?? "Unknown Organization";

                        await organizationService.SyncOrganizationAsync(new Logistiq.Application.Organizations.DTOs.SyncOrganizationRequest
                        {
                            Name = organizationName
                        });

                        logger.LogInformation("Organization synced: {OrganizationId}", organizationId);
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

// CORS - Configure for Vite frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .SetIsOriginAllowed(origin => true);
        }
        else
        {
            policy.WithOrigins("https://yourdomain.com")
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