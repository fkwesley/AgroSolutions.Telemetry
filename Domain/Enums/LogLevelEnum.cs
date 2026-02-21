using System.Text.Json.Serialization;

namespace Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LogLevelEnum
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}
