using Microsoft.AspNetCore.Builder;

namespace BGarden.API.Middleware
{
    /// <summary>
    /// Расширение для подключения защиты от брутфорс-атак
    /// </summary>
    public static class BruteForceProtectionExtension
    {
        public static IApplicationBuilder UseBruteForceProtection(this IApplicationBuilder app)
        {
            return app.UseMiddleware<BruteForceProtectionMiddleware>();
        }
    }
} 