using Newtonsoft.Json;

namespace Ssit.CrossX2.Graphics.Sprites.Json;

internal class JsonFrame
{
    [JsonProperty("filename")] 
    public string FileName { get; set; }

    [JsonProperty("frame")] 
    public JsonRect FrameRect { get; set; }

    [JsonProperty("spriteSourceSize")] 
    public JsonRect SourceRect { get; set; }

    [JsonProperty("sourceSize")] 
    public JsonSize SourceSize { get; set; }

    [JsonProperty("duration")] 
    public int Duration { get; set; }

    public string Sequence => FileName.Split('|')[0];

    public int Index
    {
        get
        {
            var parts = FileName.Split('|');
            if (parts.Length < 2)
                return 0;

            int.TryParse(parts[1], out var value);
            return value;
        }
    }
}