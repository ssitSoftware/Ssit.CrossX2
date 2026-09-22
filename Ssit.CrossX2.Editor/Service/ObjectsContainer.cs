using System.Collections.Generic;
using System.Linq;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Service;

public class ObjectsContainer : IObjectsContainer
{
    private readonly IGameTemplate _gameTemplate;
    private readonly IServices _services;
    
    public IReadOnlyList<string> Categories { get; private set; }
    public IReadOnlyList<EditorObject> Objects => _objects;

    private readonly Dictionary<string, EditorObject> _objectsMap = new();
    
    public EditorObject Get(string objId)
    {
        return _objectsMap[objId];
    }

    private readonly List<EditorObject> _objects = new();

    public ObjectsContainer(IGameTemplate gameTemplate, IServices services)
    {
        _gameTemplate = gameTemplate;
        _services = services;
    }
    
    public void Load()
    {
        var categories = new HashSet<string>();
        
        foreach (var obj in _gameTemplate.Objects)
        {
            var editorObj = _services.Create<EditorObject>(obj);
            
            _objects.Add(editorObj);
            _objectsMap.Add(editorObj.Id, editorObj);
            
            var tags = obj.Tags;

            for (var idx =0; idx < tags.Length; ++idx)
            {
                categories.Add(tags[idx]);
            }
        }

        Categories = categories.OrderBy(o => o).ToArray();
    }
}