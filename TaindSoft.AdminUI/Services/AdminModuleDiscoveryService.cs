using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TaindSoft.AdminUI.Contracts;

namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Service for discovering and initializing admin modules from assemblies
    /// </summary>
    public class AdminModuleDiscoveryService(IAdminModuleRegistry moduleRegistry, IServiceCollection services)
    {
        private readonly IAdminModuleRegistry _moduleRegistry = moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
        private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));

        /// <summary>
        /// Discover and register modules from specified assemblies
        /// </summary>
        public void DiscoverModules(AdminUIOptions options)
        {
            List<Type> moduleTypes = [];

            // Determine assemblies to scan. Use only already-loaded assemblies (WASM-safe).
            // Prefer explicit ModuleAssemblies provided by the host, but always include
            // AppDomain assemblies so modules packaged with the host are discovered.
            List<Assembly> assembliesToScan = [];

            if (options.ModuleAssemblies != null && options.ModuleAssemblies.Count > 0)
            {
                assembliesToScan.AddRange(options.ModuleAssemblies);
            }

            try
            {
                Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                assembliesToScan.AddRange(allAssemblies);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminModuleDiscoveryService] Failed to enumerate AppDomain assemblies: {ex.Message}");
            }

            // Limit discovery to repository-owned assemblies to avoid scanning test, system or third-party assemblies.
            // Only include assemblies whose simple name starts with "TaindSoft.".
            var beforeFilter = assembliesToScan.Count;
            assembliesToScan = [.. assembliesToScan
                .Where(a =>
                {
                    try
                    {
                        string? name = a.GetName().Name;
                        return !string.IsNullOrEmpty(name) && name.StartsWith("TaindSoft.", StringComparison.Ordinal);
                    }
                    catch
                    {
                        return false;
                    }
                })
                // Deduplicate by assembly identity
                .GroupBy(a => a.FullName)
                .Select(g => g.First())];


            // Collect all module types from specified or discovered assemblies
            foreach (Assembly assembly in assembliesToScan)
            {
                Type[] typesInAssembly;
                try
                {
                    typesInAssembly = assembly.GetTypes();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AdminModuleDiscoveryService] Assembly {assembly.GetName().Name}: GetTypes() threw {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                IEnumerable<Type> types = typesInAssembly
                    .Where(t => typeof(IAdminModule).IsAssignableFrom(t)
                             && t.IsClass
                             && !t.IsAbstract);

                var typesList = types.ToList();
                moduleTypes.AddRange(typesList);
            }

            // ponytail: if no TaindSoft.*.AdminUI modules were discovered, the host
            // probably forgot to ScanModulesFromAssemblies(typeof(SomeModule).Assembly).
            // Loud warning beats an empty sidebar.
            if (moduleTypes.Count == 0)
            {
                Console.WriteLine("[AdminModuleDiscoveryService] WARNING: 0 IAdminModule types found. " +
                    "Ensure the host calls ScanModulesFromAssemblies(typeof(<YourModuleClass>).Assembly) " +
                    "for every *.AdminUI module — AppDomain enumeration alone misses lazily-loaded assemblies.");
            }

            ServiceProvider tempServiceProvider = _services.BuildServiceProvider();

            try
            {
                // Instantiate all modules using ActivatorUtilities for DI support
                List<IAdminModule> modules = [];

                foreach (Type moduleType in moduleTypes)
                {
                    try
                    {
                        // Use ActivatorUtilities.CreateInstance to support DI in module constructors
                        IAdminModule module = (IAdminModule)ActivatorUtilities.CreateInstance(
                            tempServiceProvider,
                            moduleType);

                        modules.Add(module);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to instantiate module {moduleType.Name}. " +
                            $"Ensure the module has a public constructor. Error: {ex.Message}", ex);
                    }
                }

                // Order modules by dependencies using topological sort
                Console.WriteLine($"[AdminModuleDiscoveryService] Ordering {modules.Count} modules by dependencies");
                List<IAdminModule> orderedModules = OrderModulesByDependencies(modules);

                // Register modules in dependency order
                foreach (IAdminModule module in orderedModules)
                {
                    // Let module configure its services
                    module.ConfigureServices(_services, options.ApiBaseUrl);

                    // Register module in registry
                    _moduleRegistry.RegisterModule(module);

                    // Register module instance in DI for later retrieval
                    _services.AddSingleton(module);
                }
            }
            finally
            {
                // Dispose temporary service provider
                if (tempServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Order modules by dependencies using topological sort
        /// </summary>
        private static List<IAdminModule> OrderModulesByDependencies(List<IAdminModule> modules)
        {
            Dictionary<string, IAdminModule> moduleMap = modules.ToDictionary(m => m.Name, m => m);
            HashSet<string> visited = [];
            List<IAdminModule> result = [];

            void Visit(IAdminModule module)
            {
                if (visited.Contains(module.Name))
                {
                    return;
                }

                visited.Add(module.Name);

                // Visit dependencies first
                foreach (string dependency in module.Dependencies ?? [])
                {
                    if (moduleMap.TryGetValue(dependency, out IAdminModule? dependencyModule))
                    {
                        Visit(dependencyModule);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Module '{module.Name}' depends on '{dependency}' which is not registered.");
                    }
                }

                result.Add(module);
            }

            // Visit all modules
            foreach (IAdminModule module in modules)
            {
                Visit(module);
            }

            return result;
        }
    }
}
