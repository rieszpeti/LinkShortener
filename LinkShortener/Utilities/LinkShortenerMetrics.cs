using System.Diagnostics.Metrics;

namespace LinkShortener.Utilities
{
    public class LinkShortenerMetrics
    {
        public const string MeterName = "LinkShortener.Api";

        private readonly Counter<long> _requestCounter;
        private readonly Histogram<double> _requestDuration;

        public LinkShortenerMetrics(IMeterFactory meterFactory)
        {
            using var meter = meterFactory.Create(MeterName);

            _requestCounter = meter.CreateCounter<long>(
                "imageProcess.api.imageProcess_requests.count");

            _requestDuration = meter.CreateHistogram<double>(
                "imageProcess.api.imageProcess_requests.count");
        }

        public void IncreaseImageRequestCount() => _requestCounter.Add(1);

        public TrackedRequestDuration MeasureRequestDuration() => new TrackedRequestDuration(_requestDuration);
    }

    public sealed class TrackedRequestDuration : IDisposable
    {
        private readonly long _requestStartTime = TimeProvider.System.GetTimestamp();
        private readonly Histogram<double> _histogram;

        public TrackedRequestDuration(Histogram<double> histogram)
        {
            _histogram = histogram;
        }

        public void Dispose()
        {
            var elapsed = TimeProvider.System.GetElapsedTime(_requestStartTime);
            _histogram.Record(elapsed.Microseconds);

            GC.SuppressFinalize(this);
        }
    }
}
