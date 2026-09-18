using Newtonsoft.Json;

namespace Ssit.CrossX2.Graphics.Sprites.Json;

internal class JsonSize
{
    [JsonProperty( "w" )]
    public int W { get; set; }
    
    [JsonProperty( "h" )]
    public int H { get; set; }
}