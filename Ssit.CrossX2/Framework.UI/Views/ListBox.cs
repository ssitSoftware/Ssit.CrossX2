namespace Ssit.CrossX2.Framework.UI.Views;

public class ListBox<TModel>: View
{
    public IReadOnlyList<TModel> Items { get; set; }
    public Func<TModel, View> ItemTemplate { get; set; }
}