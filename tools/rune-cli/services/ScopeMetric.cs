namespace vein;

public class ScopeMetric(string metricName) : IDisposable
{
    internal readonly Dictionary<string, string> data = new();
    private readonly Stopwatch _w = Stopwatch.StartNew();

    public void Dispose()
    {
        _w.Stop();
        SentrySdk.Metrics.Gauge("compile-time",
            _w.ElapsedMilliseconds,
            unit: MeasurementUnit.Duration.Millisecond,
            tags: data);
    }

    public static ScopeMetric Begin(string name) => new ScopeMetric(name);
}

public static class ScopeMetricExtensions
{
    public static ScopeMetric WithProject(this ScopeMetric metric, VeinProject project)
    {
        metric.data.TryAdd("project.Name", project.Name);
        metric.data.TryAdd("project.License", project.License);
        metric.data.TryAdd("project.Version", project.Version.ToNormalizedString());
        return metric;
    }
    public static ScopeMetric WithWorkload(this ScopeMetric metric, string key, string version)
    {
        metric.data.TryAdd("workload.Name", key);
        metric.data.TryAdd("workload.Version", version);
        return metric;
    }
}
