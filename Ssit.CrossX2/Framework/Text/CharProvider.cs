namespace Ssit.CrossX2.Framework.Text;

public abstract class CharProvider: ICharProvider
{
    public abstract int Length { get; }

    public abstract char this[int index] { get; }

    public override string ToString() => this.ToString(0, Length);
}