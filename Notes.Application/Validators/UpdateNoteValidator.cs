using FluentValidation;
using Notes.Application.Commands.UpdateNote;

namespace Notes.Application.Validators
{
    public class UpdateNoteValidator : AbstractValidator<UpdateNoteCommand>
    {
        public UpdateNoteValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100);
        }
    }
}
