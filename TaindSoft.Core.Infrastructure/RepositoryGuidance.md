Repository usage guidance

Correct pattern (commands - tracked):
1. var aggregate = await repository.GetByIdAsync(id);
2. aggregate.DoSomething(...);
3. await repository.SaveChangesAsync();

Wrong pattern (do not use):
1. var aggregate = await repository.GetByIdAsync(id);
2. aggregate.DoSomething(...);
3. await repository.UpdateAsync(aggregate); // Do NOT do this for tracked aggregates

Repository contracts
- GetByIdAsync -> returns tracked entity (for commands)
- GetByIdReadOnlyAsync -> returns AsNoTracking entity (for queries)
- SaveChangesAsync -> commit unit of work
- UpdateAsync -> only for detached entities (marked obsolete and guarded)

Guidance for reviewers
- Reject PRs that call UpdateAsync after loading entity in same method
- Prefer SaveChangesAsync for tracked aggregates
