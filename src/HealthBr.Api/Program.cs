using HealthBr.Application.Common.Interfaces;
using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Configure it via user-secrets (dev) or environment variable (spec 12.2/11.2).");

builder.Services.AddDbContext<HealthBrDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddScoped<ITenantContext, TenantContext>();

var app = builder.Build();

app.MapGet("/", () => "HealthBr API");

app.Run();
