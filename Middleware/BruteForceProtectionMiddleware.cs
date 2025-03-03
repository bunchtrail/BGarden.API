using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BGarden.API.Middleware
{
    /// <summary>
    /// Промежуточное ПО для защиты от брутфорс-атак
    /// </summary>
    public class BruteForceProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BruteForceProtectionMiddleware> _logger;
        private readonly int _maxRequestsPerIp;
        private readonly TimeSpan _blockDuration;
        private readonly string[] _protectedRoutes;

        public BruteForceProtectionMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<BruteForceProtectionMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            
            // Загружаем конфигурацию из настроек
            _maxRequestsPerIp = configuration.GetValue<int>("SecuritySettings:MaxRequestsPerMinute", 60);
            _blockDuration = TimeSpan.FromMinutes(
                configuration.GetValue<int>("SecuritySettings:BlockDurationMinutes", 15));
            
            // Защищаемые маршруты (пути)
            _protectedRoutes = new[]
            {
                "/api/auth/login",
                "/api/auth/verify-2fa",
                "/api/auth/refresh-token"
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestPath = context.Request.Path.Value?.ToLowerInvariant();
            
            // Проверяем только защищаемые маршруты
            if (requestPath != null && IsProtectedRoute(requestPath))
            {
                var ipAddress = GetIpAddress(context);
                var cacheKey = $"brute_force_{ipAddress}_{requestPath}";
                var blockedKey = $"blocked_{ipAddress}_{requestPath}";

                // Проверяем, не заблокирован ли IP
                if (_cache.TryGetValue(blockedKey, out _))
                {
                    _logger.LogWarning("Заблокирован запрос от {IpAddress} к {Path}", ipAddress, requestPath);
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                    return;
                }

                // Увеличиваем счетчик запросов
                int requestCount = 1;
                if (_cache.TryGetValue(cacheKey, out int currentCount))
                {
                    requestCount = currentCount + 1;
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                _cache.Set(cacheKey, requestCount, cacheEntryOptions);

                // Если превышен лимит запросов, блокируем IP
                if (requestCount > _maxRequestsPerIp)
                {
                    _logger.LogWarning("Превышен лимит запросов от {IpAddress} к {Path}. Блокировка на {Duration} минут", 
                        ipAddress, requestPath, _blockDuration.TotalMinutes);
                    
                    var blockOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(_blockDuration);
                    _cache.Set(blockedKey, true, blockOptions);
                    
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                    return;
                }
            }

            await _next(context);
        }

        private bool IsProtectedRoute(string path)
        {
            foreach (var route in _protectedRoutes)
            {
                if (path.StartsWith(route, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private string GetIpAddress(HttpContext context)
        {
            // Получаем IP-адрес из X-Forwarded-For или из соединения
            string ipAddress = context.Request.Headers["X-Forwarded-For"].ToString();
            
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            }
            
            return ipAddress;
        }
    }
} 