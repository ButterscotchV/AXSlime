using System.Net;
using System.Text.Json.Serialization;

namespace AxSlime.Config
{
    public record AxSlimeConfig
    {
        public static readonly AxSlimeConfig Default = new();

        [JsonPropertyName("config_version")]
        public int ConfigVersion { get; set; } = 1;

        [JsonPropertyName("slimevr_endpoint")]
        public string SlimeVrEndPointStr { get; set; } = "127.0.0.1:6969";

        [JsonPropertyName("osc_enabled")]
        public bool OscEnabled { get; set; } = false;

        [JsonPropertyName("osc_receive_endpoint")]
        public string OscReceiveEndPointStr { get; set; } = "127.0.0.1:9001";

        [JsonPropertyName("haptic_vibration_intensity")]
        public float HapticVibrationIntensity { get; set; } = 1f;

        [JsonPropertyName("haptic_vibration_duration_s")]
        public float HapticVibrationDurationS { get; set; } = 1f;

        [JsonPropertyName("enable_bhaptics_support")]
        public bool EnableBHapticsSupport { get; set; } = true;

        // Ease of use utilities
        [JsonIgnore]
        public IPEndPoint SlimeVrEndPoint => IPEndPoint.Parse(SlimeVrEndPointStr);

        [JsonIgnore]
        public IPEndPoint OscReceiveEndPoint => IPEndPoint.Parse(OscReceiveEndPointStr);
    }

    [JsonSerializable(typeof(AxSlimeConfig))]
    public partial class JsonContext : JsonSerializerContext { }
}
