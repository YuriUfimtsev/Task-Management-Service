using HomeworkApp.Bll;
using HomeworkApp.UnitTests.Fixtures;
using Moq;
using StackExchange.Redis;

namespace HomeworkApp.UnitTests;

public class RateLimiterServiceTests : IClassFixture<TestFixture>
{
    private TestFixture _testFixture;
    
    public RateLimiterServiceTests(TestFixture fixture)
    {
        _testFixture = fixture;
    }
    
    [Fact]
    public async Task ThrowIfTooManyRequests_GetFiveActualRequestsScoreFromRedis_ShouldSuccess()
    {
        // Arrange
        var clientIp = "8.8.8.8";
        var key = $"rate-limit:{clientIp}";
        
        _testFixture.RepositoryDatabaseFake
            .Setup(fake => fake.StringDecrement(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, long, CommandFlags>((actualKey, _, _) =>
            {
                if (actualKey != key)
                {
                    Assert.Fail($"Expected key: {key}. Actual key: {actualKey}");
                }
            })
            .Returns(5);

        // Act && Assert
        await _testFixture.RateLimiterService.ThrowIfTooManyRequests(clientIp);
    }
    
    [Fact]
    public async Task
        ThrowIfTooManyRequests_GetMinusOneActualRequestsScoreFromRedis_ShouldThrowRequestLimitExceededException()
    {
        // Arrange
        var clientIp = "8.8.8.8";
        var key = $"rate-limit:{clientIp}";
        
        _testFixture.RepositoryDatabaseFake
            .Setup(fake => fake.StringDecrement(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, long, CommandFlags>((actualKey, _, _) =>
            {
                if (actualKey != key)
                {
                    Assert.Fail($"Expected key: {key}. Actual key: {actualKey}");
                }
            })
            .Returns(-1);

        // Act && Assert
        await Assert.ThrowsAsync<RequestLimitExceeded>(
            () => _testFixture.RateLimiterService.ThrowIfTooManyRequests(clientIp));
    }
}