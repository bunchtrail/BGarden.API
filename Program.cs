using Application;
using BGarden.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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

            // Добавим Swagger для удобства тестирования
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Подключаем контроллеры (MVC)
            builder.Services.AddControllers();

            var app = builder.Build();

            // Включаем Swagger в Dev-режиме
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
