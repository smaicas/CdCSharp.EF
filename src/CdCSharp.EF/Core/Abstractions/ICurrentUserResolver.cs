namespace CdCSharp.EF.Core.Abstractions;

public interface ICurrentUserResolver
{
    Task<string?> ResolveCurrentUserIdAsync();
}
