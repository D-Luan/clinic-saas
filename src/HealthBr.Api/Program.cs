using System.Text;

using FluentValidation;

using HealthBr.Api.Middlewares;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Auth.Commands;
using HealthBr.Application.Features.Auth.Validators;
using HealthBr.Domain.Repositories;
using HealthBr.Infrastructure.Auth;
using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;
using HealthBr.Infrastructure.Persistence.Repositories;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Configure it via user-secrets (dev) or environment variable (spec 12.2/11.2).");

builder.Services.AddDbContext<HealthBrDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

builder.Services.AddControllers();

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
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAccessTokenService, JwtTokenService>();
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddScoped<RefreshTokenCommandHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

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

app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "HealthBr API");

app.Run();

public partial class Program
{
}
