namespace Ssit.CrossX2.Framework.Graphics.Sprites;

public interface ISpriteEvent
{
    string EventName { get; }
    TParameters GetParameters<TParameters>() where TParameters : class, new();
}