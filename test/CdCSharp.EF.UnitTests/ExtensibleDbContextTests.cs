using CdCSharp.EF.Features;
using CdCSharp.EF.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CdCSharp.EF.UnitTests;

public class ExtensibleDbContextTests : IDisposable
{
    private readonly TestExtensibleDbContext _context;
    private readonly List<Mock<IFeatureProcessor>> _mockProcessors;
    private readonly IServiceProvider _serviceProvider;

    public ExtensibleDbContextTests()
    {
        ServiceCollection services = new();

        // Crear mocks de procesadores
        _mockProcessors = new List<Mock<IFeatureProcessor>>
        {
            new(),
            new()
        };

        // Registrar features por defecto
        services.AddSingleton(DbContextFeatures.Default);

        // Registrar los mocks como servicios
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
        {
            services.AddScoped<IFeatureProcessor>(_ => mockProcessor.Object);
        }

        _serviceProvider = services.BuildServiceProvider();

        DbContextOptions<TestExtensibleDbContext> options = new DbContextOptionsBuilder<TestExtensibleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Resolver los procesadores a través del contenedor de DI
        IEnumerable<IFeatureProcessor> processors = _serviceProvider.GetServices<IFeatureProcessor>();
        _context = new TestExtensibleDbContext(options, _serviceProvider);
    }

    [Fact]
    public async Task SaveChangesAsync_CallsAllProcessors()
    {
        // Arrange
        TestProduct entity = new() { Name = "Test" };
        _context.Products.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
        {
            mockProcessor.Verify(p => p.OnSaveChanges(_context.ChangeTracker), Times.Once);
        }
    }

    [Fact]
    public void SaveChanges_CallsAllProcessors()
    {
        // Arrange
        TestProduct entity = new() { Name = "Test" };
        _context.Products.Add(entity);

        // Act
        _context.SaveChanges();

        // Assert
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
        {
            mockProcessor.Verify(p => p.OnSaveChanges(_context.ChangeTracker), Times.Once);
        }
    }

    [Fact]
    public void OnModelCreating_CallsAllProcessorsForEachEntity()
    {
        // Arrange
        _context.Database.EnsureCreated();

        // Assert
        foreach (Mock<IFeatureProcessor> mockProcessor in _mockProcessors)
        {
            mockProcessor.Verify(p => p.OnModelCreating(
                It.IsAny<ModelBuilder>(),
                typeof(TestProduct)), Times.Once);
        }
    }

    public void Dispose() => _context.Dispose();
}
