using Newtonsoft.Json;

namespace Ssit.CrossX2.Framework.Input.InputConfig;

public class ButtonDesc
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("button")]
    public GameControllerButton Button { get; set; } = GameControllerButton.None;
    
    [JsonProperty("alt-button")]
    public GameControllerButton AltButton { get; set; } = GameControllerButton.None;
    
    [JsonProperty("key")]
    public Key Key { get; set; } = Key.None;
    
    [JsonProperty("alt-key")]
    public Key AltKey { get; set; } = Key.None;
}