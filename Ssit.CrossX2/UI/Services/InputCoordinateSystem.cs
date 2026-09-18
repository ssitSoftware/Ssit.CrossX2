using System.Numerics;
using Ssit.CrossX2.Core;

namespace Ssit.CrossX2.UI.Services;

internal class InputCoordinateSystem(IAppHost appHost): IInputCoordinateSystem
{
    public Matrix3x2 Transform => appHost.TransformInv;
    public Matrix3x2 TransformInv => appHost.Transform;
}