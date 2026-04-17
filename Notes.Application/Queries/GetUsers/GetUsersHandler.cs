using MediatR;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Application.Queries.GetUsers
{
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<User>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUsersHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<User>> Handle(GetUsersQuery request, CancellationToken ct)
        {
            return await _unitOfWork.Users.GetAllAsync(ct);
        }
    }
}
