using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace BGarden.API.Middleware
{
    /// <summary>
    /// Промежуточное ПО для обработки JWT токенов
    /// </summary>
    public class JwtTokenHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtTokenHandlerMiddleware> _logger;
        // Регулярное выражение для проверки формата JWT
        private static readonly Regex JwtRegex = new Regex(@"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$", RegexOptions.Compiled);
        // Пути, которые не требуют авторизации
        private static readonly string[] _publicPaths = new[] 
        { 
            "/api/auth/register", 
            "/api/auth/login",
            "/api/map/options/default",
            "/api/map/markers",
            "/api/map/areas"
        };

        public JwtTokenHandlerMiddleware(
            RequestDelegate next,
            ILogger<JwtTokenHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Получаем путь запроса в нижнем регистре для сравнения
                var path = context.Request.Path.Value?.ToLowerInvariant();
                
                // Проверяем, является ли путь публичным (не требующим авторизации)
                bool isPublicPath = _publicPaths.Any(p => path != null && path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
                
                // Если путь публичный, пропускаем проверку заголовка Authorization
                if (isPublicPath)
                {
                    _logger.LogInformation("JwtTokenHandlerMiddleware: Пропуск проверки для публичного пути: {Path}", path);
                    await _next(context);
                    return;
                }
                
                // Получаем заголовок Authorization
                var authHeader = context.Request.Headers["Authorization"].ToString();
                
                // Добавляем отладочную информацию
                _logger.LogInformation("JwtTokenHandlerMiddleware: Обработка заголовка Authorization: '{AuthHeader}'", authHeader);
                _logger.LogInformation("JwtTokenHandlerMiddleware: URL запроса: {Path}", context.Request.Path.Value);

                // Если заголовок не пустой и не начинается с "Bearer "
                if (!string.IsNullOrEmpty(authHeader) && !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    // Проверяем, похож ли он на JWT токен (имеет формат xxx.yyy.zzz) с помощью регулярного выражения
                    if (JwtRegex.IsMatch(authHeader))
                    {
                        _logger.LogInformation("JwtTokenHandlerMiddleware: Обнаружен JWT токен без префикса Bearer, добавляем префикс");
                        
                        // Добавляем префикс "Bearer " к токену
                        context.Request.Headers.Remove("Authorization");
                        context.Request.Headers.Add("Authorization", $"Bearer {authHeader}");
                        
                        _logger.LogInformation("JwtTokenHandlerMiddleware: Заголовок Authorization изменен на: '{NewAuthHeader}'", $"Bearer {authHeader}");
                    }
                    else
                    {
                        _logger.LogWarning("JwtTokenHandlerMiddleware: Заголовок Authorization не похож на JWT токен по регулярному выражению");
                    }
                }
                else if (!string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogInformation("JwtTokenHandlerMiddleware: Заголовок уже содержит префикс Bearer или имеет другой формат");
                }
                else
                {
                    _logger.LogWarning("JwtTokenHandlerMiddleware: Заголовок Authorization отсутствует");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JwtTokenHandlerMiddleware: Ошибка при обработке заголовка Authorization");
            }

            // Продолжаем обработку запроса
            await _next(context);
        }
    }
} 