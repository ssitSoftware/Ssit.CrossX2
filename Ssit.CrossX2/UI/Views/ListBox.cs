namespace Ssit.CrossX2.UI.Views;

public class ListBox<TModel>: View
{
    public IReadOnlyList<TModel> Items { get; set; }
    public Func<TModel, View> ItemTemplate { get; set; }
}