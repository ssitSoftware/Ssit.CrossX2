namespace Ssit.CrossX2.Framework.Games.Editor;

public class EditorLinkAttribute : EditorAttribute
{
    public Type Type { get; }
    public bool ShowCircularConnection { get; }
    
    public EditorLinkAttribute(Type type, Type validatorType = null, bool showCircularConnection =  false): base(validatorType)
    {
        Type = type;
        ShowCircularConnection = showCircularConnection;
    }
    
    public EditorLinkAttribute(Type type, bool showCircularConnection): this(type, null, showCircularConnection)
    {
    }
}