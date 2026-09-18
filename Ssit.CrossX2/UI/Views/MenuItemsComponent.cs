using System.Windows.Input;
using Ssit.CrossX2.UI.Handlers;
using Ssit.CrossX2.UI.Parameters;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;


[Obsolete("Create menu items as views in vertical stack and use NextId do generate id.\nThis is no longer needed as navigation is not id based anymore.")]
public class MenuItemsComponent<TButton, TVerticalStack>: VerticalStack where TButton : LabelButton, new() where TVerticalStack: VerticalStack, new()
{
    public MenuItemsComponent()
    {
        Padding = (8, 8);
        VerticalAlign = Align.Center;
        HorizontalAlign = Align.Center;
        
        CustomHandlerType = typeof(VerticalStackHandler<MenuItemsComponent<TButton, TVerticalStack>>);
    }
    
    public string IdPrefix { get; set; }
    
    public IReadOnlyList<(SharedString text, ICommand command, bool enableCommandType)> ItemsWithCommandType { get; set; }

    public IReadOnlyList<(SharedString text, ICommand command)> Items
    {
        set
        {
            ItemsWithCommandType = value.Select(i => (i.text, i.command, false)).ToArray();
        }
    }
    
    public int DoubleRows { get; set; }
    
    public IPageCommandsSource CommandsSource { get; set; }

    public Action<TButton> ApplyButtonStyle { get; set; }
    
    public string GenerateDefaultId(int defaultButtonIndex = 0)
    {
        return $"{IdPrefix}{defaultButtonIndex}";
    }
    
    protected override void Initialize(IUiServices services)
    {
        var controls = new List<View>();
        
        int navId = 0;
        
        for (var i = 0; i < ItemsWithCommandType.Count; i++)
        {
            var item = ItemsWithCommandType[i];
            if ((item.text?.Length ?? 0) == 0)
            {
                controls.Add(new Background
                {
                    Height = 4
                });
                continue;
            }

            var button = new TButton
            {
                Text = item.text,
                UniqueId = $"{IdPrefix}{navId}",
                Command = item.command
            };
            ApplyButtonStyle?.Invoke(button);
            controls.Add(button);
            navId++;
        }
        
        Children = controls.ToArray();
    }
}