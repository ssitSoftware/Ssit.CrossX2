namespace Ssit.CrossX2.Editor.Input
{
    public interface IPointerHandler
    {
        void OnMouseMove(MouseInputInfo input);
        void OnButtonDown(MouseInputInfo input);
        void OnButtonUp(MouseInputInfo input);
    
        void OnMouseLeave(MouseInputInfo input);
        bool OnMouseWheel(MouseInputInfo input);
    }
}