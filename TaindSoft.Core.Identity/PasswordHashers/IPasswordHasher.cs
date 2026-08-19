namespace TaindSoft.Core.Identity.PasswordHashers
{
    /// <summary>
    /// TODO: Document interface IPasswordHasher
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string hashedPassword);
    }
}
