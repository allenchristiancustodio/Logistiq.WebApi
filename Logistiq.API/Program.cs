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
        // Clerk configuration
        options.Authority = builder.Configuration["Clerk:Authority"];
        options.Audience = builder.Configuration["Clerk:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),

            // Clerk typically uses 'sub' for the user ID
            NameClaimType = "sub",

            // You might need to adjust these based on your Clerk configuration
            ValidIssuers = new[] { "https://ruling-wren-85.clerk.accounts.dev" },
            ValidAudiences = new[] { "https://ruling-wren-85.clerk.accounts.dev" }
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                    // Get the subject claim (Clerk user ID)
                    var clerkUserId = context.Principal?.FindFirst("sub")?.Value;
                    var email = context.Principal?.FindFirst("email")?.Value;

                    logger.LogInformation("Token validated for Clerk user: {ClerkUserId}, Email: {Email}",
                        clerkUserId, email);

                    if (!string.IsNullOrEmpty(clerkUserId))
                    {
                        // In Clerk, the user ID from 'sub' claim is what we store as KindeUserId
                        // You might need to adjust this based on how you want to handle the migration
                        var user = await userRepository.GetUserWithCompaniesByKindeIdAsync(clerkUserId);

                        if (user != null)
                        {
                            var activeCompanyUser = user.CompanyUsers?.FirstOrDefault(cu => cu.IsActive);
                            if (activeCompanyUser != null)
                            {
                                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                                identity?.AddClaim(new System.Security.Claims.Claim("company_id", activeCompanyUser.CompanyId.ToString()));

                                logger.LogInformation("Added company claim: {CompanyId} for user: {ClerkUserId}",
                                    activeCompanyUser.CompanyId, clerkUserId);
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
app.UseCors("AllowVite"); // Updated CORS policy name

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
        logger.LogInformation("Database migration completed successfully with Clerk auth!");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();