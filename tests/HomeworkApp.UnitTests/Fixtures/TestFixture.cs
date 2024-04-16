using HomeworkApp.Bll.Services;
using HomeworkApp.Bll.Services.Interfaces;
using HomeworkApp.Dal.Repositories.Interfaces;
using Moq;

namespace HomeworkApp.UnitTests.Fixtures;

public class TestFixture
{
    public TestFixture()
    {
        RateLimiterRepositoryFake = new();
        RateLimiterService = new RateLimiterService(RateLimiterRepositoryFake.Object);
    }
    
    public IRateLimiterService RateLimiterService { get; }
    
    public Mock<IRateLimiterRepository> RateLimiterRepositoryFake { get; }
}