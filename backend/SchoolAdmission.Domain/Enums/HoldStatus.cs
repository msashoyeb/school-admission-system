using System.Text.Json.Serialization;

namespace SchoolAdmission.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HoldStatus
    {
        HELD = 0,
        CONFIRMED = 1,
        EXPIRED = 2,
        CANCELLED = 3
    }
}
