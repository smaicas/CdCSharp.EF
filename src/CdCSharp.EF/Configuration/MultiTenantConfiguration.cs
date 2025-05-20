using CdCSharp.EF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Configuration;

public class MultiTenantConfiguration<TContext> where TContext : DbContext
{
    public MultiTenantStrategy Strategy { get; set; }
    public Action<DbContextOptionsBuilder<TContext>>? DiscriminatorConfiguration { get; set; }
    public Dictionary<string, Action<DbContextOptionsBuilder<TContext>>> DatabaseConfigurations { get; set; } = new();
}
