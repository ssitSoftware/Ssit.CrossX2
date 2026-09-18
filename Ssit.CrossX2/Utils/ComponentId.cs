namespace Ssit.CrossX2.Utils;

public readonly struct ComponentId(): IEquatable<ComponentId>
{
    public static ComponentId Invalid { get; } = new() { Id = uint.MaxValue };
    
    private uint Id { get; init; } = 0;

    public static ComponentId operator ++(ComponentId cid)
    {
        return new ComponentId
        {
            Id = cid.Id + 1
        };
    }
    
    public bool Equals(ComponentId other) => Id == other.Id;
    public override bool Equals(object obj) => obj is ComponentId other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => Id.ToString();

    public static bool operator ==(ComponentId left, ComponentId right) => left.Id == right.Id;
    public static bool operator !=(ComponentId left, ComponentId right) => left.Id != right.Id;
}