namespace Ssit.CrossX2.Framework.Games.Platformer.Builders;

public interface ILinkMap
{
    void RequestLink<TLink>(int id, Action<TLink> action) where TLink : class;
}