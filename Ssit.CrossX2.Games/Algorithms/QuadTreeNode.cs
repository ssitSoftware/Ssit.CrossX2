using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Algorithms;

public class QuadTreeNode<T> where T : IAabbObject
{
    private readonly QuadTreeNode<T>[] _children = { null, null, null, null };

    private readonly List<T> _elements;

    public readonly Aabb Aabb;

    private readonly int _maxElementsPerNode;

    private static readonly IEnumerable<T> EmptyElements = new List<T>();

    public QuadTreeNode(Aabb aabb, IEnumerable<T> elements, int maxElementsPerNode)
    {
        Aabb = aabb;
        _elements = elements.ToList();
        _maxElementsPerNode = maxElementsPerNode;

        if (_elements.Count <= maxElementsPerNode) return;

        Aabb[] aabbs =
        {
            GetChildAabb(0),
            GetChildAabb(1),
            GetChildAabb(2),
            GetChildAabb(3)
        };

        List<T>[] elementsForNodes =
        {
            new(), new(), new(), new()
        };

        for (var idx = 0; idx < _elements.Count; idx++)
        {
            var element = _elements[idx];

            for (var nodeId = 0; nodeId < 4; ++nodeId)
            {
                if (!aabbs[nodeId].Contains(element.Aabb)) continue;

                elementsForNodes[nodeId].Add(element);
                _elements.RemoveAt(idx);
                idx--;
                break;
            }
        }

        for (var nodeId = 0; nodeId < 4; ++nodeId)
        {
            _children[nodeId] = new QuadTreeNode<T>(aabbs[nodeId], elementsForNodes[nodeId], maxElementsPerNode);
        }
    }

    public void RemoveElement(T element)
    {
        if (_elements.Remove(element) || _children[0] == null) return;

        foreach (var node in _children)
        {
            node.RemoveElement(element);
        }
    }

    public void AddElement(T element)
    {
        foreach (var node in _children)
        {
            if (node == null) break;

            if (!node.Aabb.Contains(element.Aabb))
            {
                continue;
            }

            node.AddElement(element);
            return;
        }

        if (_elements.Count + 1 > _maxElementsPerNode)
        {
            if (_children[0] == null)
            {
                for (var idx = 0; idx < 4; ++idx)
                {
                    _children[idx] = new QuadTreeNode<T>(GetChildAabb(idx), EmptyElements, _maxElementsPerNode);
                }
            }

            foreach (var node in _children)
            {
                if (!node.Aabb.Contains(element.Aabb))
                {
                    continue;
                }

                node.AddElement(element);
                return;
            }
        }

        _elements.Add(element);
    }

    private Aabb GetChildAabb(int index)
    {
        switch (index)
        {
            case 0: return new Aabb(Aabb.Left, Aabb.Top, Aabb.Center.X, Aabb.Center.Y);
            case 1: return new Aabb(Aabb.Center.X, Aabb.Top, Aabb.Right, Aabb.Center.Y);
            case 2: return new Aabb(Aabb.Left, Aabb.Center.Y, Aabb.Center.X, Aabb.Bottom);
            case 3: return new Aabb(Aabb.Center.X, Aabb.Center.Y, Aabb.Right, Aabb.Bottom);
        }
        throw new IndexOutOfRangeException();
    }

    public void GetElements(Aabb aabb, IList<T> list)
    {
        if (!aabb.Intersects(Aabb, 0.001f)) return;

        for (var idx = 0; idx < _elements.Count; ++idx)
        {
            var element = _elements[idx];
            if (element.Aabb.Intersects(aabb))
            {
                list.Add(_elements[idx]);
            }
        }

        foreach (var child in _children)
        {
            child?.GetElements(aabb, list);
        }
    }

    public void Debug_GetNodesAabbs(IList<Aabb> list)
    {
        list.Add(Aabb);
        foreach (var child in _children)
        {
            child?.Debug_GetNodesAabbs(list);
        }
    }
}