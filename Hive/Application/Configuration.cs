using Hive.Application.Logging;
using Newtonsoft.Json;

namespace Hive.Application
{
    public class Configuration
    {
        [JsonProperty(Required = Required.Always)]
        public string MySqlHost { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string MySqlUser { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string MySqlPassword { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string MySqlSchema { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public bool UseExternalConsole { get; set; }
        
        [JsonProperty(Required = Required.Default)]
        public bool? UpdateRevertsSoftDeletion { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public LogLevel LogLevel { get; set; }
    }
}