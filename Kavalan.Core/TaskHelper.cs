using System.Diagnostics;

namespace Kavalan.Core
{
    public static class TaskExtensions
    {
        public static async Task<(T result, long elapsedMilliSeconds)> TimeAsync<T>(this Task<T> task)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            T result = await task;
            stopwatch.Stop();

            return (result, stopwatch.ElapsedMilliseconds);
        }
        public static async Task<long> TimeOnlyAsync(this Task task)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await task;
            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }
    }
}
