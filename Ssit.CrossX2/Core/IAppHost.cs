using System.Numerics;

namespace Ssit.CrossX2.Core;

public interface IAppHost : IDisposable
{
    public Matrix3x2 Transform { get; }
    public Matrix3x2 TransformInv { get; }
    Size TargetSize { get; }
    int Scale { get; }
}