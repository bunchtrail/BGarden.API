using Application;
using BGarden.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using BGarden.API.Middleware;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BGarden.API.Services;
using BGarden.API.Interfaces;
using BGarden.API.Adapters;
using BGarden.Application;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;
using BGarden.Application.Interfaces;
using BGarden.Domain.Interfaces;
using Application.Interfaces;
using BGarden.Application.Services;
using BGarden.Infrastructure.Services;
using Application.Services;
using BGarden.API.Exceptions;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Настройка Kestrel для прослушивания всех IP-адресов
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                // Используем порт 7254 (из существующей конфигурации) и прослушиваем на всех IP-адресах
                serverOptions.Listen(IPAddress.Any, 7254);
            });

            // Подключаем конфигурацию из appsettings.json
            var configuration = builder.Configuration;

            // Сначала регистрируем инфраструктуру
            builder.Services.AddInfrastructure(configuration);

            // Затем регистрируем application слой
            builder.Services.AddApplication();

            // Register IHttpContextAccessor to allow access to HttpContext in services
            builder.Services.AddHttpContextAccessor();

            // Регистрируем собственный JwtService из API.Interfaces
            builder.Services.AddScoped<BGarden.API.Interfaces.IJwtService, JwtService>();

            // Добавляем настройки логирования
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            // Устанавливаем уровень логирования для разных категорий
            builder.Logging.AddFilter("BGarden.API.Middleware.JwtTokenHandlerMiddleware", LogLevel.Error); // Только ошибки для jwt middleware
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
            builder.Logging.AddFilter("System", LogLevel.Warning);
            builder.Logging.AddFilter("Default", LogLevel.Information);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Warning); // Снижаем уровень логирования для аутентификации
            builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication.JwtBearer", LogLevel.Error); // Только ошибки для JWT
            builder.Logging.AddFilter("System.Security.Claims", LogLevel.Error); // Снижаем уровень логирования для Claims
            builder.Logging.AddFilter("System.IdentityModel.Tokens.Jwt", LogLevel.Error); // Снижаем уровень логирования для JWT

            // Дополнительно отключаем логирование для часто используемых контроллеров
            builder.Logging.AddFilter("BGarden.API.Controllers.MapController", LogLevel.Warning);
            builder.Logging.AddFilter("BGarden.API.Controllers.UserController", LogLevel.Warning);

            // Регистрируем адаптер для совместимости с Domain.Interfaces.IJwtService
            builder.Services.AddScoped<BGarden.Domain.Interfaces.IJwtService, JwtServiceAdapter>();

            // Добавляем встроенный кэш в память для защиты от брутфорса
            builder.Services.AddMemoryCache();

            // Конфигурируем JWT аутентификацию
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // В продакшн установить true
                options.SaveToken = true;
                options.UseSecurityTokenValidators = true; // Добавляем для совместимости с .NET 8
                // Добавляем дополнительную отладочную информацию
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Исключаем статические файлы и часто запрашиваемые ресурсы из логирования
                        var path = context.Request.Path.Value?.ToLowerInvariant();
                        if (path == null ||
                            path.Contains("/maps/") ||
                            path.Contains("/images/") ||
                            Path.HasExtension(path) ||
                            path == "/api/Map/active" ||
                            path == "/api/Map" ||
                            path == "/api/Map/all" ||
                            path == "/api/User/me")
                        {
                            return Task.CompletedTask;
                        }

                        // Выводим лог только для остальных API запросов
                        if (path.StartsWith("/api/"))
                        {
                            // Убираем вывод полного токена из соображений безопасности
                            Console.WriteLine($"Получен запрос авторизации");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Исключаем статические файлы и часто запрашиваемые ресурсы из логирования
                        var path = context.Request.Path.Value?.ToLowerInvariant();
                        if (path == null ||
                            path.Contains("/maps/") ||
                            path.Contains("/images/") ||
                            Path.HasExtension(path) ||
                            path == "/api/Map/active" ||
                            path == "/api/Map" ||
                            path == "/api/Map/all" ||
                            path == "/api/User/me")
                        {
                            return Task.CompletedTask;
                        }

                        // Вместо вывода полной информации о токене только сообщаем об успешной валидации
                        if (path.StartsWith("/api/"))
                        {
                            // Убираем детальное логирование токена и claims
                            Console.WriteLine("Аутентификация пройдена");
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Исключаем статические файлы и часто запрашиваемые ресурсы из логирования
                        var path = context.Request.Path.Value?.ToLowerInvariant();
                        if (path == null ||
                            path.Contains("/maps/") ||
                            path.Contains("/images/") ||
                            Path.HasExtension(path) ||
                            path == "/api/Map/active" ||
                            path == "/api/Map" ||
                            path == "/api/Map/all" ||
                            path == "/api/User/me")
                        {
                            return Task.CompletedTask;
                        }

                        // Выводим лог только для остальных API запросов
                        if (path.StartsWith("/api/"))
                        {
                            Console.WriteLine($"Ошибка аутентификации: {context.Exception.Message}");
                            // Выводим уточненную причину ошибки
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                Console.WriteLine("Токен истек");
                            }
                            else if (context.Exception.GetType() == typeof(SecurityTokenValidationException))
                            {
                                Console.WriteLine("Токен не прошел валидацию");
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // Исключаем статические файлы и часто запрашиваемые ресурсы из логирования
                        var path = context.Request.Path.Value?.ToLowerInvariant();
                        if (path == null ||
                            path.Contains("/maps/") ||
                            path.Contains("/images/") ||
                            Path.HasExtension(path) ||
                            path == "/api/Map/active" ||
                            path == "/api/Map" ||
                            path == "/api/Map/all" ||
                            path == "/api/User/me")
                        {
                            return Task.CompletedTask;
                        }

                        // Выводим лог только для остальных API запросов
                        if (path.StartsWith("/api/"))
                        {
                            Console.WriteLine($"Вызов Challenge. Аутентификация не прошла: {context.Error}, {context.ErrorDescription}");
                        }
                        return Task.CompletedTask;
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    {
                        // Явно устанавливаем kid для ключа
                        KeyId = "BGardenSigningKey"
                    },
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    NameClaimType = ClaimTypes.Name, // Указываем, какой claim использовать для Name
                    RoleClaimType = ClaimTypes.Role, // Указываем, какой claim использовать для Role
                    ClockSkew = TimeSpan.Zero,
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateActor = false,
                    ValidateTokenReplay = false
                };
            });

            // Добавим Swagger для удобства тестирования с поддержкой JWT
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BGarden API", Version = "v1" });

                // Настройка Swagger для работы с JWT
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Подключаем контроллеры (MVC)
            builder.Services.AddControllers();

            // Настройка CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins",
    builder =>
    {
        builder
            .SetIsOriginAllowed(origin => true) // разрешает все источники
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

            });

            // Регистрация сервисов
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ISpecimenService, SpecimenService>();
            builder.Services.AddScoped<IFamilyService, FamilyService>();
            builder.Services.AddScoped<IExpositionService, ExpositionService>();
            builder.Services.AddScoped<IRegionService, RegionService>();
            builder.Services.AddScoped<IPhenologyService, PhenologyService>();
            builder.Services.AddScoped<IBiometryService, BiometryService>();
            builder.Services.AddScoped<IMapService, MapService>();
            builder.Services.AddScoped<ISpecimenImageService, SpecimenImageService>();

            // Регистрация Use Cases
            builder.Services.AddScoped<Application.UseCases.CreateSpecimenWithImagesUseCase>();

            var app = builder.Build();
            
            // Регистрация middleware обработки исключений
            // ВАЖНО: Должен быть первым в цепочке middleware
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            
            app.UseCors();
            // Выводим информацию о конфигурации JWT для отладки
            Console.WriteLine("=== Конфигурация JWT ===");
            Console.WriteLine($"Issuer: {jwtSettings["Issuer"]}");
            Console.WriteLine($"Audience: {jwtSettings["Audience"]}");
            Console.WriteLine($"ExpiryMinutes: {jwtSettings["AccessTokenExpiryMinutes"]}");
            Console.WriteLine($"Secret Key Length: {jwtSettings["SecretKey"]?.Length ?? 0} символов");
            Console.WriteLine($"Алгоритм подписи: HmacSha256");
            Console.WriteLine($"Кодировка ключа: UTF8");
            Console.WriteLine("=======================");

            // Включаем Swagger в Dev-режиме
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BGarden API v1"));
            }

            // Убираем перенаправление на HTTPS для разработки
            // app.UseHttpsRedirection();

            // Подключаем middleware безопасности
            app.UseSecurityHeaders();
            app.UseBruteForceProtection();

            // Подключаем наш JwtTokenHandler middleware
            app.UseJwtTokenHandler();

            // Подключаем CORS
            app.UseCors("AllowSpecificOrigins");

            // Подключаем обслуживание статических файлов
            app.UseStaticFiles();

            // Важно! Аутентификация должна быть перед авторизацией
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
