using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadpool_Starvation_Simulation
{
    public static class SimulationOptions
    {
        public static int RequestsCount { get; } = 20000;
        public static int MinSeconds { get; } = 30;
        public static int MaxSeconds { get; } = 35;
        public static bool IsUsingNonBlocking { get; } = false;
    }
}
