namespace CdCSharp.EF.Core.Abstractions;

public interface IAuditableEntity
{
    DateTime CreatedDate { get; set; }
    DateTime LastModifiedDate { get; set; }
}

public interface IAuditableWithUserEntity : IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
}
