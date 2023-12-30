using Microsoft.Extensions.Caching.Memory;

namespace BusinessLogic.Utils.SecurityServices.Implements
{
    public class TokenBlackListService : ITokenBlacklistService
    {
        private readonly IMemoryCache _cache;

        public TokenBlackListService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsTokenBlacklisted(string token)
        {
            if (_cache.TryGetValue(token, out _))
            {
                return true;
            }

            return false;
        }

        public void AddTokenToBlacklist(string token, DateTimeOffset expirationTime)
        {
            _cache.Set(token, "", expirationTime);
        }
    }
}
