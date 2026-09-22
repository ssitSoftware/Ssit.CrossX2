using System;
using System.Linq;
using System.Numerics;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools;

public class InsertObjectTool : InsertImageTool
{
    public new const string Name = "Insert Object";

    protected override bool HasLogic => true;

    protected override bool CanInsertObject => Editor.SelectedLayer == Instances.Map.MainLayer;

    public EditorObject Object
    {
        set => Image = value;
    }

    public InsertObjectTool(IEditorInstances instances) : base(Name, instances)
    {
    }

    protected override object CreateParameters()
    {
        var objDesc = Instances.Template.Objects.FirstOrDefault(o => o.FullName == Image.Id);
        if (objDesc is null) return null;

        if (objDesc.ParametersType == null) return null;

        return Activator.CreateInstance(objDesc.ParametersType);
    }

    protected override Type GetObjectType()
    {
        var objDesc = Instances.Template.Objects.FirstOrDefault(o => o.FullName == Image.Id);
        if (objDesc is null) return null;

        return objDesc.TargetType;
    }

    protected override int GetObjectZOrder()
    {
        var objDesc = Instances.Template.Objects.FirstOrDefault(o => o.FullName == Image.Id);
        if (objDesc is null) return 0;

        return objDesc.DefaultZOrder;
    }
}

public class InsertImageTool : EditorTool
{
    public new const string Name = "Insert Image";
        
    public EditorImage Image { get; set; }
    public bool Flipped { get; set; }

    protected virtual bool HasLogic => false;

    private Vector2? _insertPosition;

    protected virtual bool CanInsertObject => true;

    private SKPaint _skPaint = new();
    
    protected InsertImageTool(string name, IEditorInstances instances) : base(name, instances)
    {
    }
    
    public InsertImageTool(IEditorInstances instances) : base(Name, instances)
    {
    }

    public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
    {
        if (!_insertPosition.HasValue) return;
        if (Image is null) return;

        _skPaint.Color = SKColors.White.WithAlpha((byte)(CanInsertObject ? 192 : 128));
        var rect = Editor.DrawEditorImage(Image, Flipped, _insertPosition.Value, skCanvas, grContext, _skPaint);

        _skPaint.IsStroke = true;

        if (CanInsertObject)
        {
            _skPaint.Color = SKColors.GreenYellow.WithAlpha(192);
            skCanvas.DrawRect(rect.ToSkia(), _skPaint);

            var pos = Editor.MapToScreen(_insertPosition.Value);
            
            skCanvas.DrawLine(pos.X + 10, pos.Y + 10, pos.X - 10, pos.Y - 10, _skPaint);
            skCanvas.DrawLine(pos.X + 10, pos.Y - 10, pos.X - 10, pos.Y + 10, _skPaint);
        }
        else
        {
            _skPaint.Color = SKColors.Red;
            skCanvas.DrawLine(rect.TopLeft.ToSkia(), rect.BottomRight.ToSkia(), _skPaint);
            skCanvas.DrawLine(rect.BottomLeft.ToSkia(), rect.TopRight.ToSkia(), _skPaint);
        }
    }
    
    public override void OnMouseMove(MouseInputInfo input)
    {
        base.OnMouseMove(input);

        if (MousePosition.HasValue)
        {
            _insertPosition = Editor.ScreenToMap(MousePosition.Value);
            if (input.Modifiers.HasFlag(KeyModifiers.Control))
            {
                int x = (int) (_insertPosition.Value.X * 2);
                int y = (int) (_insertPosition.Value.Y * 2);

                _insertPosition = new Vector2(x / 2f, y / 2f);
            }
        }
        else
        {
            _insertPosition = null;
        }

        Editor.Redraw();
    }

    public override void OnMouseLeave(MouseInputInfo input)
    {
        base.OnMouseLeave(input);
        _insertPosition = null;
    }

    public override void OnButtonDown(MouseInputInfo input)
    {
        base.OnButtonDown(input);
        
        if (input.ActionButton != MouseButton.Left) return;
        if (!_insertPosition.HasValue) return;
        if (Image is null) return;
        if (!CanInsertObject) return;

        var parametersObj = CreateParameters();
        
        Instances.UndoRedoServices.PushState();
        
        Editor.SelectedLayer.Objects.Add(new MapObject
        {
            Id = Instances.Map.NextObjectId,
            TypeId = Image.Id,
            HasLogic = HasLogic,
            Position = _insertPosition.Value,
            ParametersObject = parametersObj,
            ZOrder = GetObjectZOrder(),
            Flipped = Flipped,
            Type = GetObjectType()
        });

        Editor.SelectedLayer.SortObjects();
        
        Editor.Redraw();
        Instances.Map.OnModified();
    }

    protected virtual int GetObjectZOrder() => 0;

    protected virtual Type GetObjectType() => null;

    protected virtual object CreateParameters() => new StaticObjectParameters();
}