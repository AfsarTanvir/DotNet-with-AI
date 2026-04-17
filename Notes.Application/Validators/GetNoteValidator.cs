using FluentValidation;
using Notes.Application.Queries.GetNote;

namespace Notes.Application.Validators
{
    public class GetNoteValidator : AbstractValidator<GetNoteQuery>
    {
        public GetNoteValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Note ID is required");
        }
    }
}
