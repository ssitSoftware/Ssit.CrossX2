using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Editor;

namespace Ssit.CrossX2.Map;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class StaticObjectParameters
{
    [EditorInt(0, 5000, 10)]
    public int AnimationTimeOffsetInMs { get; set; }
}