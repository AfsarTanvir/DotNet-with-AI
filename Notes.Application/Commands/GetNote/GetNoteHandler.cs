using MediatR;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.GetNote
{
    public class GetNoteHandler : IRequestHandler<GetNoteQuery, Note?>
    {
        private readonly INoteRepository _noteRepository;

        public GetNoteHandler(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<Note?> Handle(GetNoteQuery command, CancellationToken cancellationToken)
        {
            return await _noteRepository.GetByIdAsync(command.id, cancellationToken);
        }
    }
}
