namespace Ssit.CrossX2.Framework.Text;

public interface ICharProvider
{
    int Length { get; }
    char this[int index] { get; }
}