using Grpc.Core;
using Grpc.Core.Interceptors;
using HomeworkApp.Bll.Services.Interfaces;

namespace HomeworkApp.Interceptors;

public class RateLimitingInterceptor : Interceptor
{
    private readonly IRateLimiterService _rateLimiterService;

    public RateLimitingInterceptor(IRateLimiterService rateLimiterService)
    {
        _rateLimiterService = rateLimiterService;
    }
    
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var clientIPEntry = context.RequestHeaders.Get("X-R256-USER-IP");
        if (clientIPEntry is null)
        {
            throw new InvalidOperationException(
                "Please set IP in the X-R256-USER-IP header to send requests");
        }
        
        await _rateLimiterService.ThrowIfTooManyRequests(clientIPEntry.Value);
        return await continuation(request, context);
    }
}