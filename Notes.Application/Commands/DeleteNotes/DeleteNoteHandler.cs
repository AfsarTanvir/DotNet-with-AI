using MediatR;
using Microsoft.AspNetCore.Http;
using Notes.Application.Interfaces;
using Notes.Domain.Exceptions;
using System.Security.Claims;

namespace Notes.Application.Commands.DeleteNotes
{
    public class DeleteNoteHandler : IRequestHandler<DeleteNoteCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeleteNoteHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Unit> Handle(DeleteNoteCommand command, CancellationToken cancellationToken)
        {
            // Extract userId from JWT claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user context. Token must contain valid userId.");

            var note = await _unitOfWork.Notes.GetByIdAsync(command.Id, cancellationToken);

            if (note == null)
                throw new NoteNotFoundException(command.Id);

            // Verify user owns this note
            if (note.CreatedBy != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this note.");

            note.SoftDelete();

            _unitOfWork.Notes.Update(note);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
