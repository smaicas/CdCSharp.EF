using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_ExtensibleDbContext;

public class ExtensibleDbContext_DbContext : ExtensibleDbContext
{
    public ExtensibleDbContext_DbContext(DbContextOptions<ExtensibleDbContext_DbContext> options,
        IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<ExtensibleDbContext_Product> Products { get; set; } = null!;
}

public class ExtensibleDbContext_Product : IAuditableWithUserEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    // IAuditableWithUserEntity
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

public class ExtensibleDbContext_CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}
