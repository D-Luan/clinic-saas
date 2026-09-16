using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Features.Users.Dto;
using HealthBr.Domain.Entities;
using HealthBr.Domain.Enums;
using HealthBr.Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace HealthBr.Application.Features.Users.Commands;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITenantContext tenantContext,
    ILogger<CreateUserCommandHandler> logger)
{
    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Global uniqueness among ACTIVE users (decision fixed in the task
        // 2.2 brief, consistent with login/2.1): the login lookup is global
        // and ambiguous e-mails authenticate as nobody, so a duplicate in
        // ANOTHER tenant would create an account that could never log in —
        // rejected here with 409 and a generic message that does not reveal
        // in which tenant the e-mail already exists (spec 15.1).
        var existing = await userRepository.FindActiveByEmailAsync(command.Email, cancellationToken);
        if (existing.Count > 0)
        {
            logger.LogWarning(
                "User creation rejected: e-mail already active ({Email})",
                command.Email);
            throw new DomainException(
                "USER_EMAIL_ALREADY_EXISTS",
                "Já existe um usuário ativo com este e-mail.",
                new Dictionary<string, string> { ["email"] = command.Email });
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId, // from the JWT, never the payload (spec 15.1)
            Name = command.Name,
            Email = command.Email,
            PasswordHash = passwordHasher.HashPassword(command.Password),
            Role = UserRole.Receptionist, // spec 9: POST /users creates Receptionist only; Doctors exist via provisioning
            CreatedAt = now,
            UpdatedAt = now,
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        // Spec 15.6: user creation is a security event; origin IP and trace
        // id ride the request log scope set in the Api pipeline.
        logger.LogInformation(
            "User created (UserId={UserId}, TenantId={TenantId}, Role={Role})",
            user.Id,
            user.TenantId,
            user.Role);

        return new CreateUserResult(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
    }
}
