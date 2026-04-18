using MediatR;
using Microsoft.AspNetCore.Http;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;
using Notes.Domain.Exceptions;
using System.Security.Claims;

namespace Notes.Application.Queries.GetNote
{
    public class GetNoteHandler : IRequestHandler<GetNoteQuery, Note?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetNoteHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Note?> Handle(GetNoteQuery command, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("You do not have permission to access this note.");

            return note;
        }
    }
}
