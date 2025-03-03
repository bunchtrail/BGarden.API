using Microsoft.AspNetCore.Builder;

namespace BGarden.API.Middleware
{
    /// <summary>
    /// Расширение для подключения промежуточного ПО безопасности
    /// </summary>
    public static class SecurityHeadersExtension
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
} 