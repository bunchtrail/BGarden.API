using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BGarden.API.Middleware
{
    /// <summary>
    /// Промежуточное ПО для добавления заголовков безопасности
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Добавляем заголовки безопасности
            
            // X-Content-Type-Options - предотвращает MIME sniffing
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            
            // X-Frame-Options - защита от clickjacking
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            
            // X-XSS-Protection - дополнительная защита от XSS
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            // Content-Security-Policy - ограничение источников контента
            context.Response.Headers.Add(
                "Content-Security-Policy",
                "default-src 'self'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "connect-src 'self'"
            );
            
            // Referrer-Policy - ограничение информации в заголовке Referer
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Strict-Transport-Security - принудительное использование HTTPS
            // Для продакшн среды
            if (!string.IsNullOrEmpty(context.Request.Host.Host) && 
                !context.Request.Host.Host.Contains("localhost"))
            {
                context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            await _next(context);
        }
    }
} 