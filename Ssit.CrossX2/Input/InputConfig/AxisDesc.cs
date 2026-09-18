using Newtonsoft.Json;

namespace Ssit.CrossX2.Input.InputConfig;

public class AxisDesc
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("axis")]
    public GameControllerAxis Axis { get; set; }
    
    [JsonProperty("buttons")]
    public AxisButtonsDesc<GameControllerButton> Buttons { get; set; }
    
    [JsonProperty("keys")]
    public AxisButtonsDesc<Key> Keys { get; set; }
}