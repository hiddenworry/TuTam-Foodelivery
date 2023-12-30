using System.Security.Cryptography;

namespace BusinessLogic.Utils.SecurityServices.Implements
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 20;
        private const int Iterations = 10000;

        public string Hash(string password)
        {
            // Generate a salt value to use with the hash
            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            // Hash the password with the salt
            var hashedBytes = new Rfc2898DeriveBytes(password, salt, Iterations);
            byte[] hash = hashedBytes.GetBytes(HashSize);
            // Combine the salt and hash into a single string
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);
            string hashedPassword = Convert.ToBase64String(hashBytes);
            return hashedPassword;
        }

        public bool Verify(string password, string hashedPassword)
        {
            try
            {
                // Convert the hashed password from base64 string to bytes
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                // Extract the salt from the hash
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Hash the entered password with the extracted salt
                var hashedBytes = new Rfc2898DeriveBytes(password, salt, Iterations);
                byte[] hash = hashedBytes.GetBytes(HashSize);

                // Compare the computed hash with the stored hash
                for (int i = 0; i < HashSize; i++)
                {
                    if (hashBytes[i + SaltSize] != hash[i])
                    {
                        return false; // Passwords don't match
                    }
                }

                return true; // Passwords match
            }
            catch
            {
                return false; // An exception occurred, indicating invalid input
            }
        }

        public string GenerateNewPassword()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32];
                rng.GetBytes(tokenBytes);
                var base64String = Convert.ToBase64String(tokenBytes);
                return base64String.TrimEnd('=');
            }
        }
    }
}
