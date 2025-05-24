using CdCSharp.EF.Features.Auditing;

namespace CdCSharp.EF.UnitTests.Features.Auditing;

public class AuditingConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        AuditingConfiguration config = new();

        // Assert
        Assert.Equal(AuditingBehavior.SaveAsNull, config.BehaviorWhenNoUser);
        Assert.Equal("system", config.DefaultUserId);
    }

    [Fact]
    public void BehaviorWhenNoUser_CanBeSet()
    {
        // Arrange
        AuditingConfiguration config = new()
        {
            // Act & Assert
            BehaviorWhenNoUser = AuditingBehavior.ThrowException
        };
        Assert.Equal(AuditingBehavior.ThrowException, config.BehaviorWhenNoUser);

        config.BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser;
        Assert.Equal(AuditingBehavior.UseDefaultUser, config.BehaviorWhenNoUser);

        config.BehaviorWhenNoUser = AuditingBehavior.SaveAsNull;
        Assert.Equal(AuditingBehavior.SaveAsNull, config.BehaviorWhenNoUser);

        config.BehaviorWhenNoUser = AuditingBehavior.SkipUserFields;
        Assert.Equal(AuditingBehavior.SkipUserFields, config.BehaviorWhenNoUser);
    }

    [Fact]
    public void DefaultUserId_CanBeSet()
    {
        // Arrange
        AuditingConfiguration config = new()
        {
            // Act
            DefaultUserId = "ADMIN"
        };

        // Assert
        Assert.Equal("ADMIN", config.DefaultUserId);
    }

    [Fact]
    public void DefaultUserId_CanBeSetToNull()
    {
        // Arrange
        AuditingConfiguration config = new()
        {
            // Act
            DefaultUserId = null
        };

        // Assert
        Assert.Null(config.DefaultUserId);
    }

    [Fact]
    public void AllBehaviorValues_AreValid()
    {
        // Arrange
        AuditingConfiguration config = new();
        AuditingBehavior[] allBehaviors = Enum.GetValues<AuditingBehavior>();

        // Act & Assert
        foreach (AuditingBehavior behavior in allBehaviors)
        {
            config.BehaviorWhenNoUser = behavior;
            Assert.Equal(behavior, config.BehaviorWhenNoUser);
        }
    }
}
