using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.Features.MultiTenant;

public class MultiTenantFeature
{
    public bool Enabled { get; set; } = false;
    public MultiTenantConfiguration Configuration { get; set; } = new();
}

public class MultiTenantConfiguration
{
    public MultiTenantStrategy Strategy { get; set; } = MultiTenantStrategy.Discriminator;
    public Dictionary<string, Action<DbContextOptionsBuilder>> DatabaseConfigurations { get; set; } = new();
    public Action<DbContextOptionsBuilder>? DiscriminatorConfiguration { get; set; }
}

public enum MultiTenantStrategy
{
    Discriminator, // default(MultiTenantStrategy)
    Database,
}