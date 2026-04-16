using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ArandanoIRT.Web._2_Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Obtener el entorno (Development, Production, etc.)
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Construir la configuración leyendo los archivos appsettings
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // Establece la ruta base al directorio principal del proyecto web
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory()))
                // Carga el appsettings.json principal
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                // Carga el appsettings.{Entorno}.json (ej. appsettings.Development.json)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Obtener la cadena de conexión
            var connectionString = configuration.GetConnectionString("PostgresConnection");

            // Si la cadena de conexión es nula o vacía, lanza un error claro.
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("No se encontró la cadena de conexión 'PostgresConnection' en los archivos de configuración.");
            }

            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}