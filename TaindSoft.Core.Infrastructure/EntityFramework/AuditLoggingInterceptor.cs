using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TaindSoft.Core.Infrastructure.Auditing;

namespace TaindSoft.Core.Infrastructure.EntityFramework
{
    /// <summary>
    /// EF Core interceptor that captures audit information on SaveChanges.
    /// </summary>
    public class AuditLoggingInterceptor(Func<CancellationToken, Task<string?>> getUserIdAsync, IServiceProvider serviceProvider) : SaveChangesInterceptor
    {
        private readonly Func<CancellationToken, Task<string?>> _getUserIdAsync = getUserIdAsync ?? throw new ArgumentNullException(nameof(getUserIdAsync));
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Entities whose changes must NOT be audited (prevents infinite recursion
        // when the audit store itself saves to SystemManagementDbContext).
        private static readonly HashSet<string> SkipEntityNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "AuditLog",
        };

        private static readonly HashSet<string> DenyList = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "Secret",
            "Token",
            "ApiKey",
            "Api_Key",
            "Credentials"
        };

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            DbContext? context = eventData.Context;
            if (context == null)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            List<EntityEntry> entries = [.. context.ChangeTracker.Entries()
                .Where(e =>
                    (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    && !SkipEntityNames.Contains(e.Entity.GetType().Name))];

            if (entries.Count == 0)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            string? userId = await _getUserIdAsync(cancellationToken);

            List<AuditRequest> auditLogs = [];

            foreach (EntityEntry e in entries)
            {
                string entityName = e.Entity.GetType().Name;
                PropertyEntry? key = e.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                string entityId = key?.CurrentValue?.ToString() ?? string.Empty;

                if (e.State == EntityState.Added)
                {
                    Dictionary<string, object?> newVals = e.Properties
                        .Where(p => !DenyList.Contains(p.Metadata.Name))
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

                    auditLogs.Add(new AuditRequest
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Create",
                        UserId = userId,
                        OldValues = null,
                        NewValues = JsonSerializer.Serialize(newVals),
                        ExecutionTime = DateTime.UtcNow
                    });
                }
                else if (e.State == EntityState.Deleted)
                {
                    Dictionary<string, object?> origVals = e.Properties
                        .Where(p => !DenyList.Contains(p.Metadata.Name))
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                    auditLogs.Add(new AuditRequest
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Delete",
                        UserId = userId,
                        OldValues = JsonSerializer.Serialize(origVals),
                        NewValues = null,
                        ExecutionTime = DateTime.UtcNow
                    });
                }
                else if (e.State == EntityState.Modified)
                {
                    Dictionary<string, object?> original = [];
                    Dictionary<string, object?> updated = [];
                    foreach (PropertyEntry p in e.Properties)
                    {
                        if (p.IsModified && !DenyList.Contains(p.Metadata.Name))
                        {
                            original[p.Metadata.Name] = p.OriginalValue;
                            updated[p.Metadata.Name] = p.CurrentValue;
                        }
                    }

                    if (original.Count != 0)
                    {
                        auditLogs.Add(new AuditRequest
                        {
                            EntityName = entityName,
                            EntityId = entityId,
                            Action = "Update",
                            UserId = userId,
                            OldValues = JsonSerializer.Serialize(original),
                            NewValues = JsonSerializer.Serialize(updated),
                            ExecutionTime = DateTime.UtcNow
                        });
                    }
                }
            }

            if (auditLogs.Count != 0)
            {
                // Resolve ISystemAuditStore per-call from a new scope to avoid capturing
                // a scoped service inside a singleton, and to prevent circular calls when
                // SystemManagementDbContext itself is intercepted (AuditLog entries are
                // skipped via SkipEntityNames above).
                try
                {
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    ISystemAuditStore? auditStore = scope.ServiceProvider.GetService<ISystemAuditStore>();
                    if (auditStore != null)
                    {
                        await auditStore.SaveAsync(auditLogs, cancellationToken);
                    }
                }
                catch
                {
                    // Audit failures must never break the main operation.
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
