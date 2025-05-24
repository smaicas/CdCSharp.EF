using CdCSharp.EF.Features.Auditing;

namespace CdCSharp.EF.UnitTests.Features.Auditing;

public class AuditingFeatureTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        AuditingFeature feature = new();

        // Assert
        Assert.False(feature.Enabled);
        Assert.NotNull(feature.Configuration);
        Assert.IsType<AuditingConfiguration>(feature.Configuration);
    }

    [Fact]
    public void Enabled_CanBeToggled()
    {
        // Arrange
        AuditingFeature feature = new();

        // Act & Assert
        Assert.False(feature.Enabled);

        feature.Enabled = true;
        Assert.True(feature.Enabled);

        feature.Enabled = false;
        Assert.False(feature.Enabled);
    }

    [Fact]
    public void Configuration_CanBeModified()
    {
        // Arrange
        AuditingFeature feature = new();
        AuditingConfiguration newConfig = new()
        {
            BehaviorWhenNoUser = AuditingBehavior.ThrowException,
            DefaultUserId = "CUSTOM_SYSTEM"
        };

        // Act
        feature.Configuration = newConfig;

        // Assert
        Assert.Same(newConfig, feature.Configuration);
        Assert.Equal(AuditingBehavior.ThrowException, feature.Configuration.BehaviorWhenNoUser);
        Assert.Equal("CUSTOM_SYSTEM", feature.Configuration.DefaultUserId);
    }

    [Fact]
    public void Configuration_HasCorrectDefaultValues()
    {
        // Arrange & Act
        AuditingFeature feature = new();

        // Assert
        Assert.Equal(AuditingBehavior.SaveAsNull, feature.Configuration.BehaviorWhenNoUser);
        Assert.Equal("system", feature.Configuration.DefaultUserId);
    }
}
