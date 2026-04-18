using MediatR;
using Notes.Application.Interfaces;

namespace Notes.Application.Commands.Login
{
    public class LoginHandle : IRequestHandler<LoginCommand, string>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public LoginHandle(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials. Please enter correct email and password.");
            }

            // Verify password with BCrypt (with exception handling for invalid hashes)
            try
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    throw new UnauthorizedAccessException("Invalid credentials. Please enter correct email and password.");
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // This happens if the stored hash is not a valid BCrypt hash
                // Likely a data issue - user created without proper hashing
                throw new UnauthorizedAccessException("Invalid credentials. Please enter correct email and password.");
            }

            return _authService.GenerateToken(user.Id, user.Email);
        }
    }
}
