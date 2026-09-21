using Newtonsoft.Json;

namespace Ssit.CrossX2.Framework.Input.InputConfig;

public class MappingDesc
{
    [JsonProperty("axes")]
    public AxisDesc[] Axes { get; set; }
    
    [JsonProperty("buttons")]
    public ButtonDesc[] Buttons { get; set; }
}