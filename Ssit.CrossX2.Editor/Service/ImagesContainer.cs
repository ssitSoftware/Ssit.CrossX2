using System.Collections.Generic;
using System.Linq;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Service;

public class ImagesContainer : IImagesContainer
{
    private readonly IGameTemplate _gameTemplate;
    private readonly IServices _services;
    
    public IReadOnlyList<string> Categories { get; private set; }
    public IReadOnlyList<EditorImage> Images => _images;

    private readonly Dictionary<string, EditorImage> _imagesMap = new();

    private readonly List<EditorImage> _images = new();
    
    public ImagesContainer(IGameTemplate gameTemplate, IServices services)
    {
        _gameTemplate = gameTemplate;
        _services = services;
    }
    
    public EditorImage Get(string objId)
    {
        return _imagesMap[objId];
    }
    
    public void Load()
    {
        var categories = new HashSet<string>();
        
        foreach (var obj in _gameTemplate.Images)
        {
            var editorObj = _services.Create<EditorImage>(obj);
            _images.Add(editorObj);
            
            _imagesMap.Add(editorObj.Id, editorObj);
            
            var tags = obj.Tags;

            for (var idx =0; idx < tags.Length; ++idx)
            {
                categories.Add(tags[idx]);
            }
        }

        Categories = categories.OrderBy(o => o).ToArray();
    }
}