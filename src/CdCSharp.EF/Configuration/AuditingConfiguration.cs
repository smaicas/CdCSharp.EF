namespace CdCSharp.EF.Configuration;

public enum AuditingBehavior
{
    ThrowException,      // Throws exception if can't resolve user.
    UseDefaultUser,      // Uses default user if can't resolve user.
    SaveAsNull,          // Saves null if can't resolve user.
    SkipUserFields       // Skip user fields leaving them as they are if can't resolve user.
}

public class AuditingConfiguration
{
    public AuditingBehavior BehaviorWhenNoUser { get; set; } = AuditingBehavior.SaveAsNull;
    public string? DefaultUserId { get; set; } = "system";

    public static AuditingConfiguration Default => new();

    public static AuditingConfiguration ThrowOnMissingUser => new()
    {
        BehaviorWhenNoUser = AuditingBehavior.ThrowException
    };

    public static AuditingConfiguration UseSystemUser(string systemUserId = "system") => new()
    {
        BehaviorWhenNoUser = AuditingBehavior.UseDefaultUser,
        DefaultUserId = systemUserId
    };

    public static AuditingConfiguration SkipUserFields => new()
    {
        BehaviorWhenNoUser = AuditingBehavior.SkipUserFields
    };
}
