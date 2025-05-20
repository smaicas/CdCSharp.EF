using CdCSharp.EF.Configuration;

namespace CdCSharp.EF.Features;

public class DbContextFeatures
{
    public bool AuditingEnabled { get; set; }
    public AuditingConfiguration AuditingConfiguration { get; set; } = AuditingConfiguration.Default;

    public static DbContextFeatures Default => new()
    {
        AuditingEnabled = false
    };

    public static DbContextFeatures WithAuditing => new()
    {
        AuditingEnabled = true
    };
}

public class DbContextFeaturesBuilder
{
    private readonly DbContextFeatures _features = new();

    public DbContextFeaturesBuilder EnableAuditing(Action<AuditingConfiguration>? configureAuditing = null)
    {
        _features.AuditingEnabled = true;

        configureAuditing?.Invoke(_features.AuditingConfiguration);

        return this;
    }

    public DbContextFeatures Build() => _features;
}
