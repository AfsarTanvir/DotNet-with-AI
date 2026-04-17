using MediatR;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.CreateUser
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateUserHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
        {
            var user = new User(request.FirstName, request.LastName, request.Email, request.PasswordHash);

            await _unitOfWork.Users.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return user;
        }
    }
}
