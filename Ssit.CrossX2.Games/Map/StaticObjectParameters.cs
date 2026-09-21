using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Framework.Games.Map;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class StaticObjectParameters
{
    [EditorInt(0, 5000, 10)]
    public int AnimationTimeOffsetInMs { get; set; }
}