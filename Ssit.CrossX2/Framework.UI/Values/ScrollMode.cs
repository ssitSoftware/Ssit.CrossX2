namespace Ssit.CrossX2.Framework.UI.Values;

[Flags]
public enum ScrollMode
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Both = Horizontal | Vertical
}