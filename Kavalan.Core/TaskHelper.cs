using System.Diagnostics;

namespace Kavalan.Core
{
    public static class TaskExtensions
    {
        public static async Task<(T result, double elapsedMilliSeconds)> TimeAsync<T>(this Task<T> task)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            T result = await task;
            stopwatch.Stop();

            return (result, stopwatch.Elapsed.TotalMilliseconds);
        }
        public static async Task<double> TimeOnlyAsync(this Task task)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await task;
            stopwatch.Stop();

            return stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}
