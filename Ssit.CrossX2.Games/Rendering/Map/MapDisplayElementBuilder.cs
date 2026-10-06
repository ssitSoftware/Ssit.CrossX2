using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class MapDisplayElementBuilder
{
    private MapFile _file;
    
    private IIoCContainer _container;
    private IContentManager _contentManager;
    private IGameTemplate _gameTemplate;
    private ILightsProvider _lightsProvider;
    
    public MapDisplayElementBuilder WithServices(IIoCContainer container, IContentManager contentManager)
    {
        _container = container;
        _contentManager = contentManager;
        return this;
    }
    
    public MapDisplayElementBuilder WithLightsProvider( ILightsProvider lightsProvider )
    {
        _lightsProvider = lightsProvider;
        return this;
    }
    
    public MapDisplayElementBuilder WithMap(MapFile mapFile)
    {
        _file = mapFile;
        return this;
    }
    
    public MapDisplayElementBuilder WithTemplate(IGameTemplate template)
    {
        _gameTemplate = template;
        return this;
    }

    public MapDisplayElement Build()
    {
        var layers = new List<LayerDisplayElement>();

        foreach (var layer in _file.Layers)
        {
            var builder = new LayerDisplayElementBuilder()
                .WithServices(_container, _contentManager)
                .WithMap(_file)
                .WithLayer(layer)
                .WithTemplate(_gameTemplate);

            layers.Add(builder.Build());
        }
        
        return new MapDisplayElement(layers, _file.BackgroundColor.AsPremultiplied(), _lightsProvider);
    }
}