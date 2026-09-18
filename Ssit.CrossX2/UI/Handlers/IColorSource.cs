namespace Ssit.CrossX2.UI.Handlers;

public interface IColorSource: IViewParent
{
    RgbaColor? GetColor(string id);
}