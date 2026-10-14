using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace EAP.Application.Logging;

public sealed class MethodTimingScope : IDisposable
{
    private const string ActivityPropertyName = "EAP.MethodTimings";

    private readonly Activity? _activity;
    private readonly string _layer;
    private readonly string _methodName;
    private readonly string? _resourceName;
    private readonly DateTimeOffset _startedAtUtc;
    private readonly long _started;
    private bool _disposed;

    private MethodTimingScope(string layer, string methodName, string? resourceName)
    {
        _activity = Activity.Current;
        _layer = layer;
        _methodName = methodName;
        _resourceName = resourceName;
        _startedAtUtc = DateTimeOffset.UtcNow;
        _started = Stopwatch.GetTimestamp();
    }

    public static MethodTimingScope Start(string layer, string methodName, string? resourceName = null)
    {
        return new MethodTimingScope(layer, methodName, resourceName);
    }

    public static MethodTimingScope Start(string methodName, [CallerFilePath] string callerFilePath = "")
    {
        var resourceName = Path.GetFileNameWithoutExtension(callerFilePath);
        return new MethodTimingScope("Application", methodName, resourceName);
    }

    public static IReadOnlyList<MethodTimingEntry> GetTimings(Activity? activity)
    {
        if (activity?.GetCustomProperty(ActivityPropertyName) is not ConcurrentQueue<MethodTimingEntry> timings)
        {
            return Array.Empty<MethodTimingEntry>();
        }

        return timings.OrderBy(timing => timing.StartedAtUtc).ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_activity is null)
        {
            return;
        }

        var entry = new MethodTimingEntry(
            _layer,
            _methodName,
            _resourceName,
            _startedAtUtc,
            Stopwatch.GetElapsedTime(_started).TotalMilliseconds);

        GetOrCreateQueue(_activity).Enqueue(entry);
    }

    private static ConcurrentQueue<MethodTimingEntry> GetOrCreateQueue(Activity activity)
    {
        if (activity.GetCustomProperty(ActivityPropertyName) is ConcurrentQueue<MethodTimingEntry> existingQueue)
        {
            return existingQueue;
        }

        lock (activity)
        {
            if (activity.GetCustomProperty(ActivityPropertyName) is ConcurrentQueue<MethodTimingEntry> createdByAnotherCall)
            {
                return createdByAnotherCall;
            }

            var newQueue = new ConcurrentQueue<MethodTimingEntry>();
            activity.SetCustomProperty(ActivityPropertyName, newQueue);
            return newQueue;
        }
    }
}

public sealed class MethodTimingEntry
{
    public MethodTimingEntry(string layer, string methodName, string? resourceName, DateTimeOffset startedAtUtc, double elapsedMilliseconds)
    {
        Layer = layer;
        MethodName = methodName;
        ResourceName = resourceName;
        StartedAtUtc = startedAtUtc;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string Layer { get; }

    public string MethodName { get; }

    public string? ResourceName { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public double ElapsedMilliseconds { get; }
}
