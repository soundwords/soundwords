using FluentValidation;

namespace SoundWords.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty();
        RuleFor(x => x.DisplayName)
            .Must(displayName => displayName.Contains(' '))
            .WithErrorCode("InvalidName")
            .WithMessage("Navnet må være på formen Fornavn Etternavn")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));
    }
}
