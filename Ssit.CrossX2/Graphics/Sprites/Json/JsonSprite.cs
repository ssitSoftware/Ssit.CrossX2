using Newtonsoft.Json;

namespace Ssit.CrossX2.Graphics.Sprites.Json;

internal class JsonSprite
{
    [JsonProperty("frames")]
    public JsonFrame[] Frames { get; set; }
}