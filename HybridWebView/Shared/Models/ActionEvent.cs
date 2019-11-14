using Newtonsoft.Json;

namespace Plugin.HybridWebView.Shared.Models
{
    [JsonObject]
    public class ActionEvent
    {

        [JsonProperty("action", Required = Required.Always)]
        public string Action { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

    }
}
