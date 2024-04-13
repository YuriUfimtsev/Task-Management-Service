using HomeworkApp.Bll.Services;
using HomeworkApp.Bll.Services.Interfaces;
using HomeworkApp.Dal.Infrastructure;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Settings;
using Moq;
using StackExchange.Redis;

namespace HomeworkApp.UnitTests.Fixtures;

public class TestFixture
{
    public TestFixture()
    {
        RateLimiterRepositoryFake = new();
        RepositoryDatabaseFake = new();

        RepositoryDatabaseFake
            .Setup(fake => fake.KeyExists(
                It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(true);

        RateLimiterRepositoryFake
            .Setup(fake => fake.GetConnection())
            .ReturnsAsync(RepositoryDatabaseFake.Object);
        
        RateLimiterService = new RateLimiterService(RateLimiterRepositoryFake.Object);
    }
    
    public IRateLimiterService RateLimiterService { get; }
    
    public Mock<IRateLimiterRepository> RateLimiterRepositoryFake { get; }

    public Mock<IDatabase> RepositoryDatabaseFake { get; }
}