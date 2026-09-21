namespace Ssit.CrossX2.Framework.Games.Physics;

public interface IParametersOwner<TParameters>
{
    TParameters Parameters { get; }
}