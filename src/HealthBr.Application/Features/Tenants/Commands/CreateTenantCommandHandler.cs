using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Features.Tenants.Dto;
using HealthBr.Domain.Entities;
using HealthBr.Domain.Enums;
using HealthBr.Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace HealthBr.Application.Features.Tenants.Commands;

public sealed class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<CreateTenantCommandHandler> logger)
{
    public async Task<CreateTenantResult> HandleAsync(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        // Global uniqueness among ACTIVE users, consistent with the login
        // lookup and the per-tenant filtered index (spec 5.1): a soft-deleted
        // user's e-mail can be reused, a live one anywhere blocks signup.
        var existing = await userRepository.FindActiveByEmailAsync(command.AdminEmail, cancellationToken);
        if (existing.Count > 0)
        {
            logger.LogWarning(
                "Tenant provisioning rejected: admin e-mail already active ({AdminEmail})",
                command.AdminEmail);
            throw new DomainException(
                "ADMIN_EMAIL_ALREADY_EXISTS",
                "Já existe um usuário ativo com este e-mail.",
                new Dictionary<string, string> { ["adminEmail"] = command.AdminEmail });
        }

        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var tenant = new Tenant
        {
            Id = tenantId,
            TenantId = tenantId, // the tenant is its own tenant (PR #3 decision)
            Name = command.ClinicName,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = command.AdminName,
            Email = command.AdminEmail, // canonical form, see CreateTenantCommand
            PasswordHash = passwordHasher.HashPassword(command.AdminPassword),
            Role = UserRole.Doctor, // first user administers the tenant (spec 5.1)
            CreatedAt = now,
            UpdatedAt = now,
        };

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await userRepository.AddAsync(admin, cancellationToken);

        // One SaveChangesAsync = one implicit transaction, so tenant and admin
        // user are atomic (spec 5.1). An explicit transaction is avoided on
        // purpose: it would require an execution strategy because
        // EnableRetryOnFailure is enabled, and that plumbing belongs to the
        // IUnitOfWork planned for task 4.2.
        await tenantRepository.SaveChangesAsync(cancellationToken);

        // Spec 15.6: tenant creation is a security event; origin IP and trace
        // id ride the request log scope set in the Api pipeline.
        logger.LogInformation(
            "Tenant created (TenantId={TenantId}, AdminEmail={AdminEmail})",
            tenantId,
            admin.Email);

        return new CreateTenantResult(tenantId);
    }
}
