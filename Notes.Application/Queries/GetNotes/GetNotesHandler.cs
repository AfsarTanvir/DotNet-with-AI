using MediatR;
using Microsoft.AspNetCore.Http;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;
using System.Security.Claims;

namespace Notes.Application.Queries.GetNotes
{
    public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<Note>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetNotesHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Note>> Handle(GetNotesQuery query, CancellationToken cancellationToken)
        {
            // Extract userId from JWT claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user context. Token must contain valid userId.");

            var notes = await _unitOfWork.Notes.GetAllAsync(cancellationToken);

            // Filter to only return notes created by this user
            return notes.Where(n => n.CreatedBy == userId).ToList();
        }
    }
}
