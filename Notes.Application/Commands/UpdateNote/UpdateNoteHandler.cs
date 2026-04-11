using Notes.Application.Interfaces;
using Notes.Domain.Exceptions;

namespace Notes.Application.Commands.UpdateNote
{
    public class UpdateNoteHandler
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateNoteHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateNoteCommand command, CancellationToken cancellationToken)
        {
            var note = await _unitOfWork.Notes.GetByIdAsync(command.Id, cancellationToken);

            if (note == null)
                throw new NoteNotFoundException(command.Id);

            note.SetTitle(command.Title);
            note.UpdateContent(command.Content);

            _unitOfWork.Notes.Update(note);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
