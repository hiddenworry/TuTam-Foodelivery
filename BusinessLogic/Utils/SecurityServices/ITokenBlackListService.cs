namespace BusinessLogic.Utils.SecurityServices
{
    public interface ITokenBlacklistService
    {
        public void AddTokenToBlacklist(string token, DateTimeOffset expirationTime);
        public bool IsTokenBlacklisted(string token);
    }
}
