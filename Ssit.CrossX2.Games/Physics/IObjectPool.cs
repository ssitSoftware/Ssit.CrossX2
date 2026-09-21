namespace Ssit.CrossX2.Framework.Games.Physics;

public interface IObjectPool<TObject, TParameters> where TObject: class, IBodyOwner, IDisposable
{
    TObject Spawn(TParameters parameters);
}