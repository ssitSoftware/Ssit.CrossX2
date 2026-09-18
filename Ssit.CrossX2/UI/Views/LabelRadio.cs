using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;

public class LabelRadio : LabelButton
{
    public SharedValue<int> SelectedValue { get; set; }
    public int Value { get; set; }
}