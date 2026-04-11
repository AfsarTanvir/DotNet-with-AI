using Notes.Application.Interfaces;
using Notes.Domain.Exceptions;

namespace Notes.Application.Commands.DeleteNotes
{
    public class DeleteNoteHandler
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteNoteHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteNoteCommand command, CancellationToken cancellationToken)
        {
            var note = await _unitOfWork.Notes.GetByIdAsync(command.Id, cancellationToken);

            if (note == null)
                throw new NoteNotFoundException(command.Id);

            note.SoftDelete();

            _unitOfWork.Notes.Update(note);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
