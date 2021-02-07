using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                b.Add(new ServiceDescriptor(typeof(IProductTransmissionStreamReader), new ProductTransmissionStreamReader()));
            });

            var host = builder.Build();
            using (host)
            {
                host.Run();
            }
        }
    }
}
