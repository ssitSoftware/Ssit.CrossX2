namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface ICollectible
{
    bool Collect();
}

public interface ICollector
{
    void Collect(ICollectible collectible);
}