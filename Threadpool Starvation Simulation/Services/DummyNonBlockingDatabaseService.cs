using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Threadpool_Starvation_Simulation.Services
{
    internal class DummyNonBlockingDatabaseService : BackgroundService
    {
        private int failedDbOperations = 0;
        private int timeoutedDbOperations = 0;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => LaunchRequestsAsync());
        }

        private void LaunchRequestsAsync()
        {
            Console.WriteLine("Testing app just started");
            Console.WriteLine($"Threadpool count is: {ThreadPool.ThreadCount}");

            var allRequestsTimer = new Stopwatch();
            allRequestsTimer.Start();

            var tasks = Enumerable.Range(0, SimulationOptions.RequestsCount)
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(number => Task.Run(async () =>
                {
                    var singleRequestTimer = new Stopwatch();
                    singleRequestTimer.Start();

                    Console.WriteLine($"Before request start for thread: {Thread.CurrentThread.ManagedThreadId}");
                    try
                    {
                        await StartNewAsync(DummyDatabaseOperation);
                        singleRequestTimer.Stop();
                        Console.WriteLine($"After request end for thread {Thread.CurrentThread.ManagedThreadId} and it took {singleRequestTimer.ElapsedMilliseconds}ms");
                    }
                    catch (TimeOutException)
                    {
                        ++timeoutedDbOperations;
                        Console.WriteLine("TIMEOUT ERROR");
                    }
                    catch (Exception)
                    {
                        ++failedDbOperations;
                        Console.WriteLine("Not timeout but different exception");
                    }
                }))
                .ToArray();


            Task.WaitAll(tasks);

            allRequestsTimer.Stop();

            Console.WriteLine($"All requests completed, it took them a total of {allRequestsTimer.ElapsedMilliseconds} ms.");
            Console.WriteLine($"And threadpool count is: {ThreadPool.ThreadCount}");
            Console.WriteLine("Failed database operations: " + failedDbOperations);
            Console.WriteLine("Timeouted database operations: " + timeoutedDbOperations);
        }

        private async Task StartNewAsync(Action actionForExecuting)
        {

            var task = Task.Run(actionForExecuting);

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SimulationOptions.MaxSeconds));

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
            {
                throw new TimeOutException("Database operation timeout! Took more than " + SimulationOptions.MaxSeconds);
            }
        }

        private void DummyDatabaseOperation()
        {
            Console.WriteLine($"Dummy database operation. Thread executing the select: {Thread.CurrentThread.ManagedThreadId} | isThreadPoolThread: {Thread.CurrentThread.IsThreadPoolThread}");
            Thread.Sleep(SimulationOptions.MinSeconds * 1000);
            Console.WriteLine($"Dummy select ended. Thread executing the select: {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}
