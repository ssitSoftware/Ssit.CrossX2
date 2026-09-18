namespace Ssit.CrossX2.Text;

public interface ICharProvider
{
    int Length { get; }
    char this[int index] { get; }
}