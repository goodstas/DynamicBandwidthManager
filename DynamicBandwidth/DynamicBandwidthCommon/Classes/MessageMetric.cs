using Prometheus;

namespace DynamicBandwidthCommon.Classes;

// a class to hold Messages Metric for Prometheus
public class MessageMetric
{
    #region Fields
    // number of messages of current type in current chunk
    private readonly Gauge _count;

    // total number of bytes sent of current type in current chunk
    private readonly Gauge _size;

    // who is responsible for this metric
    private readonly string _who;

    #endregion

    #region Constructor

    // default constructor for Total Metric
    public MessageMetric(string who)
    {
        _who = who;
        _size = Metrics.CreateGauge("db_total_messages", "Number of bytes that has been sent", labelNames: new[] { "size", _who });
        _count = Metrics.CreateGauge("db_total_messages", "Number of messages that has been sent", labelNames: new[] { "count", _who });
    }

    //constructor for specific Data Type
    internal MessageMetric(string who, string Type)
    {
        _who = who;

        var lower = Type.ToLower().Replace(" ", "_");
        _size = Metrics.CreateGauge($"db_{lower}_messages",
            $"Number of bytes that has been sent for type {Type}", labelNames: new[] { "size",_who });
        _count = Metrics.CreateGauge($"db_{lower}_messages",
            $"Number of messages that has been sent for type {Type}", labelNames: new[] { "count",_who });
    }
    #endregion

    #region Methods
    // set gauges values
    public void Set(DataStatistics statistics)
    {
        _size.WithLabels("size",_who).Set((double)statistics.Size / 1000);
        _count.WithLabels("count",_who).Set(statistics.Count);
    }

    public void Set(int count, int size)
    {
        _size.WithLabels("size",_who).Set((double)size / 1000);
        _count.WithLabels("count", _who).Set(count);
    }
    #endregion
}