using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal class SdlGpuRenderQueue(SdlGpuRenderer renderer)
{
    struct Command
    {
        public ITexture Texture;
        public int Start;
        public int Count;
    }
    
    private const int Vertices = ushort.MaxValue;
    private const int AvgVertexSize = 32;

    private const int BufferSize = Vertices * AvgVertexSize;
    
    private VertexPct2D[] _buffer = new VertexPct2D[BufferSize];
    private List<Command> _commands = new();
    
    private int _currentPosition = 0;
    private ITexture _currentTexture = null;

    private int _startPosition = 0;
    
    public void AddTriangle(VertexPct2D p1, VertexPct2D p2, VertexPct2D p3, ITexture texture)
    {
        if (_currentTexture != texture)
        {
            StoreCommand();
            _currentTexture = texture;
        }
        
        _buffer[_currentPosition++] = p1;
        _buffer[_currentPosition++] = p2;
        _buffer[_currentPosition++] = p3;
    }

    private void StoreCommand()
    {
        if (_currentPosition == _startPosition)
            return;
        
        _commands.Add(new Command
        {
            Texture = _currentTexture,
            Start = _startPosition,
            Count = _currentPosition - _startPosition
        });
    }

    private void Flush()
    {
        StoreCommand();
    }
}