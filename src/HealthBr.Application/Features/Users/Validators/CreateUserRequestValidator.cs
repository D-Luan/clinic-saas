using FluentValidation;

using HealthBr.Application.Features.Users.Dto;

namespace HealthBr.Application.Features.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    // Same rules as the tenant provisioning validator (task 2.1): PT-BR
    // messages surfaced verbatim in ProblemDetails details, lengths mirroring
    // the persistence mapping (User.Name and User.Email columns), and the
    // documented MVP password policy.
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Informe o nome do usuário.")
            .MaximumLength(200).WithMessage("O nome do usuário deve ter no máximo 200 caracteres.");

        // The e-mail is already canonical by the time validation runs (see
        // CreateUserRequest), matching what is persisted and looked up at
        // login (spec 5.1/15.2).
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Informe o e-mail do usuário.")
            .MaximumLength(256).WithMessage("O e-mail deve ter no máximo 256 caracteres.")
            .EmailAddress().WithMessage("Informe um e-mail válido.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.")
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um dígito.");
    }
}
