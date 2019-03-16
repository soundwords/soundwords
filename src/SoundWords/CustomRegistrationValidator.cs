using ServiceStack.FluentValidation;
using ServiceStack;
using ServiceStack.Auth;

namespace SoundWords
{
    public class CustomRegistrationValidator : RegistrationValidator
    {
        public CustomRegistrationValidator()
        {
            RuleSet(ApplyTo.Post,
                () =>
                {
                    RuleFor(x => x.DisplayName).NotEmpty();
                    RuleFor(x => x.DisplayName)
                        .Must(displayName => displayName.Contains(" "))
                        .WithErrorCode("InvalidName")
                        .WithMessage("Navnet må være på formen Fornavn Etternavn")
                        .When(x => !x.DisplayName.IsNullOrEmpty());
                });
        }
    }
}