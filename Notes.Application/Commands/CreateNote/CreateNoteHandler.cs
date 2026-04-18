using MediatR;
using Microsoft.AspNetCore.Http;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;
using System.Security.Claims;

namespace Notes.Application.Commands.CreateNote
{
    public class CreateNoteHandler : IRequestHandler<CreateNoteCommand, Note>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateNoteHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Note> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
        {
            // Extract userId from JWT claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user context. Token must contain valid userId.");

            var note = new Note(request.Title, request.Content, userId);

            await _unitOfWork.Notes.AddAsync(note, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return note;
        }
    }
}
