using System.Text;

using FluentValidation;

using HealthBr.Api.Filters;
using HealthBr.Api.Middlewares;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Auth.Commands;
using HealthBr.Application.Features.Auth.Validators;
using HealthBr.Application.Features.Tenants.Commands;
using HealthBr.Domain.Repositories;
using HealthBr.Infrastructure.Auth;
using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;
using HealthBr.Infrastructure.Persistence.Repositories;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Configure it via user-secrets (dev) or environment variable (spec 12.2/11.2).");

builder.Services.AddDbContext<HealthBrDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

builder.Services.AddControllers(options =>
{
    // Validation failures travel the same route as every other error: a
    // ValidationException handed to the global error pipeline, so the API has
    // a single error contract (spec 10.1/15.2).
    options.Filters.Add<FluentValidationFilter>();
});

// Binding failures FluentValidation never sees (malformed JSON, missing
// required fields) follow the same error contract (spec 15.2).
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var failures = actionContext.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors
                .Select(error => new FluentValidation.Results.ValidationFailure(entry.Key, error.ErrorMessage)))
            .ToList();
        throw new ValidationException(failures);
    };
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'Jwt' is missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is required and must have at least 32 characters (256 bits). Configure it via user-secrets (dev) or Key Vault (spec 12.2/15.8).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claims keep the exact names they were issued with (sub, email,
        // role, tenant_id — spec 5.1); role authorization reads the short
        // "role" claim (policies arrive with task 2.2).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidateIssuer = true,
            ValidAudience = jwtOptions.Audience,
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = AuthConstants.RoleClaim,
        };
        options.Events = new JwtBearerEvents
        {
            // Spec 15.6: invalid or expired tokens are security events.
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("HealthBr.Api.Auth");
                logger.LogWarning("Token JWT rejected: {Reason}", context.Exception.Message);
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// The tenant context is resolved as the interface everywhere; only the
// authentication middleware receives the concrete type, making it the sole
// caller of SetTenantId (spec 15.7).
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAccessTokenService, JwtTokenService>();
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddScoped<RefreshTokenCommandHandler>();
builder.Services.AddScoped<CreateTenantCommandHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Health checks (spec 3.2/10.3): /health is a self ping, /health/ready adds
// the SQL probe the Azure App Service watches. Both are public on purpose —
// probes do not carry a token.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HealthBrDbContext>(name: "sql-server", tags: ["ready"]);

// Swagger (spec 3.2): enabled in Development and Staging only; the pipeline
// below never mounts it in Production. Bearer definition lets the UI test
// the auth endpoints from task 1.3 directly.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthBr API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token emitted by POST /api/v1/auth/login.",
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = [],
    });
});

var app = builder.Build();

// Security-event logs carry origin IP and trace id through a request scope
// (spec 15.6).
var requestLogger = app.Logger;
app.Use(async (context, next) =>
{
    using var logScope = requestLogger.BeginScope(new Dictionary<string, object>
    {
        ["RemoteIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        ["TraceId"] = context.TraceIdentifier,
    });
    await next();
});

// Global error pipeline (spec 10.1). It sits outside the security-headers
// middleware, which sets its headers before calling next() — so error
// responses keep them too.
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>(!app.Environment.IsProduction());

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

// /health answers as long as the process answers; /health/ready additionally
// pings the SQL database (spec 3.2).
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});

app.Run();

public partial class Program
{
}
