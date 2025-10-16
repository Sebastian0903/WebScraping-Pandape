using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Middleware
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
                _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Se ha producido un error interno en el servidor.";

            if (exception is KeyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = "El recurso solicitado no fue encontrado.";
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                message = "No está autorizado para acceder a este recurso.";
            }
            else if (exception is ArgumentException || exception is FluentValidation.ValidationException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
            }

            var response = new
            {
                status = (int)statusCode,
                message = message,
                detalle = exception.Message,
                timestamp = DateTime.UtcNow
            };

            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}