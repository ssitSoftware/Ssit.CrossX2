using Ssit.CrossX2.Editor.Helpers;

namespace Ssit.CrossX2.Editor.Service;

public interface ISpritesContainer
{
    EditorSprite Get(string name);
}