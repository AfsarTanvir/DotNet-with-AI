using FluentValidation;
using Notes.Application.Commands.CreateUser;

namespace Notes.Application.Validators
{
    public class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FirstName is required")
                .MaximumLength(50);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LastName is required")
                .MaximumLength(50);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email must be valid");

            RuleFor(x => x.PasswordHash)
                .NotEmpty().WithMessage("PasswordHash is required")
                .MinimumLength(8).WithMessage("PasswordHash must be at least 8 characters");
        }
    }
}
