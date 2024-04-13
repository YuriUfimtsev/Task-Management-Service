using HomeworkApp.Bll.Services.Interfaces;
using HomeworkApp.Dal.Repositories.Interfaces;
using StackExchange.Redis;

namespace HomeworkApp.Bll.Services;

public class RateLimiterService : IRateLimiterService
{
    private readonly IRateLimiterRepository _rateLimiterRepository;
    
    private const int RequestPerMinute = 100;
    private readonly TimeSpan _keyTtl = TimeSpan.FromMinutes(1);
    
    public RateLimiterService(IRateLimiterRepository rateLimiterRepository)
    {
        _rateLimiterRepository = rateLimiterRepository;
    }
    
    public async Task ThrowIfTooManyRequests(string clientIP)
    {
        var key = $"rate-limit:{clientIP}";
        var database = await _rateLimiterRepository.GetConnection();

        if (!database.KeyExists(key))
        {
            database.StringSet(
                key,
                RequestPerMinute,
                _keyTtl,
                When.NotExists);
        }

        var actualRequestsScore = database.StringDecrement(key);

        if (actualRequestsScore < 0)
        {
            throw new RequestLimitExceeded(
                "The number of requests per minute has been exceeded." +
                " Please wait a minute before the next request.");
        }
    }
}