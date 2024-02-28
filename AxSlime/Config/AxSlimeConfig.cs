using System.Net;
using System.Text.Json.Serialization;

namespace AxSlime.Config
{
    public record AxSlimeConfig
    {
        public static readonly AxSlimeConfig Default = new();

        [JsonPropertyName("config_version")]
        public int ConfigVersion { get; set; } = 0;

        [JsonPropertyName("slimevr_endpoint")]
        public string SlimeVrEndPointStr { get; set; } = "127.0.0.1:6969";

        [JsonIgnore]
        public IPEndPoint SlimeVrEndPoint => IPEndPoint.Parse(SlimeVrEndPointStr);
    }

    [JsonSerializable(typeof(AxSlimeConfig))]
    public partial class JsonContext : JsonSerializerContext { }
}
