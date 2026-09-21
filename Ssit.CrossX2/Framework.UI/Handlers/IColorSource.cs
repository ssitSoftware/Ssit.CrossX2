namespace Ssit.CrossX2.Framework.UI.Handlers;

public interface IColorSource: IViewParent
{
    RgbaColor? GetColor(string id);
}