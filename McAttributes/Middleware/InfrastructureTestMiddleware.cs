using McAttributes.Pages;

namespace McAttributes.Middleware
{
    public class InfrastructureTestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InfrastructureTestMiddleware> _logger;

        public InfrastructureTestMiddleware(RequestDelegate next, ILogger<InfrastructureTestMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if all requests are frozen
            if (InfrastructureTestHelper.IsFrozenAll)
            {
                _logger.LogWarning($"Request frozen: {context.Request.Path}");
                // Hang indefinitely (well, until the freeze expires)
                while (InfrastructureTestHelper.IsFrozenAll)
                {
                    await Task.Delay(1000);
                }
            }

            // Check if health endpoints are frozen
            var isHealthEndpoint = context.Request.Path.StartsWithSegments("/health");
            if (isHealthEndpoint && InfrastructureTestHelper.IsFrozenHealth)
            {
                _logger.LogWarning($"Health endpoint frozen: {context.Request.Path}");
                // Hang indefinitely (well, until the freeze expires)
                while (InfrastructureTestHelper.IsFrozenHealth)
                {
                    await Task.Delay(1000);
                }
            }

            // Apply slow response delay
            if (InfrastructureTestHelper.SlowResponseMs > 0)
            {
                _logger.LogDebug($"Applying {InfrastructureTestHelper.SlowResponseMs}ms delay to {context.Request.Path}");
                await InfrastructureTestHelper.ApplyDelayIfNeeded();
            }

            await _next(context);
        }
    }

    public static class InfrastructureTestMiddlewareExtensions
    {
        public static IApplicationBuilder UseInfrastructureTestMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<InfrastructureTestMiddleware>();
        }
    }
}
