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

    protected override string? KeyPrefix { get; }

    public new async Task<IDatabase> GetConnection()
        => await base.GetConnection();
}