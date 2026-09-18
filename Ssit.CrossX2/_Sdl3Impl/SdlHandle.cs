namespace Ssit.CrossX2._Sdl3Impl;

public unsafe class SdlHandle<TResource>(TResource* pointer)
    where TResource : unmanaged
{
    public static readonly SdlHandle<TResource> Empty = new (null);
    public TResource* Pointer { get; private set; } = pointer;
    
    public void OnDisposed() => Pointer = null;
}