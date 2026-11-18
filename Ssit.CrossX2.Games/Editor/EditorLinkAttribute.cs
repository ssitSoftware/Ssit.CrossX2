namespace Ssit.CrossX2.Framework.Games.Editor;

public class EditorLinkAttribute : EditorAttribute
{
    public Type Type { get; }
    
    /// <summary>
    /// How the link is displayed in the editor.
    /// </summary>
    public EditorLinkType LinkType { get; }
    
    public EditorLinkAttribute(Type type, Type validatorType = null, EditorLinkType linkType = EditorLinkType.Point): base(validatorType)
    {
        Type = type;
        LinkType = linkType;
    }
    
    public EditorLinkAttribute(Type type, EditorLinkType linkType): this(type, null, linkType)
    {
    }
}
