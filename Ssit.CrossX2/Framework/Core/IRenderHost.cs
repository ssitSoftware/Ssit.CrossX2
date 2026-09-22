using System.Numerics;

namespace Ssit.CrossX2.Framework.Core;

public interface IRenderHost : IDisposable
{
    public Matrix3x2 Transform { get; }
    public Matrix3x2 TransformInv { get; }
    Size TargetSize { get; }
    Size LogicalSize => TargetSize / Scale;
    int Scale { get; }
    void Apply();
}