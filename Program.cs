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

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Подключаем конфигурацию из appsettings.json
            var configuration = builder.Configuration;

            // Сначала регистрируем инфраструктуру
            builder.Services.AddInfrastructure(configuration);

            // Затем регистрируем application слой
            builder.Services.AddApplication();
            
            // Регистрируем собственный JwtService из API.Interfaces
            builder.Services.AddScoped<BGarden.API.Interfaces.IJwtService, JwtService>();
            
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
                        Console.WriteLine($"Получен запрос с токеном: {context.Request.Headers["Authorization"]}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("JWT токен успешно валидирован");
                        var token = context.SecurityToken as JwtSecurityToken;
                        if (token != null)
                        {
                            Console.WriteLine($"Алгоритм: {token.Header.Alg}, KeyId: {token.Header.Kid}");
                            Console.WriteLine($"Издатель: {token.Issuer}, Аудитория: {token.Audiences.FirstOrDefault()}");
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Ошибка аутентификации: {context.Exception.Message}");
                        Console.WriteLine($"Ошибка аутентификации (детали): {context.Exception}");
                        if (context.Exception.InnerException != null)
                        {
                            Console.WriteLine($"Внутренняя ошибка: {context.Exception.InnerException.Message}");
                        }
                        
                        // Анализируем токен, который не прошел валидацию
                        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        if (!string.IsNullOrEmpty(token))
                        {
                            try 
                            {
                                var parts = token.Split('.');
                                if (parts.Length >= 2)
                                {
                                    var headerBase64 = parts[0];
                                    var headerBytes = Convert.FromBase64String(headerBase64.PadRight(headerBase64.Length + (4 - headerBase64.Length % 4) % 4, '='));
                                    var headerJson = Encoding.UTF8.GetString(headerBytes);
                                    Console.WriteLine($"Заголовок невалидного токена: {headerJson}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка при анализе токена: {ex.Message}");
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Вызов Challenge. Аутентификация не прошла: {context.Error}, {context.ErrorDescription}");
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
                        builder.WithOrigins(
                                "http://localhost:5173",  // Существующий URL
                                "http://localhost:3000",  // URL вашего фронтенда
                                "http://localhost:3001",  // Дополнительный URL для разработки
                                "http://127.0.0.1:3000",  // localhost и 127.0.0.1 обрабатываются как разные домены
                                "http://127.0.0.1:3001"
                            )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials() // Важно для передачи куки
                            .SetIsOriginAllowed(origin => true); // Разрешаем любые домены в режиме разработки
                    });
            });

            var app = builder.Build();

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

            // Важно! Аутентификация должна быть перед авторизацией
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
