namespace BusinessLogic.Utils.SecurityServices
{
    public interface IPasswordHasher
    {
        string GenerateNewPassword();
        string Hash(string password);
        bool Verify(string password, string hashedPassword);
    }
}
