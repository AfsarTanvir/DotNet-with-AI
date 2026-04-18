using MediatR;
using Microsoft.AspNetCore.Http;
using Notes.Application.Interfaces;
using Notes.Domain.Exceptions;
using System.Security.Claims;

namespace Notes.Application.Commands.UpdateNote
{
    public class UpdateNoteHandler : IRequestHandler<UpdateNoteCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateNoteHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Unit> Handle(UpdateNoteCommand command, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("You do not have permission to update this note.");

            note.SetTitle(command.Title);
            note.UpdateContent(command.Content);

            _unitOfWork.Notes.Update(note);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
