using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Settings;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HomeworkApp.Dal.Repositories;

public class RateLimiterRepository : RedisRepository, IRateLimiterRepository
{
    public RateLimiterRepository(IOptions<DalOptions> dalSettings) : base(dalSettings.Value)
    {
    }

    protected override TimeSpan KeyTtl => TimeSpan.FromMinutes(1);
    
    protected override string KeyPrefix => "rate-limit";

    public async Task<long> GetActualRequestsScore(string clientIdentifier)
    {
        var database = await base.GetConnection();

        var key = GetKey(clientIdentifier);
        if (!database.KeyExists(key))
        {
            database.StringSet(
                key,
                0,
                KeyTtl,
                When.NotExists);
        }

        return database.StringIncrement(key);
    }
}