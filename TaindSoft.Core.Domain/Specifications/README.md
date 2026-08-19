# Specification Pattern in TaindSoft.Core.Domain

Clean, composable, and reusable query logic using the Specification Pattern.

## What is the Specification Pattern?

The Specification Pattern encapsulates query logic into reusable objects. Instead of writing LINQ queries throughout your code, you define them once as specifications and reuse them everywhere.

### Benefits

? **DRY (Don't Repeat Yourself)** - Query logic defined once, used everywhere  
? **Testable** - Specifications are easy to unit test  
? **Composable** - Build complex queries from simple building blocks  
? **Clear Intent** - Specification names explain what data is being fetched  
? **Maintainable** - Changes to queries need to be made in one place  
? **Reusable** - Share specifications across handlers, services, reports  

## Architecture

```
ISpecification<T> (interface)
    ?
Specification<T> (base class)
    ?
Your Entity Specifications (User, Product, Order, etc.)
    ?
Repository.ListAsync(spec) (extension methods)
```

## Quick Start

### 1. Define a Specification

```csharp
// Simple specification
public class ActiveUsersSpec : Specification<User>
{
    public ActiveUsersSpec()
    {
        Criteria = u => !u.IsDeleted && u.IsActive;
        AddInclude(u => u.Roles);
        AddOrderBy(u => u.CreatedAt);
        DisableTracking(); // Read-only query
    }
}

// Parameterized specification
public class UsersByRoleSpec : Specification<User>
{
    public UsersByRoleSpec(string roleName)
    {
        Criteria = u => !u.IsDeleted && u.Roles.Any(r => r.Name == roleName);
        AddInclude(u => u.Roles);
        AddOrderBy(u => u.Name);
    }
}

// Paginated specification
public class ActiveUsersPaginatedSpec : Specification<User>
{
    public ActiveUsersPaginatedSpec(int pageNumber, int pageSize = 10)
    {
        Criteria = u => !u.IsDeleted && u.IsActive;
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        AddInclude(u => u.Roles);
        AddOrderByDescending(u => u.CreatedAt);
    }
}
```

### 2. Use in Repository

```csharp
// Single query
var user = await repository.FirstOrDefaultAsync(
    new UsersByIdSpec(userId));

// Multiple entities
var users = await repository.ListAsync(
    new ActiveUsersSpec());

// Paginated
var page = await repository.ListAsync(
    new ActiveUsersPaginatedSpec(pageNumber: 2, pageSize: 20));

// Count
var count = await repository.CountAsync(
    new ActiveUsersSpec());

// Any
var hasAdmins = await repository.AnyAsync(
    new UsersByRoleSpec("Admin"));
```

### 3. Use in CQRS Handlers

```csharp
public class GetActiveUsersQueryHandler : CQRSQueryHandler<GetActiveUsersQuery, List<UserDto>>
{
    private readonly IRepository<User> _repository;
    private readonly IObjectMapper _mapper;

    public override async Task<List<UserDto>> Handle(
        GetActiveUsersQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ActiveUsersPaginatedSpec(
            query.PageNumber, 
            query.PageSize);

        var users = await _repository.ListAsync(spec, cancellationToken);
        
        return _mapper.Map<List<UserDto>>(users);
    }
}
```

## Common Patterns

### 1. Active Records (Soft Delete)

```csharp
public abstract class ActiveSpecification<T> : Specification<T> 
    where T : class, ISoftDeletable
{
    protected ActiveSpecification()
    {
        // All active specs automatically filter deleted
        Criteria = e => !e.IsDeleted;
    }
}

// Usage
public class ActiveProductsSpec : ActiveSpecification<Product>
{
    public ActiveProductsSpec()
    {
        // Inherits !IsDeleted filter
        AddInclude(p => p.Category);
        AddOrderBy(p => p.Name);
    }
}
```

### 2. Pagination

```csharp
public abstract class PaginatedSpecification<T> : Specification<T> 
    where T : class
{
    protected PaginatedSpecification(int pageNumber, int pageSize = 10)
    {
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

// Usage
public class ProductsPaginatedSpec : PaginatedSpecification<Product>
{
    public ProductsPaginatedSpec(int pageNumber)
        : base(pageNumber, pageSize: 20)
    {
        Criteria = p => !p.IsDeleted;
        AddOrderByDescending(p => p.CreatedAt);
    }
}
```

### 3. Complex Filtering

```csharp
public class ProductSearchSpec : Specification<Product>
{
    public ProductSearchSpec(string? category, decimal? minPrice, decimal? maxPrice, 
                           int pageNumber = 1, int pageSize = 20)
    {
        var criteria = PredicateBuilder.True<Product>();
        criteria = criteria.And(p => !p.IsDeleted && p.IsActive);
        
        if (!string.IsNullOrEmpty(category))
            criteria = criteria.And(p => p.Category == category);
        
        if (minPrice.HasValue)
            criteria = criteria.And(p => p.Price >= minPrice);
        
        if (maxPrice.HasValue)
            criteria = criteria.And(p => p.Price <= maxPrice);
        
        Criteria = criteria;
        AddInclude(p => p.Category);
        AddOrderByDescending(p => p.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
```

### 4. Include Navigation Properties

```csharp
public class OrderDetailsSpec : Specification<Order>
{
    public OrderDetailsSpec(Guid orderId)
    {
        Criteria = o => o.Id == orderId;
        
        // Single navigation
        AddInclude(o => o.User);
        
        // Collection navigation
        AddInclude(o => o.Items);
        
        // String-based for deeply nested
        AddIncludeString("User.Roles");
        AddIncludeString("Items.Product.Category");
    }
}
```

### 5. Read-Only Queries

```csharp
public class ReportProductsSpec : Specification<Product>
{
    public ReportProductsSpec()
    {
        Criteria = p => !p.IsDeleted;
        AddInclude(p => p.Category);
        AddOrderBy(p => p.Name);
        DisableTracking(); // Better performance for reports
    }
}
```

## API Reference

### ISpecification<T>

```csharp
// WHERE clause
Expression<Func<T, bool>>? Criteria { get; }

// INCLUDE navigation properties
IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

// String-based includes for complex paths
IReadOnlyList<string> IncludeStrings { get; }

// ORDER BY
Expression<Func<T, object>>? OrderBy { get; }
Expression<Func<T, object>>? OrderByDescending { get; }

// SKIP/TAKE
bool IsPagingEnabled { get; }
int Skip { get; }
int Take { get; }

// Change tracking (default: true)
bool IsTrackingEnabled { get; }
```

### Specification<T> Methods

```csharp
// Add include for eager loading
protected void AddInclude(Expression<Func<T, object>> includeExpression);

// Add string-based include
protected void AddIncludeString(string includeString);

// Set primary order by
protected void AddOrderBy(Expression<Func<T, object>> orderByExpression);

// Set descending order by
protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression);

// Apply pagination (skip, take)
protected void ApplyPaging(int skip, int take);

// Disable entity tracking for read-only
protected void DisableTracking();
```

### Repository Extensions

```csharp
// Get single or default
Task<T?> FirstOrDefaultAsync<T>(ISpecification<T> spec);

// Get list
Task<List<T>> ListAsync<T>(ISpecification<T> spec);

// Get count
Task<int> CountAsync<T>(ISpecification<T> spec);

// Check existence
Task<bool> AnyAsync<T>(ISpecification<T> spec);
```

## Best Practices

### ? Do

```csharp
// Use hierarchical names
public class ActiveProductsSpec : Specification<Product> { }
public class ProductsByPriceRangeSpec : Specification<Product> { }

// Inherit from base specs
public class FeaturedProductsSpec : ActiveSpecification<Product> { }

// Use DisableTracking for read-only
public class ProductReportSpec : Specification<Product>
{
    public ProductReportSpec()
    {
        DisableTracking();
    }
}

// Document your specs
/// <summary>
/// Gets all active products with pagination and reviews
/// </summary>
public class ActiveProductsPaginatedSpec : Specification<Product> { }

// Use in CQRS handlers
public class GetProductsQueryHandler : CQRSQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public override async Task<List<ProductDto>> Handle(
        GetProductsQuery query, 
        CancellationToken cancellationToken)
    {
        var spec = new ProductsByPriceRangeSpec(
            query.MinPrice, 
            query.MaxPrice,
            query.PageNumber);
        
        var products = await _repository.ListAsync(spec, cancellationToken);
        return _mapper.Map<List<ProductDto>>(products);
    }
}
```

### ? Don't

```csharp
// Don't create a spec for every tiny query
public class OneTimeSpec : Specification<Product> { }

// Don't put complex business logic in specs
public class WeirdSpec : Specification<Product>
{
    public WeirdSpec()
    {
        // This belongs in domain logic, not here!
        Criteria = p => CalculateComplexBusinessRules(p);
    }
}

// Don't ignore includes (causes N+1 queries)
public class BadSpec : Specification<Product>
{
    public BadSpec()
    {
        Criteria = p => !p.IsDeleted;
        // Forgot to include reviews!
    }
}

// Don't use tracking for read-only queries
var products = await _repository.ListAsync(spec);
// Should have called DisableTracking()
```

## Examples

See `Examples.cs` for complete working examples:
- `AllActiveUsersSpec`
- `UsersByRoleSpec`
- `ActiveUsersPaginatedSpec`
- `ProductsByCategorySpec`
- `FeaturedProductsSpec`
- `UserCompletedOrdersSpec`
- `PendingOrdersSpec`

## Performance Tips

### 1. Use DisableTracking for Read-Only

```csharp
// Reports, exports, read-only pages
public class ProductReportSpec : Specification<Product>
{
    public ProductReportSpec()
    {
        DisableTracking();
    }
}
```

### 2. Include Related Data (Avoid N+1)

```csharp
// Bad: Causes N+1 queries
var users = await repository.ListAsync(new AllUsersSpec());
foreach (var user in users)
{
    var roles = user.Roles; // Query per user!
}

// Good: Include in spec
public class AllUsersWithRolesSpec : Specification<User>
{
    public AllUsersWithRolesSpec()
    {
        AddInclude(u => u.Roles);
    }
}
```

### 3. Project to DTOs Early

```csharp
// In CQRS handler
var spec = new ActiveProductsSpec();
var products = await _repository.ListAsync(spec);
var dtos = _mapper.Map<List<ProductDto>>(products);
```

## Extending Specifications

### Custom Base Classes

```csharp
// For specific domain
public abstract class TenantSpecification<T> : Specification<T> 
    where T : class, ITenant
{
    protected TenantSpecification(Guid tenantId)
    {
        Criteria = e => e.TenantId == tenantId;
    }
}

// Usage
public class TenantActiveProductsSpec : TenantSpecification<Product>
{
    public TenantActiveProductsSpec(Guid tenantId) 
        : base(tenantId)
    {
        AddInclude(p => p.Category);
    }
}
```

## Testing Specifications

```csharp
[TestClass]
public class ProductSpecTests
{
    [TestMethod]
    public void ActiveProductsSpec_HasCorrectCriteria()
    {
        var spec = new ActiveProductsSpec();
        
        Assert.IsNotNull(spec.Criteria);
        Assert.IsTrue(spec.Includes.Count > 0);
    }

    [TestMethod]
    public async Task ListAsyncWithSpec_ReturnsCorrectResults()
    {
        var spec = new ProductsByCategorySpec("Electronics");
        var products = await _repository.ListAsync(spec);
        
        Assert.IsTrue(products.All(p => p.Category == "Electronics"));
    }
}
```

## FAQ

**Q: When should I use Specifications?**  
A: For any query that's used more than once. They make shared queries explicit and testable.

**Q: Can I combine multiple specs?**  
A: Create composite specs that inherit from multiple base specs or build complex Criteria using PredicateBuilder.

**Q: Does DisableTracking affect updates?**  
A: Yes! Only disable for read-only queries. If you might modify entities, keep tracking enabled.

**Q: How do I test specifications?**  
A: Unit test the criteria logic, integration test with actual repository.

---

**Specification Pattern brings DDD principles to your queries.** Define once, use everywhere! ??
