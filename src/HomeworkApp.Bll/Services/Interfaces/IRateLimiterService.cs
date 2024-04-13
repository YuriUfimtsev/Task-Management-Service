namespace HomeworkApp.Bll.Services.Interfaces;

public interface IRateLimiterService
{
    public Task ThrowIfTooManyRequests(string clientIP);
}