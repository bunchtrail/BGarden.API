using System.Net;
using System.Text.Json;
using BGarden.API.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BGarden.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необработанное исключение");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Внутренняя ошибка сервера";

            if (exception is ResourceNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
            }
            else if (exception is BusinessRuleException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
            }

            var result = JsonSerializer.Serialize(new ErrorDetails
            {
                StatusCode = (int)statusCode,
                Message = message
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            
            return context.Response.WriteAsync(result);
        }
    }

    public class ErrorDetails
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 