namespace Ssit.CrossX2.Editor;

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