namespace CdCSharp.EF.Features.Auditing;

public class AuditingFeature
{
    public bool Enabled { get; set; } = false;
    public AuditingConfiguration Configuration { get; set; } = new();
}

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

}
