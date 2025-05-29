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

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICompanyManagementService, CompanyManagementService>();

// Kinde Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Kinde:Domain"];
        options.Audience = builder.Configuration["Kinde:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            //for better debugging
            ValidIssuer = builder.Configuration["Kinde:Domain"],
            ValidAudience = builder.Configuration["Kinde:Audience"]
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },

            OnTokenValidated = async context =>
            {
                try
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userService = context.HttpContext.RequestServices.GetRequiredService<IRepository<ApplicationUser>>();
                    var companyUserService = context.HttpContext.RequestServices.GetRequiredService<IRepository<CompanyUser>>();
                    var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

                    var kindeUserId = context.Principal?.FindFirst("sub")?.Value;

                    if (string.IsNullOrEmpty(kindeUserId))
                    {
                        logger.LogWarning("No sub claim found in token");
                        return;
                    }

                    // Check if user exists
                    var user = await userService.FirstOrDefaultAsync(u => u.KindeUserId == kindeUserId);

                    if (user == null)
                    {
                        logger.LogInformation("Creating new user for Kinde ID: {KindeUserId}", kindeUserId);

                        var email = context.Principal?.FindFirst("email")?.Value ?? "";
                        var firstName = context.Principal?.FindFirst("given_name")?.Value ?? "";
                        var lastName = context.Principal?.FindFirst("family_name")?.Value ?? "";

                        if (string.IsNullOrEmpty(email))
                        {
                            logger.LogWarning("No email found in token for user {KindeUserId}", kindeUserId);
                            return;
                        }

                        user = new ApplicationUser
                        {
                            KindeUserId = kindeUserId,
                            Email = email,
                            FirstName = firstName,
                            LastName = lastName,
                            IsActive = true
                        };

                        await userService.AddAsync(user);
                        await unitOfWork.SaveChangesAsync();

                        logger.LogInformation("Created new user: {Email}", email);

                        // New users won't have companies yet, so skip company claim logic
                        return;
                    }

                    // For existing users, check for active company
                    var activeCompanyUser = await companyUserService.FirstOrDefaultAsync(
                        cu => cu.ApplicationUserId == user.Id && cu.IsActive);

                    if (activeCompanyUser != null)
                    {
                        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                        identity?.AddClaim(new System.Security.Claims.Claim("company_id", activeCompanyUser.CompanyId.ToString()));
                        identity?.AddClaim(new System.Security.Claims.Claim("user_id", user.Id.ToString()));
                        logger.LogInformation("Added company claim for user {Email}: {CompanyId}", user.Email, activeCompanyUser.CompanyId);
                    }
                    else
                    {
                        // Add user_id claim even without company
                        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                        identity?.AddClaim(new System.Security.Claims.Claim("user_id", user.Id.ToString()));
                        logger.LogInformation("No active company found for user {Email}", user.Email);
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during token validation");
                    // Don't fail authentication, but log the error
                }
            }
        };
    });

builder.Services.AddAuthorization();

// CORS - Update for your Next.js app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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
}

// Add exception middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowNextJS");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogistiqDbContext>();
    try
    {
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();