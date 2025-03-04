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
                // Добавляем дополнительную отладочную информацию
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("JWT токен успешно валидирован");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Ошибка аутентификации: {context.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    NameClaimType = ClaimTypes.Name, // Указываем, какой claim использовать для Name
                    RoleClaimType = ClaimTypes.Role, // Указываем, какой claim использовать для Role
                    ClockSkew = TimeSpan.Zero
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
