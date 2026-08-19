using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Reflection;
using TaindSoft.Core.Application.CQRS;
using TaindSoft.Core.Dtos;
using TaindSoft.Core.PermissionCheckers;

namespace TaindSoft.Core.HttpApi.Endpoints
{
    public abstract class BaseEndpoint<TKey, TRequest> : AbstractEndpoint
    {
        protected abstract Task<IResult> HandleAsync(TKey key, TRequest request, ICQRSManager cqrsManager, HttpContext httpContext, CancellationToken cancellationToken);

        protected Task<IResult> ExecuteAsync(TKey key, TRequest request, ICQRSManager cqrsManager, HttpContext httpContext, CancellationToken cancellationToken)
        {
            return ExecuteWithWorkflowAsync(httpContext, cancellationToken, () => HandleAsync(key, request, cqrsManager, httpContext, cancellationToken));
        }

        protected override RouteHandlerBuilder? BuildEndpoint(IEndpointRouteBuilder app)
        {
            EndpointDefinitionAttribute? def = GetType().GetCustomAttribute<EndpointDefinitionAttribute>(inherit: true);
            if (def is null)
            {
                return null;
            }

            EndpointRequestSource source = GetType().GetCustomAttribute<EndpointRequestSourceAttribute>(inherit: true)?.Source
                         ?? (string.Equals(def.Method, "GET", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(def.Method, "DELETE", StringComparison.OrdinalIgnoreCase)
                                ? EndpointRequestSource.Query
                                : EndpointRequestSource.Body);

            // Log the final route that will be used for this endpoint (deterministic)
            RouteHandlerBuilder builder = BuildWithSource(app, def, source);
            builder = ApplyEndpointConventions(builder);
            return ApplyEndpointDefinitionMetadata(builder, def);
        }

        private RouteHandlerBuilder BuildWithSource(IEndpointRouteBuilder app, EndpointDefinitionAttribute def, EndpointRequestSource source)
        {
            if (source == EndpointRequestSource.HttpRequest)
            {
                return MapByMethod(app, def, async (HttpContext httpContext, HttpRequest request, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
                {
                    if (!TryGetRouteKey(httpContext, out TKey? key))
                    {
                        return Results.BadRequest(ApiResponse.Failure("Invalid route key"));
                    }

                    TRequest? dto = CreateRequestFromHttpRequest<TRequest>(request);
                    return await ExecuteAsync(key!, dto, cqrsManager, httpContext, cancellationToken);
                });
            }

            if (source == EndpointRequestSource.Query)
            {
                return MapByMethod(app, def, async (HttpContext httpContext, HttpRequest request, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
                {
                    if (!TryGetRouteKey(httpContext, out TKey? key))
                    {
                        return Results.BadRequest(ApiResponse.Failure("Invalid route key"));
                    }

                    TRequest? dto = CreateRequestFromHttpRequest<TRequest>(request);
                    return await ExecuteAsync(key!, dto, cqrsManager, httpContext, cancellationToken);
                });
            }

            return MapByMethod(app, def, async (HttpContext httpContext, [FromBody] TRequest request, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
            {
                if (!TryGetRouteKey(httpContext, out TKey? key))
                {
                    return Results.BadRequest(ApiResponse.Failure("Invalid route key"));
                }

                return await ExecuteAsync(key!, request, cqrsManager, httpContext, cancellationToken);
            });
        }
    }

    public abstract class BaseEndpoint<TRequest> : AbstractEndpoint
    {
        protected abstract Task<IResult> HandleAsync(TRequest request, ICQRSManager cqrsManager, HttpContext httpContext, CancellationToken cancellationToken);

        protected Task<IResult> ExecuteAsync(TRequest request, ICQRSManager cqrsManager, HttpContext httpContext, CancellationToken cancellationToken)
        {
            return ExecuteWithWorkflowAsync(httpContext, cancellationToken, () => HandleAsync(request, cqrsManager, httpContext, cancellationToken));
        }

        protected override RouteHandlerBuilder? BuildEndpoint(IEndpointRouteBuilder app)
        {
            EndpointDefinitionAttribute? def = GetType().GetCustomAttribute<EndpointDefinitionAttribute>(inherit: true);
            if (def is null)
            {
                return null;
            }

            EndpointRequestSource source = GetType().GetCustomAttribute<EndpointRequestSourceAttribute>(inherit: true)?.Source
                         ?? (string.Equals(def.Method, "GET", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(def.Method, "DELETE", StringComparison.OrdinalIgnoreCase)
                                ? EndpointRequestSource.Query
                                : EndpointRequestSource.Body);

            try
            {
                string finalRoutePreview = CombineApiBasePath(app, def.Route);
                _ = finalRoutePreview; // route computed for convention application
            }
            catch { }

            RouteHandlerBuilder builder = source switch
            {
                EndpointRequestSource.HttpRequest => MapByMethod(app, def, async (HttpContext httpContext, HttpRequest request, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
                {
                    TRequest? dto = CreateRequestFromHttpRequest<TRequest>(request);
                    return await ExecuteAsync(dto, cqrsManager, httpContext, cancellationToken);
                }),
                EndpointRequestSource.Query => MapByMethod(app, def, async (HttpContext httpContext, HttpRequest request, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
                {
                    TRequest? dto = CreateRequestFromHttpRequest<TRequest>(request);
                    return await ExecuteAsync(dto, cqrsManager, httpContext, cancellationToken);
                }),
                _ => MapByMethod(app, def, async (HttpContext httpContext, [FromBody] TRequest request, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
                {
                    return await ExecuteAsync(request, cqrsManager, httpContext, cancellationToken);
                })
            };

            builder = ApplyEndpointConventions(builder);
            return ApplyEndpointDefinitionMetadata(builder, def);
        }
    }

    public abstract class BaseEndpoint : AbstractEndpoint
    {
        protected abstract Task<IResult> HandleAsync(ICQRSManager cqrsManager, HttpContext httpContext, CancellationToken cancellationToken);

        protected Task<IResult> ExecuteAsync(ICQRSManager cqrsManager, HttpContext httpContext, CancellationToken cancellationToken)
        {
            return ExecuteWithWorkflowAsync(httpContext, cancellationToken, () => HandleAsync(cqrsManager, httpContext, cancellationToken));
        }

        protected override RouteHandlerBuilder? BuildEndpoint(IEndpointRouteBuilder app)
        {
            EndpointDefinitionAttribute? def = GetType().GetCustomAttribute<EndpointDefinitionAttribute>(inherit: true);
            if (def is null)
            {
                return null;
            }

            try
            {
                string finalRoutePreview = CombineApiBasePath(app, def.Route);
                _ = finalRoutePreview; // route computed for convention application
            }
            catch { }

            RouteHandlerBuilder builder = MapByMethod(app, def, async (HttpContext httpContext, [FromServices] ICQRSManager cqrsManager, CancellationToken cancellationToken) =>
            {
                return await ExecuteAsync(cqrsManager, httpContext, cancellationToken);
            });

            builder = ApplyEndpointConventions(builder);
            return ApplyEndpointDefinitionMetadata(builder, def);
        }
    }

    public abstract class AbstractEndpoint : IEndpoint
    {
        public virtual void MapEndpoint(IEndpointRouteBuilder app)
        {
            _ = BuildEndpoint(app) ?? throw new NotImplementedException($"Endpoint '{GetType().Name}' must implement MapEndpoint or use EndpointDefinition attributes.");
        }

        protected virtual RouteHandlerBuilder? BuildEndpoint(IEndpointRouteBuilder app)
        {
            return null;
        }

        protected static IResult OkResponse<T>(T data)
        {
            return Results.Ok(ApiResponse<T>.Successful(data));
        }

        protected static IResult CreatedResponse<T>(string location, T data)
        {
            return Results.Created(location, ApiResponse<T>.Successful(data));
        }

        protected static IResult Success()
        {
            return Results.Ok(ApiResponse.Successful());
        }

        protected static IResult Failure(string message, ErrorDetails? error = null)
        {
            return Results.BadRequest(ApiResponse.Failure(message, error));
        }

        protected RouteHandlerBuilder MapPost(IEndpointRouteBuilder app, string pattern, Delegate handler)
        {
            RouteHandlerBuilder builder = app.MapPost(pattern, handler);
            return ApplyEndpointMetadata(builder);
        }

        protected RouteHandlerBuilder MapGet(IEndpointRouteBuilder app, string pattern, Delegate handler)
        {
            RouteHandlerBuilder builder = app.MapGet(pattern, handler);
            return ApplyEndpointMetadata(builder);
        }

        protected RouteHandlerBuilder MapPut(IEndpointRouteBuilder app, string pattern, Delegate handler)
        {
            RouteHandlerBuilder builder = app.MapPut(pattern, handler);
            return ApplyEndpointMetadata(builder);
        }

        protected RouteHandlerBuilder MapPatch(IEndpointRouteBuilder app, string pattern, Delegate handler)
        {
            RouteHandlerBuilder builder = app.MapPatch(pattern, handler);
            return ApplyEndpointMetadata(builder);
        }

        protected RouteHandlerBuilder MapDelete(IEndpointRouteBuilder app, string pattern, Delegate handler)
        {
            RouteHandlerBuilder builder = app.MapDelete(pattern, handler);
            return ApplyEndpointMetadata(builder);
        }

        protected virtual bool CanCheckPermission(HttpContext httpContext)
        {
            return true;
        }

        protected virtual async Task<bool> CheckPermissionAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            EndpointPermissionAttribute? permissionAttribute = GetType().GetCustomAttribute<EndpointPermissionAttribute>(inherit: true);
            if (permissionAttribute is null || !permissionAttribute.Enable)
            {
                return true;
            }

            if (!CanCheckPermission(httpContext))
            {
                return true;
            }

            // If the principal contains a direct 'permission' claim matching the required permission, allow
            if (httpContext.User?.HasClaim(c => string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase) && string.Equals(c.Value, permissionAttribute.PermissionCode, StringComparison.OrdinalIgnoreCase)) == true)
            {
                return true;
            }

            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            IPermissionChecker? permissionChecker = httpContext.RequestServices.GetService<IPermissionChecker>();
            if (permissionChecker is null)
            {
                return false;
            }

            return await permissionChecker.HasPermissionAsync(permissionAttribute.PermissionCode, cancellationToken);
        }

        protected async Task<IResult> ExecuteWithWorkflowAsync(HttpContext httpContext, CancellationToken cancellationToken, Func<Task<IResult>> handleAction)
        {
            if (!await CheckPermissionAsync(httpContext, cancellationToken))
            {
                return UnauthorizedResponse();
            }

            return await handleAction();
        }

        protected static IResult UnauthorizedResponse(string message = "Access denied")
        {
            ErrorDetails detail = new() { Code = ErrorCodes.Unauthorized, Description = message };
            ApiResponse body = ApiResponse.Failure(message, detail, ErrorCodes.Unauthorized);
            return Results.Json(body, statusCode: StatusCodes.Status401Unauthorized);
        }

        protected static IResult NotFoundResponse(string message = "Not found")
        {
            ErrorDetails detail = new() { Code = ErrorCodes.NotFound, Description = message };
            ApiResponse body = ApiResponse.Failure(message, detail, ErrorCodes.NotFound);
            return Results.Json(body, statusCode: StatusCodes.Status404NotFound);
        }

        protected RouteHandlerBuilder ApplyEndpointConventions(RouteHandlerBuilder builder)
        {
            Type endpointType = GetType();

            foreach (EndpointProducesAttribute produces in endpointType.GetCustomAttributes<EndpointProducesAttribute>(inherit: true))
            {
                if (produces.ResponseType is null)
                {
                    builder.Produces(produces.StatusCode);
                }
                else
                {
                    builder.Produces(produces.StatusCode, produces.ResponseType);
                }
            }

            if (endpointType.GetCustomAttribute<EndpointDisableAntiforgeryAttribute>(inherit: true) is not null)
            {
                builder.DisableAntiforgery();
            }

            if (endpointType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null)
            {
                builder.AllowAnonymous();
            }

            AuthorizeAttribute[] authorizeAttributes = [.. endpointType.GetCustomAttributes<AuthorizeAttribute>(inherit: true)];
            if (authorizeAttributes.Length > 0)
            {
                foreach (AuthorizeAttribute? authorize in authorizeAttributes)
                {
                    builder.RequireAuthorization(authorize);
                }
            }

            // If endpoint route begins with /v1, enforce JwtBearer authentication at endpoint level
            // to ensure API endpoints always require JWT regardless of middleware ordering.
            // EXCEPTION: skip auto-policy when the endpoint already declares [Authorize] with explicit
            // AuthenticationSchemes (e.g. "AuthServer.Session,Bearer") — those endpoints manage their
            // own scheme negotiation and the auto-policy would override their cookie acceptance.
            try
            {
                EndpointDefinitionAttribute? def = endpointType.GetCustomAttribute<EndpointDefinitionAttribute>(inherit: true);
                if (def is not null && !string.IsNullOrWhiteSpace(def.Route))
                {
                    string route = def.Route.StartsWith('/') ? def.Route : "/" + def.Route;
                    if (route.StartsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check whether any [Authorize] on this type already specifies schemes.
                        bool hasExplicitSchemes = endpointType
                            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                            .Any(a => !string.IsNullOrWhiteSpace(a.AuthenticationSchemes));

                        if (!hasExplicitSchemes)
                        {
                            var jwtAuthorize = new AuthorizeAttribute { Policy = "ApiJwt" };
                            builder.RequireAuthorization(jwtAuthorize);
                        }
                    }
                }
            }
            catch { }

            return builder;
        }

        protected RouteHandlerBuilder ApplyEndpointMetadata(RouteHandlerBuilder builder)
        {
            EndpointMetadataAttribute? metadata = GetType().GetCustomAttribute<EndpointMetadataAttribute>(inherit: true);
            if (metadata is null)
            {
                return builder;
            }

            if (!string.IsNullOrWhiteSpace(metadata.Tag))
            {
                builder.WithTags(metadata.Tag);
            }

            if (!string.IsNullOrWhiteSpace(metadata.Name))
            {
                builder.WithName(metadata.Name);
            }

            return builder;
        }

        protected RouteHandlerBuilder ApplyEndpointDefinitionMetadata(RouteHandlerBuilder builder, EndpointDefinitionAttribute def)
        {
            EndpointMetadataAttribute? metadata = GetType().GetCustomAttribute<EndpointMetadataAttribute>(inherit: true);
            EndpointOpenApiDescriptionAttribute? description = GetType().GetCustomAttribute<EndpointOpenApiDescriptionAttribute>(inherit: true);

            string tag = metadata?.Tag ?? def.Tag;
            string name = metadata?.Name ?? def.Name;

            if (!string.IsNullOrWhiteSpace(tag))
            {
                builder.WithTags(tag);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                builder.WithName(name);
            }

            if (!string.IsNullOrWhiteSpace(description?.Description))
            {
                builder.WithDescription(description.Description);
            }

            return builder;
        }

        protected static RouteHandlerBuilder MapByMethod(IEndpointRouteBuilder app, EndpointDefinitionAttribute def, Delegate handler)
        {
            string route = CombineApiBasePath(app, def.Route);

            return def.Method.ToUpperInvariant() switch
            {
                "GET" => app.MapGet(route, handler),
                "POST" => app.MapPost(route, handler),
                "PUT" => app.MapPut(route, handler),
                "DELETE" => app.MapDelete(route, handler),
                "PATCH" => app.MapPatch(route, handler),
                _ => throw new InvalidOperationException($"Unsupported HTTP method '{def.Method}' for endpoint '{def.Name}'.")
            };
        }

        protected static string CombineApiBasePath(IEndpointRouteBuilder app, string route)
        {
            try
            {
                IConfiguration? config = app.ServiceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
                string? basePath = config?.GetValue<string>("Api:BasePath");

                if (string.IsNullOrWhiteSpace(basePath))
                {
                    return route;
                }

                if (!basePath.StartsWith('/'))
                {
                    basePath = "/" + basePath;
                }

                basePath = basePath!.TrimEnd('/');

                if (string.IsNullOrWhiteSpace(route))
                {
                    return basePath;
                }

                if (!route.StartsWith('/'))
                {
                    route = "/" + route;
                }

                // If route already contains the basePath prefix, don't duplicate
                if (route.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase) || string.Equals(route, basePath, StringComparison.OrdinalIgnoreCase))
                {
                    return route;
                }

                return basePath + route;
            }
            catch
            {
                return route;
            }
        }

        protected static TRequest CreateRequestFromHttpRequest<TRequest>(HttpRequest httpRequest)
        {
            ConstructorInfo? ctor = typeof(TRequest).GetConstructor([typeof(HttpRequest)]);
            if (ctor is null)
            {
                if (TryCreateRequestFromSimpleValue(httpRequest, out TRequest? simpleValue))
                {
                    return simpleValue!;
                }

                if (TryCreateRequestFromProperties(httpRequest, out TRequest? propertyValue))
                {
                    return propertyValue!;
                }

                if (TryCreateRequestFromConstructorParameters(httpRequest, out TRequest? complexValue))
                {
                    return complexValue!;
                }

                throw new InvalidOperationException($"Type '{typeof(TRequest).Name}' must provide a constructor with HttpRequest parameter or be bindable from route/query values.");
            }

            return (TRequest)ctor.Invoke([httpRequest]);
        }

        private static bool TryCreateRequestFromSimpleValue<TRequest>(HttpRequest httpRequest, out TRequest? result)
        {
            Type requestType = typeof(TRequest);

            if (requestType == typeof(object))
            {
                result = (TRequest)(object)new object();
                return true;
            }

            if (!IsSimpleBindableType(requestType))
            {
                result = default;
                return false;
            }

            if (!TryGetFirstRawValue(httpRequest, out string? rawValue) || !TryConvertRawValue(rawValue, requestType, out object? converted))
            {
                result = default;
                return false;
            }

            result = (TRequest?)converted;
            return true;
        }

        private static bool TryCreateRequestFromProperties<TRequest>(HttpRequest httpRequest, out TRequest? result)
        {
            Type requestType = typeof(TRequest);
            ConstructorInfo? ctor = requestType.GetConstructor(Type.EmptyTypes);
            if (ctor is null)
            {
                result = default;
                return false;
            }

            object instance = ctor.Invoke(null);
            foreach (PropertyInfo property in requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                if (!TryGetNamedRawValue(httpRequest, property.Name, out string? rawValue))
                {
                    continue;
                }

                if (TryConvertRawValue(rawValue, property.PropertyType, out object? converted))
                {
                    property.SetValue(instance, converted);
                }
            }

            result = (TRequest)instance;
            return true;
        }

        private static bool TryCreateRequestFromConstructorParameters<TRequest>(HttpRequest httpRequest, out TRequest? result)
        {
            Type requestType = typeof(TRequest);
            ConstructorInfo[] constructors = [.. requestType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)];

            foreach (ConstructorInfo? constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    continue;
                }

                object?[] values = new object?[parameters.Length];
                bool canBind = true;

                for (int index = 0; index < parameters.Length; index++)
                {
                    ParameterInfo parameter = parameters[index];
                    if (TryGetNamedRawValue(httpRequest, parameter.Name ?? string.Empty, out string? rawValue)
                        && TryConvertRawValue(rawValue, parameter.ParameterType, out object? converted))
                    {
                        values[index] = converted;
                        continue;
                    }

                    if (parameter.HasDefaultValue)
                    {
                        values[index] = parameter.DefaultValue;
                        continue;
                    }

                    if (!parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) is not null)
                    {
                        values[index] = null;
                        continue;
                    }

                    canBind = false;
                    break;
                }

                if (!canBind)
                {
                    continue;
                }

                result = (TRequest)constructor.Invoke(values);
                return true;
            }

            result = default;
            return false;
        }

        private static bool TryGetFirstRawValue(HttpRequest httpRequest, out string rawValue)
        {
            if (httpRequest.RouteValues.Values.FirstOrDefault() is { } routeValue)
            {
                rawValue = routeValue.ToString() ?? string.Empty;
                return true;
            }

            if (httpRequest.Query.Count > 0)
            {
                rawValue = httpRequest.Query.First().Value.ToString();
                return true;
            }

            rawValue = string.Empty;
            return false;
        }

        private static bool TryGetNamedRawValue(HttpRequest httpRequest, string name, out string rawValue)
        {
            foreach (KeyValuePair<string, object?> routeValue in httpRequest.RouteValues)
            {
                if (string.Equals(routeValue.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    rawValue = routeValue.Value?.ToString() ?? string.Empty;
                    return true;
                }
            }

            foreach (KeyValuePair<string, StringValues> queryValue in httpRequest.Query)
            {
                if (string.Equals(queryValue.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    rawValue = queryValue.Value.ToString();
                    return true;
                }
            }

            rawValue = string.Empty;
            return false;
        }

        private static bool IsSimpleBindableType(Type type)
        {
            Type targetType = Nullable.GetUnderlyingType(type) ?? type;
            return targetType.IsPrimitive
                || targetType.IsEnum
                || targetType == typeof(string)
                || targetType == typeof(Guid)
                || targetType == typeof(DateTime)
                || targetType == typeof(DateOnly)
                || targetType == typeof(TimeOnly)
                || targetType == typeof(decimal);
        }

        private static bool TryConvertRawValue(string rawValue, Type targetType, out object? converted)
        {
            Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                converted = targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null
                    ? Activator.CreateInstance(targetType)
                    : null;
                return true;
            }

            try
            {
                if (effectiveType == typeof(string))
                {
                    converted = rawValue;
                    return true;
                }

                if (effectiveType == typeof(Guid))
                {
                    converted = Guid.Parse(rawValue);
                    return true;
                }

                if (effectiveType == typeof(DateOnly))
                {
                    converted = DateOnly.Parse(rawValue, CultureInfo.InvariantCulture);
                    return true;
                }

                if (effectiveType == typeof(TimeOnly))
                {
                    converted = TimeOnly.Parse(rawValue, CultureInfo.InvariantCulture);
                    return true;
                }

                if (effectiveType.IsEnum)
                {
                    converted = Enum.Parse(effectiveType, rawValue, ignoreCase: true);
                    return true;
                }

                converted = Convert.ChangeType(rawValue, effectiveType, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                converted = null;
                return false;
            }
        }

        protected static bool TryGetRouteKey<TKey>(HttpContext httpContext, out TKey? key)
        {
            key = default;

            object? raw = null;
            if (httpContext.Request.RouteValues.TryGetValue("id", out object? idValue))
            {
                raw = idValue;
            }
            else if (httpContext.Request.RouteValues.Values.FirstOrDefault() is { } first)
            {
                raw = first;
            }

            if (raw is null)
            {
                return false;
            }

            if (raw is TKey typed)
            {
                key = typed;
                return true;
            }

            string? text = raw.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            try
            {
                Type targetType = Nullable.GetUnderlyingType(typeof(TKey)) ?? typeof(TKey);
                object converted = Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture);
                key = (TKey?)converted;
                return true;
            }
            catch
            {
                if (typeof(TKey) == typeof(Guid) && Guid.TryParse(text, out Guid g))
                {
                    key = (TKey)(object)g;
                    return true;
                }
                return false;
            }
        }
    }

    public record EmptyRequest { }
}
