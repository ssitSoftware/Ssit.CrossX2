using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Builders;

public class ObjectCreationParameters
{
    /// <summary>
    /// Id of the object in the map file. 0 for objects created at runtime (they are not part of the map
    /// and are not tracked by <see cref="IMapObjects"/> or checkpoints).
    /// </summary>
    public int MapId { get; internal set; }
    
    public Vector2 Position { get; internal set; }
    public bool Flipped { get; internal set; }
    
    internal virtual object ParametersObject
    {
        // ReSharper disable once ValueParameterNotUsed
        set
        {
        }
    }

    public ILinkMap LinkMap { get; internal set; }
    public int ZOrder { get; internal set; }
    
    public static ObjectCreationParameters Create(Vector2 position, bool flipped, int zOrder, ILinkMap linkMap = null)
    {
        return new ObjectCreationParameters
        {
            Position = position,
            Flipped = flipped,
            LinkMap = linkMap,
            ZOrder = zOrder
        };
    }
}

public class ObjectCreationParameters<TParameters>: ObjectCreationParameters
    where TParameters : class
{
    public static ObjectCreationParameters<TParameters> Create(Vector2 position, bool flipped, int zOrder,
        TParameters parameters,
        ILinkMap linkMap = null)
    {
        return new ObjectCreationParameters<TParameters>
        {
            Position = position,
            Flipped = flipped,
            LinkMap = linkMap,
            ZOrder = zOrder,
            Parameters = parameters
        };
    }
    
    public TParameters Parameters { get; private set; }

    internal override object ParametersObject
    {
        set =>  Parameters = (TParameters)value;
    }

    public ObjectCreationParameters<T> Cast<T>() where T: class
    {
        return new ObjectCreationParameters<T>
        {
            MapId = MapId,
            Position = Position,
            Flipped = Flipped,
            LinkMap = LinkMap,
            ZOrder = ZOrder,
            Parameters = (T)(object)Parameters
        };
    }
}