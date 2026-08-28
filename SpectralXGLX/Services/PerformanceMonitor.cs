namespace SpectralXGLX.Services
{
    public class PerformanceMonitor
    {
        private Queue<double> frameTimesMs = new Queue<double>(120); // Last 2 seconds at 60fps
        private Stopwatch frameStopwatch = new Stopwatch();
        private Dictionary<string, PerformanceMetric> metrics = new Dictionary<string, PerformanceMetric>();

        public double CurrentFPS { get; private set; }
        public double AverageFPS { get; private set; }
        public double MinFPS { get; private set; } = double.MaxValue;
        public double MaxFPS { get; private set; }
        public double FrameTimeMs { get; private set; }

        public class PerformanceMetric
        {
            public string Name { get; set; } = default!;
            public double TotalMs { get; set; }
            public double AverageMs { get; set; }
            public int CallCount { get; set; }
            public double PercentOfFrame { get; set; }
        }

        public void StartFrame()
        {
            frameStopwatch.Restart();
        }

        public void EndFrame()
        {
            frameStopwatch.Stop();
            FrameTimeMs = frameStopwatch.Elapsed.TotalMilliseconds;

            // Add to rolling window
            frameTimesMs.Enqueue(FrameTimeMs);
            if (frameTimesMs.Count > 120)
                frameTimesMs.Dequeue();

            // Calculate FPS
            CurrentFPS = 1000.0 / FrameTimeMs;
            AverageFPS = 1000.0 / frameTimesMs.Average();
            MinFPS = Math.Min(MinFPS, CurrentFPS);
            MaxFPS = Math.Max(MaxFPS, CurrentFPS);

            // Calculate percentages for metrics
            foreach (var metric in metrics.Values)
            {
                metric.PercentOfFrame = (metric.AverageMs / FrameTimeMs) * 100.0;
            }
        }

        public IDisposable MeasureSection(string sectionName)
        {
            return new PerformanceSection(this, sectionName);
        }

        private void RecordSection(string sectionName, double ms)
        {
            if (!metrics.ContainsKey(sectionName))
            {
                metrics[sectionName] = new PerformanceMetric { Name = sectionName };
            }

            var metric = metrics[sectionName];
            metric.TotalMs += ms;
            metric.CallCount++;
            metric.AverageMs = metric.TotalMs / metric.CallCount;
        }

        public Dictionary<string, PerformanceMetric> GetMetrics() => metrics;

        public void Reset()
        {
            frameTimesMs.Clear();
            metrics.Clear();
            MinFPS = double.MaxValue;
            MaxFPS = 0;
        }

        private class PerformanceSection : IDisposable
        {
            private readonly PerformanceMonitor monitor;
            private readonly string sectionName;
            private readonly Stopwatch stopwatch;

            public PerformanceSection(PerformanceMonitor monitor, string sectionName)
            {
                this.monitor = monitor;
                this.sectionName = sectionName;
                this.stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                stopwatch.Stop();
                monitor.RecordSection(sectionName, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
