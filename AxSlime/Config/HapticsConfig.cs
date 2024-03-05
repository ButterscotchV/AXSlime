using System.Text.Json.Serialization;

namespace AxSlime.Config
{
    public record class HapticsConfig
    {
        public static readonly HapticsConfig Default = new();

        [JsonPropertyName("enable_touch")]
        public bool EnableTouch { get; set; } = true;

        [JsonPropertyName("touch_intensity")]
        public float TouchIntensity { get; set; } = 1f;

        [JsonPropertyName("touch_duration_s")]
        public float TouchDurationS { get; set; } = 1f;

        [JsonPropertyName("enable_proximity")]
        public bool EnableProx { get; set; } = true;

        [JsonPropertyName("proximity_threshold")]
        public float ProxThreshold { get; set; } = 0f;

        [JsonPropertyName("proximity_min_intensity")]
        public float ProxMinIntensity { get; set; } = 0.25f;

        [JsonPropertyName("proximity_max_intensity")]
        public float ProxMaxIntensity { get; set; } = 1f;

        [JsonPropertyName("proximity_duration_s")]
        public float ProxDurationS { get; set; } = 0.1f;

        [JsonPropertyName("nonlinear_proximity")]
        public bool NonlinearProx { get; set; } = true;

        [JsonPropertyName("enable_axhaptics_support")]
        public bool EnableAxHaptics { get; set; } = true;

        [JsonPropertyName("enable_bhaptics_support")]
        public bool EnableBHaptics { get; set; } = true;

        // Utilities

        [JsonIgnore]
        public float ProxIntensityRange => ProxMaxIntensity - ProxMinIntensity;
    }
}
