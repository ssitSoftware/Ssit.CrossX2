using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Service
{
    public interface IEditorInstances
    {
        IGameTemplate Template { get; }
        IEditorTools Tools { get; }
        MapFile Map { get; }
        IEditor Editor { get; }
        ITilesetsContainer TilesetsContainer { get;  }
        IObjectsContainer ObjectsContainer { get; }
        IImagesContainer ImagesContainer { get; }
        ISpritesContainer SpritesContainer { get; }
        IUndoRedoService UndoRedoServices { get; }

        void SetEditor(IEditor editorVm);
        void SetMap(MapFile map);
    }

    public class EdtitorInstances : IEditorInstances
    {
        public EdtitorInstances(IGameTemplate template, ITilesetsContainer tilesetsContainer, IObjectsContainer objectsContainer, IImagesContainer imagesContainer, ISpritesContainer spritesContainer)
        {
            Template = template;
            TilesetsContainer = tilesetsContainer;
            ObjectsContainer = objectsContainer;
            ImagesContainer = imagesContainer;
            SpritesContainer = spritesContainer;
        }

        public IGameTemplate Template { get; }
        public IEditorTools Tools { get; set; }
        public MapFile Map { get; private set; }
        public IEditor Editor { get; private set; }
        public ITilesetsContainer TilesetsContainer { get; }
        public IObjectsContainer ObjectsContainer { get; }
        public IImagesContainer ImagesContainer { get; }
        public ISpritesContainer SpritesContainer { get; }
        public IUndoRedoService UndoRedoServices { get; set; }
        
        public void SetEditor(IEditor editor)
        {
            Editor = editor;
        }

        public void SetMap(MapFile map)
        {
            Map = map;
        }
    }
}