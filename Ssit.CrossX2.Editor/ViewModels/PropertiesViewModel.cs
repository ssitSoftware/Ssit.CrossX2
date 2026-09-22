using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ssit.CrossX2.Editor.Models.Parameters;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Editor.Tools;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.ViewModels;

public class PropertiesViewModel: BindableModel
{
    private class LinkHandler : IPropertyHandler
    {
        private readonly IPropertyHandler _parent;
        private readonly MapObject _mapObject;

        public LinkHandler(IPropertyHandler parent, MapObject mapObject)
        {
            _parent = parent;
            _mapObject = mapObject;
        }

        public bool Validate(object value)
        {
            return _parent.Validate(value);
        }

        public void OnUpdating()
        {
            _parent.OnUpdating();
        }
        
        public bool Enable(object value)
        {
            return _parent.Enable(value);
        }

        public void OnUpdated()
        {
            _mapObject?.UpdateLinks();
            _parent.OnUpdated();
        }
    }
    
    private class InternalHandler : IPropertyHandler
    {
        private readonly IPropertyHandler _parent;
        private readonly IEditor _editor;
        private readonly IEditorInstances _instances;
        private readonly MapFile _mapFile;
        
        public InternalHandler(IPropertyHandler parent, IEditor editor, IEditorInstances instances, MapFile mapFile)
        {
            _parent = parent;
            _editor = editor;
            _instances = instances;
            _mapFile = mapFile;
        }
        
        public bool Validate(object value)
        {
            _editor.Redraw();
            var valid = _parent?.Validate(value) ?? true;
            
            return valid;
        }

        public bool Enable(object value)
        {
            return _parent?.Enable(value) ?? true;
        }

        public void OnUpdating()
        {
            _instances.UndoRedoServices.PushState();
        }
        
        public void OnUpdated()
        {
            _mapFile.OnModified();
            _editor.SelectedLayer.SortObjects();
        }
    }
    
    private MapFile _mapFile;
    private readonly IEditor _editor;
    private readonly IWindowService _windowService;
    private readonly IEditorInstances _instances;

    public string Title
    {
        get;
        set => SetField(ref field, value);
    }

    public MapFile MapFile
    {
        set
        {
            _mapFile = value;
            UpdateProperties();
        }
    }

    public ObservableCollection<ParameterModel> Parameters { get; } = new();

    public ParameterModel Selected
    {
        get => null;
        
        set
        {
            if (value != null)
            {
                OnPropertyChanged();
            }
        }
    }

    public PropertiesViewModel(IEditor editor, IWindowService windowService, IEditorInstances instances)
    {
        _editor = editor;
        _windowService = windowService;
        _instances = instances;
    }

    private void UpdateProperties()
    {
        Parameters.Clear();
        
        if (_editor.SelectedObject != 0)
        {
            var obj = _mapFile.FindObject(_editor.SelectedObject);
            if (obj is not null)
            {
                Title = "Object Properties";
                
                Parameters.Add(new ParameterInfoModel(obj.TypeId.Split('/').Last()));
                
                FillProperties(obj);
                return;
            }
        }
        
        if (_editor?.SelectedLayer is not null)
        {
            Title = "Map & Layer Properties";
            FillProperties(_mapFile);
            
            FillProperties(_editor.SelectedLayer);
        }
    }

    private void FillProperties(object source)
    {
        var type = source.GetType();

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<EditorAttribute>() is not null)
            {
                GenerateProperty(property, source);
            }
        }
    }

    private void GenerateProperty(PropertyInfo prop, object source)
    {
        if (prop is null) throw new InvalidOperationException();
        
        var attrBase = prop.GetCustomAttribute<EditorAttribute>();

        var handlerType = attrBase?.HandlerType;

        var handler = handlerType == null ? null : Activator.CreateInstance(handlerType) as IPropertyHandler;
        handler = new InternalHandler(handler, _editor, _instances, _mapFile);

        var name = new StringBuilder();
        foreach (var ch in prop.Name)
        {
            if (name.Length > 0 && Char.IsAsciiLetterUpper(ch))
            {
                name.Append(' ');
            }

            name.Append(ch);
        }

        var propName = name.ToString();
        
        if (prop.PropertyType == typeof(string))
        {
            Parameters.Add(new ParameterStringModel(propName, source, prop, handler));
            return;
        }
        
        if (prop.PropertyType == typeof(int))
        {
            var attr = prop.GetCustomAttribute<EditorIntAttribute>();

            if (attr is null)
            {
                var attr2 = prop.GetCustomAttribute<EditorLinkAttribute>();
                if (attr2 is null)
                    return;

                
                var obj = _mapFile.FindObject(_editor.SelectedObject);
                var newHandler = new LinkHandler(handler, obj);
                
                Parameters.Add(new ParameterTargetModel(propName, source, prop, attr2.Type, SelectMapObject, newHandler, _mapFile));
                return;
            }
            else
            {
                Parameters.Add(new ParameterIntModel(propName, attr.Min, attr.Max, attr.Step, source, prop, handler));
                return;
            }
        }
        
        if (prop.PropertyType == typeof(float))
        {
            var attr = prop.GetCustomAttribute<EditorFloatAttribute>();
            if (attr is null)
                return;
            
            Parameters.Add(new ParameterFloatModel(propName, attr.Min, attr.Max, attr.Step, source, prop, handler));
            return;
        }
        
        if (prop.PropertyType == typeof(bool))
        {
            Parameters.Add(new ParameterBooleanModel(propName, source, prop, handler));
            return;
        }
        
        if (prop.PropertyType == typeof(RgbaColor))
        {
            Parameters.Add(new ParameterColorModel(propName, source, prop, handler));
            return;
        }
        
        if (prop.GetCustomAttribute<EditorLayerSizeAttribute>() != null)
        {
            Parameters.Add(new ParameterLayerSizeModel(propName, source, ResizeLayer));
            return;
        }
        
        if (prop.GetCustomAttribute<EditorComplex>() != null)
        {
            var value = prop.GetValue(source);
            if (value != null)
            {
                FillProperties(value);
            }
            return;
        }

        throw new InvalidOperationException();
    }

    private Task<MapObject> SelectMapObject(Type type)
    {
        var sot = _instances.Tools.GetTool<SelectionTool>();

        _instances.Tools.Current = sot;
        return sot.SelectObject(type);
    }

    private async Task ResizeLayer()
    {
        if (_editor.SelectedLayer is null) return;
        await _windowService.ShowDialog<ResizeLayerDialogViewModel>(_editor.SelectedLayer);
    }
}