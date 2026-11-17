namespace Ssit.CrossX2.Framework.Games.Rendering;

/// <summary>
/// Converts integer z-orders into float render z-orders with a small random fraction (0.1 - 0.2),
/// so objects sharing the same integer z-order get a fixed, deterministic order between frames.
/// </summary>
public static class RenderZOrder
{
    private const float MinFraction = 0.1f;
    private const float FractionRange = 0.1f;

    public static float FromInt(int zOrder) => zOrder + MinFraction + Random.Shared.NextSingle() * FractionRange;
}
