using Newtonsoft.Json;

namespace Ssit.CrossX2.Framework.Graphics.Sprites.Json;

internal class JsonSprite
{
    [JsonProperty("frames")]
    public JsonFrame[] Frames { get; set; }
}