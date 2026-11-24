using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views.CutScenes;

public class CutSceneView: View, IFocusableView
{
    public IReadOnlyList<CsScene> Scenes { get; set; }
    public string UniqueId { get; set; }
    public IValueConsumer<bool> FocusConsumer { get; set; }
}