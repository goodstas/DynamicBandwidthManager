namespace DynamicBandwidthCommon
{
    public enum DataType : byte
    {
        None = 0,
        Track,
        Plot,
        SensorStatus,
        MissionStatus
    }

    public enum MessagePriority : byte
    {
        None = 0,
        Normal,
        High
    }
}