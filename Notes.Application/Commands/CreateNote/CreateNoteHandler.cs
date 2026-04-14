using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.CreateNote
{
    public class CreateNoteHandler
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateNoteHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateNoteCommand request, CancellationToken ct)
        {
            var note = new Note(request.Title, request.Content, request.UserId);

            await _unitOfWork.Notes.AddAsync(note, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return note.Id;
        }
    }
}
