using Microsoft.AspNetCore.Http;
using System.Text.Json;
using TaindSoft.Core.Dtos;

namespace TaindSoft.Core.Host.Middleware
{
    /// <summary>
    /// Middleware to handle validation exceptions and convert them to appropriate HTTP responses
    /// </summary>
    public class ValidationExceptionHandlingMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                JsonSerializerOptions _jsonSerializerOptions = context.RequestServices.GetService(typeof(JsonSerializerOptions)) as JsonSerializerOptions
                    ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                await HandleValidationExceptionAsync(context, ex, _jsonSerializerOptions);
            }
        }

        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception, JsonSerializerOptions jsonSerializerOptions)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            ErrorDetails errorDetails = new()
            {
                Code = ErrorCodes.ValidationFailed,
                Description = exception.Message,
                ValidationErrors = exception.Errors
            };

            ApiResponse response = ApiResponse.Failure(
                "Validation failed",
                errorDetails,
                ErrorCodes.ValidationFailed);

            string json = JsonSerializer.Serialize(response, jsonSerializerOptions);

            return context.Response.WriteAsync(json);
        }
    }
}
