using Notes.Application.Interfaces;
using Notes.Domain.Entities;
using Notes.Domain.Exceptions;

namespace Notes.Application.Commands.CreateNote
{
    public class CreateNoteHandler
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateNoteHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateNoteCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var note = new Note(command.Title, command.Content, command.UserId);

                await _unitOfWork.Notes.AddAsync(note, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return note.Id;
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException(ex.Message);
            }
        }
    }
}
