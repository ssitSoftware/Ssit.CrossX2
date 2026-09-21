namespace Ssit.CrossX2.Framework.UI.Views;

public class CustomDataView : View
{
    public object Data { get; set; }
}

public class CustomDataView<TData>: CustomDataView where TData: class
{
    public new TData Data
    {
        get => base.Data as TData;
        set => base.Data = value;
    }
}