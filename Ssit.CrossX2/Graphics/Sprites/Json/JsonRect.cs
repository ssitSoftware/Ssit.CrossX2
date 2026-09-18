using Newtonsoft.Json;

namespace Ssit.CrossX2.Graphics.Sprites.Json;

internal class JsonRect: JsonSize
{
    [JsonProperty( "x" )]
    public int X { get; set; }
    
    [JsonProperty( "y" )]
    public int Y { get; set; }
}