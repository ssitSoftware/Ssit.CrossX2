using System.Collections.Specialized;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public abstract class ChildrenContainerHandler<TContainer> 
    : BackgroundHandler<TContainer>, IChildrenContainer, IViewParent where TContainer : ChildrenContainer
{
    private readonly IHandlerMapper _handlerMapper;
    private readonly Dictionary<Guid, ViewHandler> _childrenHandlers = new();
    private readonly List<ViewHandler> _childrenHandlersList = new();
    
    protected readonly HashSet<View> RecalculateChildren = new();
    
    private bool _hasUnprocessedRemovedChildren;
    private bool _hasUnprocessedAddedChildren;

    IReadOnlyList<ViewHandler> IChildrenContainer.Children => _childrenHandlersList;

    protected IReadOnlyList<ViewHandler> Children => _childrenHandlersList;
    
    TParent IViewParent.GetParent<TParent>(bool optional)
    {
        if (this is TParent parent)
        {
            return parent;
        }

        return Parent.GetParent<TParent>(optional);
    }
    
    RectangleF IViewParent.CalculateTargetBounds()
    {
        return CalculateTargetBounds(Bounds);
    }
    
    protected RectangleF CalculateTargetBounds(RectangleF bounds)
    {
        var padding = AttachedView.Padding;

        var left = padding?.Left?.Calculate(CurrentScale, bounds.Width) ?? 0;
        var right = padding?.Right?.Calculate(CurrentScale, bounds.Width) ?? 0;
        
        var top = padding?.Top?.Calculate(CurrentScale, bounds.Height) ?? 0;
        var bottom = padding?.Bottom?.Calculate(CurrentScale, bounds.Height) ?? 0;

        return new RectangleF(left, top, bounds.Width - right - left, bounds.Height - bottom - top);
    }
    
    protected ChildrenContainerHandler(CreateHandlerParameters parameters, IHandlerMapper handlerMapper) : base(parameters)
    {
        _handlerMapper = handlerMapper;
        
        if (AttachedView.Children is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnCollectionChanged;
        }

        if (AttachedView.LayoutSignal is not null)
        {
            AttachedView.LayoutSignal.Signal += RecalculateLayout;
        }

        UpdateCollection(true, false);
    }

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);
        RecalculateChildrenLayouts();

        DrawChildren(renderer);
    }
    
    private void DrawChildren(IRenderer renderer)
    {
        for (var idx = 0; idx < AttachedView.Children.Count; idx++)
        {
            var child = AttachedView.Children[idx];
            var handlerView = (IHandlerView)child;
            handlerView.Handler.Draw(renderer);
        }
    }

    public override void Update(float dt)
    {
        for (var idx = 0; idx < AttachedView.Children.Count; idx++)
        {
            var child = AttachedView.Children[idx];
            var handlerView = (IHandlerView)child;
            handlerView.Handler.Update(dt);
        }
        
        UpdateCollection(_hasUnprocessedAddedChildren, _hasUnprocessedRemovedChildren);
        RecalculateChildrenLayouts();
    }

    public override void SetBounds(RectangleF rectangleF)
    {
        base.SetBounds(rectangleF);
        RecalculateLayout();
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        foreach (var child in _childrenHandlersList)
        {
            child.Dispose();
        }
        
        if (AttachedView.Children is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= OnCollectionChanged;
        }

        if (AttachedView.LayoutSignal is not null)
        {
            AttachedView.LayoutSignal.Signal -= RecalculateLayout;
        }
    }

    private void UpdateCollection(bool added, bool removed)
    {
        if (added)
        {
            foreach (var child in AttachedView.Children)
            {
                if (!_childrenHandlers.ContainsKey(child.Guid))
                {
                    CreateChild(child);
                }
            }
        }

        if (removed)
        {
            var toRemove = new List<(Guid, ViewHandler)>();
            foreach (var handler in _childrenHandlers.Values)
            {
                if (!AttachedView.Children.Contains(handler.View))
                {
                    toRemove.Add((handler.View.Guid, handler));
                }
            }
            
            foreach (var pair in toRemove)
            {
                _childrenHandlers.Remove(pair.Item1);
                _childrenHandlersList.Remove(pair.Item2);
                pair.Item2.Dispose();
            }
        }
    }

    private void OnCollectionChanged(object _, NotifyCollectionChangedEventArgs args)
    {
        _hasUnprocessedRemovedChildren = args.Action is NotifyCollectionChangedAction.Remove
            or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset;

        _hasUnprocessedAddedChildren = args.Action is NotifyCollectionChangedAction.Remove 
            or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset;
    }

    private void CreateChild(View child)
    {
        var handler = _handlerMapper.Create(child, this);
        _childrenHandlers.Add(child.Guid, handler);
        _childrenHandlersList.Add(handler);
        RecalculateChildren.Add(child);
    }
    
    protected abstract void RecalculateChildrenLayouts();
    
    public virtual void RecalculateLayout(View view = null)
    {
        if (view is null)
        {
            RecalculateChildren.UnionWith(AttachedView.Children);
        }
        else
        {
            RecalculateChildren.Add(view);
        }
        
        foreach (var child in RecalculateChildren)
        {
            if (child is IViewParent parent)
            {
                parent.RecalculateLayout();
            }
        }
        
        SignalRecalculationPending();
    }
}