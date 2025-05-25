using CdCSharp.EF.Core;
using CdCSharp.EF.Core.Abstractions;
using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests.Core;

public class ExtensibleDbContextTests : IDisposable
{
    private readonly ExtensibleDbContextTests_ExtensibleDbContext _context;
    private readonly List<Mock<IFeatureProcessor>> _mockProcessors;
    private readonly IServiceProvider _serviceProvider;

    public ExtensibleDbContextTests()
    {
        ServiceCollection services = new();

        Mock<IFeatureProcessor> mockProcessor1 = new(MockBehavior.Strict);
        Mock<IFeatureProcessor> mockProcessor2 = new(MockBehavior.Strict);

        mockProcessor1
            .Setup(p => p.OnModelCreating(It.IsAny<ModelBuilder>()))
            .Verifiable();

        mockProcessor2
            .Setup(p => p.OnModelCreating(It.IsAny<ModelBuilder>()))
            .Verifiable();

        mockProcessor1
            .Setup(p => p.OnModelCreatingEntity(It.IsAny<ModelBuilder>(), It.IsAny<Type>(), It.IsAny<ExtensibleDbContext>()))
            .Verifiable();

        mockProcessor2
            .Setup(p => p.OnModelCreatingEntity(It.IsAny<ModelBuilder>(), It.IsAny<Type>(), It.IsAny<ExtensibleDbContext>()))
            .Verifiable();

        mockProcessor1
            .Setup(p => p.OnSaveChanges(It.IsAny<ChangeTracker>()))
            .Verifiable();

        mockProcessor2
            .Setup(p => p.OnSaveChanges(It.IsAny<ChangeTracker>()))
            .Verifiable();

        // Crear mocks de procesadores
        _mockProcessors = new List<Mock<IFeatureProcessor>>
        {
            mockProcessor1,
            mockProcessor2
        };

        // Registrar features por defecto
        services.AddSingleton(new DbContextFeatures());

        // Registrar los mocks como servicios
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
            services.AddScoped(_ => mockProcessor.Object);

        _serviceProvider = services.BuildServiceProvider();

        DbContextOptions<ExtensibleDbContextTests_ExtensibleDbContext> options = new DbContextOptionsBuilder<ExtensibleDbContextTests_ExtensibleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Resolver los procesadores a través del contenedor de DI
        _context = new ExtensibleDbContextTests_ExtensibleDbContext(options, _serviceProvider);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCalled_CallsAllProcessors()
    {
        // Arrange
        ExtensibleDbContextTests_TenantEntity entity = new() { Name = "Test" };
        _context.Products.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
            mockProcessor.Verify(p => p.OnSaveChanges(_context.ChangeTracker), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenCalled_CallsAllProcessors()
    {
        // Arrange
        ExtensibleDbContextTests_TenantEntity entity = new() { Name = "Test" };
        _context.Products.Add(entity);

        // Act
        _context.SaveChanges();

        // Assert
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
            mockProcessor.Verify(p => p.OnSaveChanges(_context.ChangeTracker), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
            mockProcessor.Reset();
    }

    internal class ExtensibleDbContextTests_ExtensibleDbContext : ExtensibleDbContext
    {
        public ExtensibleDbContextTests_ExtensibleDbContext(DbContextOptions<ExtensibleDbContextTests_ExtensibleDbContext> options,
            IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<ExtensibleDbContextTests_TenantEntity> Products { get; set; } = null!;
    }

    internal class ExtensibleDbContextTests_TenantEntity : ITenantEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }
}