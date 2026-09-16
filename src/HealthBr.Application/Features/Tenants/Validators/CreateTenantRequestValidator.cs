using FluentValidation;

using HealthBr.Application.Features.Tenants.Dto;

namespace HealthBr.Application.Features.Tenants.Validators;

public sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    // PT-BR messages on purpose: these details surface verbatim in the
    // ProblemDetails consumed by the signup UI (spec 15.2). Lengths mirror the
    // persistence mapping (Tenant.Name, User.Name and User.Email columns).
    public CreateTenantRequestValidator()
    {
        RuleFor(request => request.ClinicName)
            .NotEmpty().WithMessage("Informe o nome da clínica.")
            .MaximumLength(200).WithMessage("O nome da clínica deve ter no máximo 200 caracteres.");

        RuleFor(request => request.AdminName)
            .NotEmpty().WithMessage("Informe o nome do administrador.")
            .MaximumLength(200).WithMessage("O nome do administrador deve ter no máximo 200 caracteres.");

        // The e-mail is already canonical by the time validation runs (see
        // CreateTenantRequest): trimmed and lower-cased, matching what is
        // persisted and looked up at login (spec 5.1/15.2).
        RuleFor(request => request.AdminEmail)
            .NotEmpty().WithMessage("Informe o e-mail do administrador.")
            .MaximumLength(256).WithMessage("O e-mail deve ter no máximo 256 caracteres.")
            .EmailAddress().WithMessage("Informe um e-mail válido.");

        // Reasonable MVP policy (documented in the task 2.1 PR): minimum 8 and
        // maximum 128 characters — the same cap as the login validator — with
        // at least one uppercase letter, one lowercase letter and one digit.
        RuleFor(request => request.AdminPassword)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.")
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um dígito.");
    }
}
