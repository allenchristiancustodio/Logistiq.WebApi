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
using Logistiq.Application.Products.Commands.CreateProduct;
using Microsoft.IdentityModel.Tokens;
using Logistiq.API.Middleware;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<LogistiqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductCommandValidator).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

// Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
builder.Services.AddScoped(typeof(ITenantRepository<>), typeof(TenantRepository<>));
builder.Services.AddScoped(typeof(ITenantRepository<,>), typeof(TenantRepository<,>));

// Specific repository registrations
builder.Services.AddScoped<IRepository<ApplicationUser>, Repository<ApplicationUser>>();
builder.Services.AddScoped<IRepository<Company>, Repository<Company>>();
builder.Services.AddScoped<IRepository<CompanyUser>, Repository<CompanyUser>>();
builder.Services.AddScoped<IRepository<Subscription>, Repository<Subscription>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICompanyManagementService, CompanyManagementService>();

// JWT Authentication for Clerk
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://master-grouse-87.clerk.accounts.dev";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, 
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),

            // Clerk uses 'sub' for the user ID
            NameClaimType = "sub",
            RoleClaimType = "role",

            // Set the valid issuer to match your Clerk domain
            ValidIssuer = "https://master-grouse-87.clerk.accounts.dev",
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                    // Log all claims for debugging
                    var claims = context.Principal?.Claims?.ToList() ?? new List<Claim>();
                    logger.LogInformation("Token validated successfully! Claims: {Claims}",
                        string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

                    // Extract Clerk user ID - try multiple claim types
                    var clerkUserId = context.Principal?.FindFirst("sub")?.Value
                                   ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? context.Principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                    // Extract email - try multiple claim types  
                    var email = context.Principal?.FindFirst("email")?.Value
                             ?? context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                             ?? context.Principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                    logger.LogInformation("Extracted - Clerk user: {ClerkUserId}, Email: {Email}",
                        clerkUserId, email ?? "not_in_token");

                    if (!string.IsNullOrEmpty(clerkUserId))
                    {
                        // Use the new Clerk method
                        var user = await userRepository.GetUserWithCompaniesByClerkIdAsync(clerkUserId);

                        if (user != null)
                        {
                            var activeCompanyUser = user.CompanyUsers?.FirstOrDefault(cu => cu.IsActive);
                            if (activeCompanyUser != null)
                            {
                                var identity = context.Principal?.Identity as ClaimsIdentity;
                                identity?.AddClaim(new Claim("company_id", activeCompanyUser.CompanyId.ToString()));

                                logger.LogInformation("Added company claim: {CompanyId} for user: {ClerkUserId}",
                                    activeCompanyUser.CompanyId, clerkUserId);
                            }
                            else
                            {
                                logger.LogInformation("User has no active company: {ClerkUserId}", clerkUserId);
                            }
                        }
                        else
                        {
                            logger.LogInformation("User not found in database: {ClerkUserId} - user may need to be created via webhook", clerkUserId);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Could not extract user ID from token claims");
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during token validation");
                    // Don't fail authentication, just log the error
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
            // More permissive in development
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .SetIsOriginAllowed(origin => true);
        }
        else
        {
            // More restrictive in production
            policy.WithOrigins("https://yourdomain.com") // Replace with your production domain
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

// Add a health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Logistiq API starting with Clerk authentication");
logger.LogInformation("Clerk Authority: https://master-grouse-87.clerk.accounts.dev");
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