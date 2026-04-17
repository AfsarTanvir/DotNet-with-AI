using MediatR;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Application.Queries.GetUser
{
    public class GetUserHandler : IRequestHandler<GetUserQuery, User?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<User?> Handle(GetUserQuery request, CancellationToken ct)
        {
            return await _unitOfWork.Users.GetByIdAsync(request.Id, ct);
        }
    }
}
