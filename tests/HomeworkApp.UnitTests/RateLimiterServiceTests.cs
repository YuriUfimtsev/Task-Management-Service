using HomeworkApp.Bll;
using HomeworkApp.UnitTests.Fixtures;
using Moq;

namespace HomeworkApp.UnitTests;

public class RateLimiterServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _testFixture;
    
    public RateLimiterServiceTests(TestFixture fixture)
    {
        _testFixture = fixture;
    }
    
    [Fact]
    public async Task ThrowIfTooManyRequests_GetFiveActualRequestsScoreFromRedis_ShouldSuccess()
    {
        // Arrange
        var clientIP = "8.8.8.8";
        
        _testFixture.RateLimiterRepositoryFake
            .Setup(fake => fake.GetActualRequestsScore(clientIP))
            .ReturnsAsync(5);

        // Act && Assert
        await _testFixture.RateLimiterService.ThrowIfTooManyRequests(clientIP);
    }
    
    [Fact]
    public async Task
        ThrowIfTooManyRequests_GetMinusOneActualRequestsScoreFromRedis_ShouldThrowRequestLimitExceededException()
    {
        // Arrange
        var clientIP = "8.8.8.8";
        
        _testFixture.RateLimiterRepositoryFake
            .Setup(fake => fake.GetActualRequestsScore(clientIP))
            .ReturnsAsync(-1);

        // Act && Assert
        await Assert.ThrowsAsync<RequestLimitExceeded>(
            () => _testFixture.RateLimiterService.ThrowIfTooManyRequests(clientIP));
    }
}