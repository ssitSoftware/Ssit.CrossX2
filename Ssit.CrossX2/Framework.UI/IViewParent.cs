using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI;

public interface IViewParent
{
    RectangleF ScreenBounds { get; }
    void RecalculateLayout(View view = null);
    RectangleF CalculateTargetBounds();
    TParent GetParent<TParent>(bool optional = false) where TParent : class;
}