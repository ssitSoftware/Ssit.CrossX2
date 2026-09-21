namespace Ssit.CrossX2.Framework.Games.Editor;

public interface IPropertyHandler
{
    bool Validate(object value);
    bool Enable(object value);

    void OnUpdating()
    {
    }
    
    void OnUpdated()
    {
    }
}