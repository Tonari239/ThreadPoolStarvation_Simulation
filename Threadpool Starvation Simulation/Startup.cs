using Ben.Diagnostics;
using Microsoft.AspNetCore.Builder;
using System.Diagnostics;

namespace Threadpool_Starvation_Simulation
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
           app.UseBlockingDetection();
        }
    }
}
