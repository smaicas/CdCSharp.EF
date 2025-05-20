using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CdCSharp.EF.IntegrationTests;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Configuración básica requerida
        builder.Services.AddControllers();

        WebApplication app = builder.Build();

        // Pipeline básico
        app.UseRouting();
        app.MapControllers();

        app.Run();
    }
}
