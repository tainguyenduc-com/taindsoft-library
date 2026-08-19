using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using TaindSoft.Core.Dtos;
using TaindSoft.Core.HttpApi.Endpoints;

namespace TaindSoft.Core.HttpApi
{
    /// <summary>
    /// Extension methods for configuring HTTP services and middleware
    /// </summary>
    public static class HttpApiServiceExtensions
    {
        // Track globally-mapped endpoint types to avoid duplicate registrations
        // when MapCoreEndpoints is invoked multiple times during app composition.
        private static readonly ConcurrentDictionary<string, byte> s_mappedEndpointTypes = new();

        /// <summary>
        /// Add core HTTP services including endpoint discovery and registration
        /// </summary>
        public static IServiceCollection AddCoreHttpServices(
            this IServiceCollection services,
            Assembly? assembly = null)
        {
            Assembly targetAssembly = assembly ?? Assembly.GetCallingAssembly();

            // Auto-discover and register endpoints
            List<Type> endpointTypes = [.. targetAssembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface &&
                       typeof(IEndpoint).IsAssignableFrom(t))];

            foreach (Type? endpointType in endpointTypes)
            {
                // Prevent duplicate registrations across multiple AddEndpoints/AddCoreHttpServices calls
                services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IEndpoint), endpointType));
            }

            return services;
        }

        /// <summary>
        /// Map all discovered endpoints to the application pipeline
        /// </summary>
        public static IEndpointRouteBuilder MapCoreEndpoints(
            this IEndpointRouteBuilder app)
        {
            using IServiceScope scope = app.ServiceProvider.CreateScope();
            IEnumerable<IEndpoint> endpoints = scope.ServiceProvider.GetServices<IEndpoint>();

            var loggerFactory = scope.ServiceProvider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            var logger = loggerFactory?.CreateLogger("MapCoreEndpoints");

            try
            {
                foreach (IEndpoint endpoint in endpoints)
                {
                    string typeId = endpoint.GetType().FullName ?? endpoint.GetType().Name;
                    if (!s_mappedEndpointTypes.TryAdd(typeId, 0))
                    {
                        continue;
                    }

                    try
                    {
                        // Map directly; routes must include full prefix (e.g., /api/{module}/v1/...)
                        endpoint.MapEndpoint(app);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to map endpoint {TypeId}", typeId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error while mapping core endpoints");
            }

            return app;
        }

        /// <summary>
        /// Add global exception handling middleware
        /// </summary>
        public static IApplicationBuilder AddGlobalExceptionHandler(
            this IApplicationBuilder app,
            bool isDevelopment = false)
        {
            _ = app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    IExceptionHandlerPathFeature? exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    Exception? exception = exceptionHandlerPathFeature?.Error;

                    context.Response.ContentType = "application/json";

                    ApiResponse<object> response = CreateErrorResponse(exception, isDevelopment);

                    context.Response.StatusCode = GetStatusCode(exception);

                    await context.Response.WriteAsJsonAsync(response);
                });
            });

            return app;
        }

        private static ApiResponse<object> CreateErrorResponse(Exception? exception, bool isDevelopment)
        {
            string message = exception?.Message ?? "An unexpected error occurred.";
            string code = GetErrorCode(exception);

            ErrorDetails errorDetails = new()
            {
                Code = code,
                Description = message
            };

            if (isDevelopment && exception != null)
            {
                errorDetails.StackTrace = exception.StackTrace;
            }

            return new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Error = errorDetails
            };
        }

        private static int GetStatusCode(Exception? exception)
        {
            return exception switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                EntityNotFoundException => StatusCodes.Status404NotFound,
                BusinessRuleViolatedException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
        }

        private static string GetErrorCode(Exception? exception)
        {
            return exception?.GetType().Name ?? ErrorCodes.InternalServerError;
        }
    }
}
