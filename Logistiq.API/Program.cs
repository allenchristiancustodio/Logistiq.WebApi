using System.Reflection;
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
using System.Text;
using Logistiq.API.Middleware;

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

// JWT Authentication for Kinde M2M
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        // For M2M applications, we use the domain as the authority
        options.Authority = builder.Configuration["Kinde:Domain"];
        // The audience should be your API identifier in Kinde
        options.Audience = builder.Configuration["Kinde:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow some clock skew
            // For M2M tokens, the name claim might be different
            NameClaimType = "sub"
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                    // Get the subject claim (Kinde user ID)
                    var kindeUserId = context.Principal?.FindFirst("sub")?.Value;

                    logger.LogInformation("Token validated for Kinde user: {KindeUserId}", kindeUserId);

                    if (!string.IsNullOrEmpty(kindeUserId))
                    {
                        var user = await userRepository.GetUserWithCompaniesByKindeIdAsync(kindeUserId);

                        // Add company claim if user exists and has active company membership
                        if (user != null)
                        {
                            var activeCompanyUser = user.CompanyUsers?.FirstOrDefault(cu => cu.IsActive);
                            if (activeCompanyUser != null)
                            {
                                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                                identity?.AddClaim(new System.Security.Claims.Claim("company_id", activeCompanyUser.CompanyId.ToString()));

                                logger.LogInformation("Added company claim: {CompanyId} for user: {KindeUserId}",
                                    activeCompanyUser.CompanyId, kindeUserId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during token validation for user");
                    // Don't fail authentication, just log the error
                }
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT Authentication failed: {Failure}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Challenge triggered: {Error} - {ErrorDescription}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS - Configure for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
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
        Description = "Inventory Management SaaS API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your Kinde M2M token.",
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
app.UseCors("AllowNextJS");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add a health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Database Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogistiqDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();