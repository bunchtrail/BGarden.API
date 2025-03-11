using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

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
            "/maps/", // Статические файлы карт
            "/api/map", // Эндпоинты карт
            "/api/map/", 
            "/api/map/active", 
            "/api/map/all",
            "/favicon.ico",
            "/images/",
            "/logo"
        };
        
        // Пути, для которых выводятся подробные логи
        private static readonly string[] _verboseLogPaths = new[]
        {
            "/api/auth/",
            "/api/user/register",
            "/api/user/login"
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
                
                // Проверяем, является ли запрос к статическому файлу
                bool isStaticFile = path != null && (
                    Path.HasExtension(path) || // Имеет расширение файла (.jpg, .png, .css, .js и т.д.)
                    path.Contains("/maps/") ||
                    path.Contains("/images/") ||
                    path.StartsWith("/api/map") // Все запросы к API карт
                );
                
                // Определяем, нужно ли выводить подробные логи для этого пути
                bool isVerboseLoggingEnabled = _verboseLogPaths.Any(p => path != null && path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
                
                // Если путь публичный или это статический файл, пропускаем проверку заголовка Authorization
                if (isPublicPath || isStaticFile)
                {
                    // Выводим минимальный лог только если включено подробное логирование для этого пути
                    if (isVerboseLoggingEnabled)
                    {
                        _logger.LogInformation("JwtTokenHandlerMiddleware: Пропуск проверки для публичного пути: {Path}", path);
                    }
                    await _next(context);
                    return;
                }
                
                // Получаем заголовок Authorization
                var authHeader = context.Request.Headers["Authorization"].ToString();
                
                // Добавляем отладочную информацию только для API запросов (не для статических файлов)
                // и только если включено подробное логирование для этого пути
                if (path != null && path.StartsWith("/api") && isVerboseLoggingEnabled)
                {
                    // Не логируем содержимое заголовка Authorization, а только факт его наличия
                    _logger.LogInformation("JwtTokenHandlerMiddleware: Обработка запроса API: {Path}", path);
                }

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
                        
                        // Не логируем полное содержимое токена
                        _logger.LogInformation("JwtTokenHandlerMiddleware: Заголовок Authorization изменен с добавлением префикса Bearer");
                    }
                    else if (isVerboseLoggingEnabled)
                    {
                        _logger.LogWarning("JwtTokenHandlerMiddleware: Заголовок Authorization не похож на JWT токен по регулярному выражению");
                    }
                }
                else if (!string.IsNullOrEmpty(authHeader) && isVerboseLoggingEnabled)
                {
                    _logger.LogInformation("JwtTokenHandlerMiddleware: Заголовок уже содержит префикс Bearer или имеет другой формат");
                }
                else if (isVerboseLoggingEnabled)
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