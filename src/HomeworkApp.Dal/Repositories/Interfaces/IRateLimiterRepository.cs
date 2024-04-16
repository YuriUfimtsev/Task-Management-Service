using StackExchange.Redis;

namespace HomeworkApp.Dal.Repositories.Interfaces;

public interface IRateLimiterRepository
{
    public Task<long> GetActualRequestsScore(string clientIdentifier);
}