using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TokenStorageSingleton.Models
{
    public class Agent2AuthData : CommonAuthData
    {
        [JsonProperty("field_1")]
        public string Field1 { get; set; }
        [JsonProperty("field_2")]
        public string Field2 { get; set; }
    }
}