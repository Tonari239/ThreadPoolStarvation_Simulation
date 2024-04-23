using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Threading.Channels;
using Threadpool_Starvation_Simulation.Services;

namespace Threadpool_Starvation_Simulation
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var host = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .ConfigureServices(services =>
                {
                    if (SimulationOptions.IsUsingNonBlocking)
                    {
                        services.AddHostedService<DummyNonBlockingDatabaseService>();
                    }
                    else
                    {
                        services.AddHostedService<DummyDatabaseService>();
                    }
                    
                })
             .Build();

            host.Run();
        }
    }
}
