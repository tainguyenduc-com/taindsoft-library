using TaindSoft.AdminUI.Contracts;

namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Service for discovering and managing admin modules
    /// </summary>
    public interface IAdminModuleRegistry
    {
        /// <summary>
        /// Get all registered modules
        /// </summary>
        IEnumerable<IAdminModule> GetModules();

        /// <summary>
        /// Register a module
        /// </summary>
        void RegisterModule(IAdminModule module);
    }

    /// <summary>
    /// Default implementation of module registry
    /// </summary>
    /// <summary>
    /// Tracks registered Admin UI modules and provides discovery helpers for the UI shell.
    /// </summary>
    public class AdminModuleRegistry : IAdminModuleRegistry
    {
        private readonly List<IAdminModule> _modules = [];

        public IEnumerable<IAdminModule> GetModules()
        {
            return _modules;
        }

        public void RegisterModule(IAdminModule module)
        {
            if (!_modules.Any(m => m.Name == module.Name))
            {
                _modules.Add(module);
            }
        }
    }
}
