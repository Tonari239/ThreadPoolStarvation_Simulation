using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Threadpool_Starvation_Simulation.Services
{

    internal class DummyNonBlockingDatabaseService : BackgroundService
    {
        private int timeoutedDbOperations = 0;
        SortedDictionary<int, int> deltas = new SortedDictionary<int, int>(); // key - the TP delta, value - how many times the delta occurs

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
                        int tpCountBefore = ThreadPool.ThreadCount;
                        await StartNewAsync(DummyDatabaseOperation);

                        int threadpoolDelta = ThreadPool.ThreadCount - tpCountBefore;
                        if (threadpoolDelta < 0) return;

                        if (!deltas.ContainsKey(threadpoolDelta))
                        {
                            deltas.Add(threadpoolDelta, 1); // first occurence;
                        }
                        else
                        {
                            deltas[threadpoolDelta] = deltas[threadpoolDelta] + 1;
                        }
                    }
                    catch (TimeOutException)
                    {
                        ++timeoutedDbOperations;
                        Console.WriteLine("TIMEOUT ERROR");
                    }

                    singleRequestTimer.Stop();
                    Console.WriteLine($"After request end for thread {Thread.CurrentThread.ManagedThreadId} and it took {singleRequestTimer.ElapsedMilliseconds}ms");
                }))
                .ToArray();


            Task.WaitAll(tasks);

            allRequestsTimer.Stop();

            Console.WriteLine($"All requests completed, it took them a total of {allRequestsTimer.ElapsedMilliseconds} ms.");
            Console.WriteLine("Timeouted database operations: " + timeoutedDbOperations);

            Console.WriteLine("Top 5 most occuring threadpool deltas:");
            PrintTopFive(deltas.OrderByDescending((pair) => pair.Value));

            Console.WriteLine("Top 5 highest threadpool deltas:");
            PrintTopFive(deltas.OrderByDescending((pair) => pair.Key));
        }

        private async Task StartNewAsync(Action actionForExecuting)
        {
            Console.WriteLine("StartNew just started");

            var task = Task.Run(actionForExecuting);

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SimulationOptions.MaxSeconds));

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
            {
                throw new TimeOutException("Database operation timeout! Took more than " + SimulationOptions.MaxSeconds);
            }

            Console.WriteLine("StartNew just ended");
        }

        private void DummyDatabaseOperation()
        {
            Console.WriteLine($"Dummy database operation. Thread executing the select: {Thread.CurrentThread.ManagedThreadId} | isThreadPoolThread: {Thread.CurrentThread.IsThreadPoolThread}");
            Thread.Sleep(SimulationOptions.MinSeconds * 1000);
            Console.WriteLine($"Dummy select ended. Thread executing the select: {Thread.CurrentThread.ManagedThreadId}");
        }

        private void PrintTopFive(IOrderedEnumerable<KeyValuePair<int, int>> data)
        {
            var enumerator = data.GetEnumerator();
            enumerator.MoveNext();
            for (int i = 0; i < 5; i++)
            {
                KeyValuePair<int, int> entry = enumerator.Current;
                Console.WriteLine("Threadpool Delta: " + entry.Key + " Count: " + entry.Value);
                enumerator.MoveNext();
            }
        }
    }
}
