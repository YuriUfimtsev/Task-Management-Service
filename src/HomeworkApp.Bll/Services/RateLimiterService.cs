using HomeworkApp.Bll.Services.Interfaces;
using HomeworkApp.Dal.Repositories.Interfaces;
using StackExchange.Redis;

namespace HomeworkApp.Bll.Services;

public class RateLimiterService : IRateLimiterService
{
    private readonly IRateLimiterRepository _rateLimiterRepository;
    
    public RateLimiterService(IRateLimiterRepository rateLimiterRepository)
    {
        _rateLimiterRepository = rateLimiterRepository;
    }
    
    public async Task ThrowIfTooManyRequests(string clientIP)
    {
        var actualRequestsScore = await _rateLimiterRepository.GetActualRequestsScore(clientIP);

        if (actualRequestsScore < 0)
        {
            throw new RequestLimitExceeded(
                "The number of requests per minute has been exceeded." +
                " Please wait a minute before the next request.");
        }
    }
}