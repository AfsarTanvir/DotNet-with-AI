namespace Notes.Application.Interfaces
{
    public interface IAuthService
    {
        string GenerateToken(Guid userId, string email);
    }
}
