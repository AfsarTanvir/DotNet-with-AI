using FluentValidation;
using Notes.Application.Commands.DeleteNotes;

namespace Notes.Application.Validators
{
    public class DeleteNoteValidator : AbstractValidator<DeleteNoteCommand>
    {
        public DeleteNoteValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Note ID is required");
        }
    }
}
