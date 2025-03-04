using Microsoft.AspNetCore.Builder;

namespace BGarden.API.Middleware
{
    /// <summary>
    /// Расширение для регистрации middleware обработки JWT токенов
    /// </summary>
    public static class JwtTokenHandlerExtension
    {
        public static IApplicationBuilder UseJwtTokenHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtTokenHandlerMiddleware>();
        }
    }
} 