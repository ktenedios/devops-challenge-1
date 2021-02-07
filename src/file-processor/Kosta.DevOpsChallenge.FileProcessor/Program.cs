using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Kosta.DevOpsChallenge.FileProcessor.WarehouseDb;
using System;
using Microsoft.EntityFrameworkCore;

namespace Kosta.DevOpsChallenge.FileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new HostBuilder();
            builder.UseEnvironment(EnvironmentName.Development);
            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
                b.AddAzureStorage();
            });

            builder.ConfigureLogging((context, b) =>
            {
                b.AddConsole();
                b.AddApplicationInsightsWebJobs();
            });

            builder.ConfigureServices((context, b) =>
            {
                b.AddScoped<IProductTransmissionStreamReader, ProductTransmissionStreamReader>();
                b.AddScoped<IWarehouseService, WarehouseService>();
                
                var connectionString = Environment.GetEnvironmentVariable("WarehouseSqlConnectionString");
                b.AddDbContext<WarehouseContext>(options =>
                    SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString));
            });

            var host = builder.Build();
            using (host)
            {
                host.Run();
            }
        }
    }
}
