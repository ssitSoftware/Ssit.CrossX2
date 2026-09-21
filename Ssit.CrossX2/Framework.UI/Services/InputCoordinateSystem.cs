using System.Numerics;
using Ssit.CrossX2.Framework.Core;

namespace Ssit.CrossX2.Framework.UI.Services;

internal class InputCoordinateSystem(IRenderHost renderHost): IInputCoordinateSystem
{
    public Matrix3x2 Transform => renderHost.TransformInv;
    public Matrix3x2 TransformInv => renderHost.Transform;
}