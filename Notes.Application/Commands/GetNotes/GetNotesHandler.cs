using MediatR;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.GetNotes
{
    public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<Note>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetNotesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Note>> Handle(GetNotesQuery query, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Notes.GetAllAsync(cancellationToken);
        }
    }
}
