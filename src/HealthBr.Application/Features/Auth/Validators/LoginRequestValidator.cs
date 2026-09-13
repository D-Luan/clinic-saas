using FluentValidation;

using HealthBr.Application.Features.Auth.Dto;

namespace HealthBr.Application.Features.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
