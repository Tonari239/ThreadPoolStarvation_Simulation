using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadpool_Starvation_Simulation
{
    internal class TimeOutException : Exception
    {
        public TimeOutException(string message) : base(message)
        {
            
        }
    }
}
