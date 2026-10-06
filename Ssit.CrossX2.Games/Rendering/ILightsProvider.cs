using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface ILightsProvider
{
    void ApplyLights(IRenderer renderer, bool useGlobalAmbient, bool useGlobalLights);
}