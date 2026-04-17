using MediatR;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;
using Notes.Domain.Exceptions;

namespace Notes.Application.Queries.GetNote
{
    public class GetNoteHandler : IRequestHandler<GetNoteQuery, Note?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetNoteHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Note?> Handle(GetNoteQuery command, CancellationToken cancellationToken)
        {
            var note = await _unitOfWork.Notes.GetByIdAsync(command.Id, cancellationToken);

            if (note == null)
                throw new NoteNotFoundException(command.Id);

            return note;
        }
    }
}
