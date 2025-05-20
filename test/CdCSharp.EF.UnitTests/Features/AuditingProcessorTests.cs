using CdCSharp.EF.Configuration;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests.Features;

public class AuditingProcessorTests : IDisposable
{
    private readonly AuditingProcessorTests_DbContext _context;
    private readonly AuditingProcessor _processor;
    private readonly Mock<ICurrentUserStore> _mockUserStore;
    private readonly ServiceProvider _serviceProvider;

    public AuditingProcessorTests()
    {
        _mockUserStore = new Mock<ICurrentUserStore>();

        ServiceCollection services = new();
        services.AddSingleton(_mockUserStore.Object);
        _serviceProvider = services.BuildServiceProvider();

        DbContextOptions<AuditingProcessorTests_DbContext> options = new DbContextOptionsBuilder<AuditingProcessorTests_DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuditingProcessorTests_DbContext(options);
        _processor = new AuditingProcessor(AuditingConfiguration.Default, _serviceProvider);
    }

    [Fact]
    public void OnSaveChanges_WhenAddingAuditableEntity_SetsAuditFields()
    {
        // Arrange
        AuditingProcessorTests_AuditableEntity entity = new() { Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;

        // Act
        _processor.OnSaveChanges(_context.ChangeTracker);

        // Assert
        Assert.True(entity.CreatedDate > DateTime.MinValue);
        Assert.True(entity.LastModifiedDate > DateTime.MinValue);
        Assert.Equal(entity.CreatedDate, entity.LastModifiedDate);
    }

    [Fact]
    public void OnSaveChanges_WhenModifyingAuditableEntity_UpdatesLastModifiedDate()
    {
        // Arrange
        AuditingProcessorTests_AuditableEntity entity = new() { Name = "Test" };
        DateTime originalDate = DateTime.UtcNow.AddHours(-1);
        entity.CreatedDate = originalDate;
        entity.LastModifiedDate = originalDate;

        _context.Entry(entity).State = EntityState.Modified;

        // Act
        _processor.OnSaveChanges(_context.ChangeTracker);

        // Assert
        Assert.Equal(originalDate, entity.CreatedDate);
        Assert.True(entity.LastModifiedDate > originalDate);
    }

    [Fact]
    public void OnSaveChanges_WhenAddingAuditableWithUserEntity_SetsUserFields()
    {
        // Arrange
        const string currentUserId = "user123";
        _mockUserStore.Setup(s => s.GetCurrentUserId()).Returns(currentUserId);

        AuditingProcessorTests_AuditableWithUserEntity entity = new() { Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;

        // Act
        _processor.OnSaveChanges(_context.ChangeTracker);

        // Assert
        Assert.Equal(currentUserId, entity.CreatedBy);
        Assert.Equal(currentUserId, entity.ModifiedBy);
    }

    [Fact]
    public void OnSaveChanges_WhenNoUserAndThrowOnMissingUser_ThrowsException()
    {
        // Arrange
        AuditingConfiguration config = AuditingConfiguration.ThrowOnMissingUser;
        AuditingProcessor processor = new(config, _serviceProvider);
        _mockUserStore.Setup(s => s.GetCurrentUserId()).Returns((string?)null);

        AuditingProcessorTests_AuditableWithUserEntity entity = new() { Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            processor.OnSaveChanges(_context.ChangeTracker));

        Assert.Contains("Current user ID is required", exception.Message);
    }

    [Fact]
    public void OnSaveChanges_WhenNoUserAndUseDefaultUser_UsesDefaultUser()
    {
        // Arrange
        AuditingConfiguration config = AuditingConfiguration.UseSystemUser("SYSTEM");
        AuditingProcessor processor = new(config, _serviceProvider);
        _mockUserStore.Setup(s => s.GetCurrentUserId()).Returns((string?)null);

        AuditingProcessorTests_AuditableWithUserEntity entity = new() { Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;

        // Act
        processor.OnSaveChanges(_context.ChangeTracker);

        // Assert
        Assert.Equal("SYSTEM", entity.CreatedBy);
        Assert.Equal("SYSTEM", entity.ModifiedBy);
    }

    [Fact]
    public void OnSaveChanges_WhenNoUserAndSaveAsNull_SetsNullUser()
    {
        // Arrange
        AuditingConfiguration config = new() { BehaviorWhenNoUser = AuditingBehavior.SaveAsNull };
        AuditingProcessor processor = new(config, _serviceProvider);
        _mockUserStore.Setup(s => s.GetCurrentUserId()).Returns((string?)null);

        AuditingProcessorTests_AuditableWithUserEntity entity = new() { Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;

        // Act
        processor.OnSaveChanges(_context.ChangeTracker);

        // Assert
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.ModifiedBy);
    }

    [Fact]
    public void OnSaveChanges_WhenNoUserAndSkipUserFields_DoesNotModifyUserFields()
    {
        // Arrange
        AuditingConfiguration config = AuditingConfiguration.SkipUserFields;
        AuditingProcessor processor = new(config, _serviceProvider);
        _mockUserStore.Setup(s => s.GetCurrentUserId()).Returns((string?)null);

        AuditingProcessorTests_AuditableWithUserEntity entity = new()
        {
            Name = "Test",
            CreatedBy = "OriginalUser",
            ModifiedBy = "OriginalUser"
        };
        _context.Entry(entity).State = EntityState.Added;

        // Act
        processor.OnSaveChanges(_context.ChangeTracker);

        // Assert
        Assert.Equal("OriginalUser", entity.CreatedBy);
        Assert.Equal("OriginalUser", entity.ModifiedBy);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    internal class AuditingProcessorTests_DbContext : DbContext
    {
        public AuditingProcessorTests_DbContext(DbContextOptions<AuditingProcessorTests_DbContext> options) : base(options) { }
        public DbSet<AuditingProcessorTests_AuditableEntity> AuditableEntities { get; set; }
        public DbSet<AuditingProcessorTests_AuditableWithUserEntity> AuditableWithUserEntities { get; set; }
    }

    internal class AuditingProcessorTests_AuditableEntity : IAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }

    internal class AuditingProcessorTests_AuditableWithUserEntity : IAuditableWithUserEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
    }

}