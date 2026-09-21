using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class LabelRadio : LabelButton
{
    public SharedValue<int> SelectedValue { get; set; }
    public int Value { get; set; }
}