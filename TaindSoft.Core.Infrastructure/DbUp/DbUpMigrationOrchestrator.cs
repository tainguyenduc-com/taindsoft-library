using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using Microsoft.Extensions.Logging;

namespace TaindSoft.Core.Infrastructure.DbUp
{
    public record DbUpMigrationOptions(string ConnectionString, string JournalTable, string ScriptPath = "Scripts", bool DisableSchemaScriptVariableSubstitution = true, bool Enabled = false, bool FailIfNoScripts = false);
    /// <summary>
    /// TODO: Document class DbUpMigrationOrchestrator
    /// </summary>
    public static class DbUpMigrationOrchestrator
    {
        private static readonly string[] OrderedScopes = ["Schema", "Data", "Seed", "Fix"];

        /// <summary>
        /// Resolves the root directory for migration scripts.
        /// Uses the TAINDSOFT_SCRIPTS_ROOT environment variable if set,
        /// otherwise falls back to AppContext.BaseDirectory + relativePath.
        /// </summary>
        public static string ResolveScriptsRoot(string relativePath)
        {
            var envRoot = Environment.GetEnvironmentVariable("TAINDSOFT_SCRIPTS_ROOT");
            return Path.Combine(envRoot ?? AppContext.BaseDirectory, relativePath);
        }

        public static bool Apply(Func<DbUpMigrationOptions> configOption, ILogger? logger = null)
        {
            logger ??= Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            logger.LogInformation("Starting DbUp migration...");
            DbUpMigrationOptions options = configOption();

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                logger.LogError("ConnectionString is required for DbUp migration.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.JournalTable))
            {
                logger.LogError("JournalTable name is required for DbUp migration.");
                return false;
            }
            string journalTable = GetJournalTableName(options.JournalTable);

            List<string> scriptPaths = ResolveScriptPaths(options.ScriptPath, logger);
            if (scriptPaths.Count == 0)
            {
                if (options.FailIfNoScripts)
                {
                    logger.LogError("DbUp: required scripts not found for {JournalTable}. Aborting startup.", journalTable);
                    return false;
                }
                logger.LogInformation("DbUp: no scripts found for {JournalTable}, skipping.", journalTable);
                return true;
            }

            try
            {
                // First, handle non-Schema scripts by collecting them into a primary builder.
                UpgradeEngineBuilder primaryBuilder = DeployChanges.To.PostgresqlDatabase(options.ConnectionString)
                    .WithoutTransaction()
                    .JournalToPostgresqlTable("public", journalTable);
                bool hasPrimaryScripts = false;

                foreach (string path in scriptPaths)
                {
                    if (string.Equals(Path.GetFileName(path), "Schema", StringComparison.OrdinalIgnoreCase))
                    {
                        // EF-generated scripts use dollar-quoted DO blocks: DO $EF$ BEGIN ... END $EF$;
                        // DbUp's variable-substitution preprocessor treats $EF$ as a variable placeholder
                        // and replaces it with an empty string, producing invalid SQL (DO  BEGIN ... END ;).
                        // Fix: run Schema scripts in a dedicated upgrader with variable substitution disabled.
                        if (Directory.GetFiles(path, "*.sql", SearchOption.AllDirectories).Length == 0)
                        {
                            continue;
                        }

                        logger.LogInformation("DbUp: Applying Schema scripts from {Path}", path);
                        UpgradeEngineBuilder schemaBuilder = DeployChanges.To.PostgresqlDatabase(options.ConnectionString)
                            .WithoutTransaction()
                            .JournalToPostgresqlTable("public", journalTable)
                            .WithScriptsFromFileSystem(path)
                            .LogToConsole();

                        if (options.DisableSchemaScriptVariableSubstitution)
                        {
                            schemaBuilder = schemaBuilder.WithVariablesDisabled();
                        }

                        UpgradeEngine schemaUpgrader = schemaBuilder.Build();
                        DatabaseUpgradeResult schemaResult = schemaUpgrader.PerformUpgrade();
                        if (!schemaResult.Successful)
                        {
                            logger.LogError("DbUp failed for {JournalTable} Schema scripts: {Error}", options.JournalTable, schemaResult.Error);
                            return false;
                        }
                    }
                    else
                    {
                        logger.LogInformation("DbUp: Adding non-Schema scripts from {Path}", path);
                        primaryBuilder = primaryBuilder.WithScriptsFromFileSystem(path);
                        hasPrimaryScripts = true;
                    }
                }

                // Execute non-schema scripts only if any were added (otherwise Build() throws)
                if (hasPrimaryScripts)
                {
                    if (options.DisableSchemaScriptVariableSubstitution)
                    {
                        primaryBuilder = primaryBuilder.WithVariablesDisabled();
                    }

                    logger.LogInformation("DbUp: Applying non-Schema scripts for {JournalTable}", journalTable);
                    UpgradeEngine primaryUpgrader = primaryBuilder.LogToConsole().Build();
                    DatabaseUpgradeResult primaryResult = primaryUpgrader.PerformUpgrade();
                    if (!primaryResult.Successful)
                    {
                        logger.LogError("DbUp failed for {JournalTable} non-Schema scripts: {Error}", options.JournalTable, primaryResult.Error);
                        return false;
                    }
                }
                logger.LogInformation("DbUp migration completed successfully for journal table {JournalTable}.", journalTable);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error during DbUp migration");
                return false;
            }
        }

        private static List<string> ResolveScriptPaths(string scriptPath, ILogger logger)
        {
            List<string> result = [];

            string fullScriptPath = ResolveScriptsRoot(scriptPath);

            logger?.LogDebug("DbUp: base={Base}, scriptPath={ScriptPath}, fullScriptPath={FullScriptPath}",
                AppContext.BaseDirectory, scriptPath, fullScriptPath);

            // Always look for 'Schema' subfolder first
            string schemaPath = Path.Combine(fullScriptPath, "Schema");
            logger?.LogDebug("DbUp: Checking schemaPath={SchemaPath}, exists={Exists}", schemaPath, Directory.Exists(schemaPath));

            if (Directory.Exists(schemaPath) && Directory.GetFiles(schemaPath, "*.sql", SearchOption.AllDirectories).Length > 0)
            {
                logger?.LogInformation("DbUp: Found Schema scripts at {SchemaPath}", schemaPath);
                result.Add(schemaPath);
            }
            else
            {
                logger?.LogWarning("DbUp: Schema path does not exist or has no SQL files: {SchemaPath}", schemaPath);
            }

            // Optionally, check other ordered scopes if they exist and contain scripts
            foreach (string scope in OrderedScopes.Where(s => !string.Equals(s, "Schema", StringComparison.OrdinalIgnoreCase)))
            {
                string scopedPath = Path.Combine(fullScriptPath, scope);
                if (Directory.Exists(scopedPath) && Directory.GetFiles(scopedPath, "*.sql", SearchOption.AllDirectories).Length > 0)
                {
                    logger?.LogInformation("DbUp: Found {Scope} scripts at {ScopedPath}", scope, scopedPath);
                    result.Add(scopedPath);
                }
            }

            return result;
        }

        private static string GetJournalTableName(string moduleName)
        {
            string normalized = new([.. moduleName
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')]);

            return $"__dbup_journal_{normalized}";
        }
    }
}
