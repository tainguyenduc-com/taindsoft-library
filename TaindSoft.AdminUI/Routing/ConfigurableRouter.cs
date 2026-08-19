using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Reflection;

namespace TaindSoft.AdminUI.Routing
{
    /// <summary>
    /// Custom Router component that normalizes incoming URLs before route matching.
    /// Maps configured admin prefix (e.g., /backoffice/...) to /admin/... internally
    /// so route templates stay as /admin/... while the actual URL uses any prefix.
    ///
    /// Uses reflection to access internal types (RouteTableFactory, RouteTable,
    /// RouteContext, RouteKey) since their constructors and key methods are public
    /// but the type declarations are internal (CS0122 at compile time).
    /// </summary>
    public class ConfigurableRouter : IComponent, IDisposable
    {
        private static class RouteTableApi
        {
            internal static readonly Type FactoryType;
            internal static readonly Type KeyType;
            internal static readonly Type CtxType;
            internal static readonly Type TableType;

            internal static readonly ConstructorInfo FactoryCtor;
            internal static readonly ConstructorInfo CtxCtor;

            internal static readonly MethodInfo CreateMethod;
            internal static readonly MethodInfo RouteMethod;

            internal static readonly PropertyInfo HandlerProp;
            internal static readonly PropertyInfo ParamsProp;

            static RouteTableApi()
            {
                var asm = typeof(Router).Assembly;

                FactoryType = asm.GetType("Microsoft.AspNetCore.Components.RouteTableFactory")
                    ?? throw new InvalidOperationException("RouteTableFactory type not found");
                KeyType = asm.GetType("Microsoft.AspNetCore.Components.Routing.RouteKey")
                    ?? throw new InvalidOperationException("RouteKey type not found");
                CtxType = asm.GetType("Microsoft.AspNetCore.Components.Routing.RouteContext")
                    ?? throw new InvalidOperationException("RouteContext type not found");
                TableType = asm.GetType("Microsoft.AspNetCore.Components.Routing.RouteTable")
                    ?? throw new InvalidOperationException("RouteTable type not found");

                FactoryCtor = FactoryType.GetConstructor(Type.EmptyTypes)
                    ?? throw new InvalidOperationException("RouteTableFactory() ctor not found");
                CtxCtor = CtxType.GetConstructor([typeof(string)])
                    ?? throw new InvalidOperationException("RouteContext(string) ctor not found");

                CreateMethod = FactoryType.GetMethod("Create", [KeyType, typeof(IServiceProvider)])
                    ?? throw new InvalidOperationException("RouteTableFactory.Create(RouteKey, IServiceProvider) not found");
                RouteMethod = TableType.GetMethod("Route", [CtxType])
                    ?? throw new InvalidOperationException("RouteTable.Route(RouteContext) not found");

                HandlerProp = CtxType.GetProperty("Handler")
                    ?? throw new InvalidOperationException("RouteContext.Handler not found");
                ParamsProp = CtxType.GetProperty("Parameters")
                    ?? throw new InvalidOperationException("RouteContext.Parameters not found");
            }
        }

        private RenderHandle _renderHandle;
        private object? _routeTable;
        private bool _initialized;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IServiceProvider ServiceProvider { get; set; } = default!;

        /// <summary>
        /// The assembly containing admin pages with @page "/admin/..." directives.
        /// </summary>
        [Parameter]
        public Assembly AppAssembly { get; set; } = default!;

        /// <summary>
        /// Optional additional assemblies containing admin pages.
        /// </summary>
        [Parameter]
        public IEnumerable<Assembly>? AdditionalAssemblies { get; set; }

        /// <summary>
        /// Render fragment for matched routes (receives RouteData).
        /// </summary>
        [Parameter]
        public RenderFragment<RouteData>? Found { get; set; }

        /// <summary>
        /// Render fragment for unmatched routes (404).
        /// </summary>
        [Parameter]
        public RenderFragment? NotFound { get; set; }

        /// <summary>
        /// Configurable admin URL prefix. Default "admin".
        /// Set to "backoffice" to serve admin pages under /backoffice/...
        /// </summary>
        [Parameter]
        public string AdminPrefix { get; set; } = "admin";

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (!_initialized)
            {
                _initialized = true;

                var assemblies = new List<Assembly> { AppAssembly };
                if (AdditionalAssemblies is not null)
                {
                    assemblies.AddRange(AdditionalAssemblies);
                }

                // RouteTableFactory factory = new RouteTableFactory();
                var factory = RouteTableApi.FactoryCtor.Invoke(null);

                var additional = AdditionalAssemblies ?? Enumerable.Empty<Assembly>();

                // RouteKey key = new RouteKey(AppAssembly, additionalAssemblies);
                var key = Activator.CreateInstance(RouteTableApi.KeyType,
                    [AppAssembly, additional]);

                // _routeTable = factory.Create(key, ServiceProvider);
                _routeTable = RouteTableApi.CreateMethod.Invoke(factory, [key, ServiceProvider])!;

                NavigationManager.LocationChanged += OnLocationChanged;
            }

            Refresh();
            return Task.CompletedTask;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var baseRelativePath = NavigationManager.ToBaseRelativePath(uri.AbsoluteUri);
            var path = "/" + baseRelativePath.TrimStart('/');

            // Strip query string for route matching
            var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                path = path[..queryIndex];
            }

            var normalizedPath = NormalizeIncoming(path);

            // RouteContext routeContext = new RouteContext(normalizedPath);
            var routeContext = RouteTableApi.CtxCtor.Invoke([normalizedPath]);

            // _routeTable.Route(routeContext);
            RouteTableApi.RouteMethod.Invoke(_routeTable, [routeContext]);

            // routeContext.Handler
            var handler = (Type)RouteTableApi.HandlerProp.GetValue(routeContext)!;

            if (handler is not null)
            {
                // routeContext.Parameters
                var parameters = (IReadOnlyDictionary<string, object?>)
                    RouteTableApi.ParamsProp.GetValue(routeContext)!;

                var routeData = new RouteData(
                    handler,
                    parameters ?? new Dictionary<string, object?>());

                _renderHandle.Render(builder =>
                {
                    Found?.Invoke(routeData)(builder);
                });
            }
            else
            {
                _renderHandle.Render(builder =>
                {
                    NotFound?.Invoke(builder);
                });
            }
        }

        /// <summary>
        /// Converts incoming URL path using configured prefix to internal /admin/ path.
        /// /backoffice/dashboard → /admin/dashboard
        /// /backoffice         → /admin
        /// /admin/dashboard    → /admin/dashboard (passthrough)
        /// /                  → / (public pages untouched)
        /// </summary>
        private string NormalizeIncoming(string path)
        {
            if (string.IsNullOrEmpty(AdminPrefix) || AdminPrefix == "admin")
            {
                return path;
            }

            var prefixSegment = "/" + AdminPrefix;

            // /{prefix}/something → /admin/something
            if (path.StartsWith(prefixSegment + "/", StringComparison.OrdinalIgnoreCase))
            {
                return "/admin" + path[prefixSegment.Length..];
            }

            // /{prefix} → /admin
            if (string.Equals(path, prefixSegment, StringComparison.OrdinalIgnoreCase))
            {
                return "/admin";
            }

            // /{prefix}/ → /admin/
            if (string.Equals(path, prefixSegment + "/", StringComparison.OrdinalIgnoreCase))
            {
                return "/admin/";
            }

            // Public page or /admin/... passthrough
            return path;
        }

        public void Dispose()
        {
            if (_initialized)
            {
                NavigationManager.LocationChanged -= OnLocationChanged;
            }
        }
    }
}
