using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views.CutScenes;

public record CsElement(CsTime Time);

public record CsBackground(CsTime Time, ColorWrapper Color) : CsElement(Time);
public record CsTypewriterText(SharedString Text, CsTime Time, CsPosition Position, CsTextParameters? Parameters = null, CsTime? TransitionTime = null) : CsElement(Time);
public record CsImage(string Path, CsTime Time, CsPosition Position) : CsElement(Time);
public record CsSprite(string Path, string Sequence, CsTime Time, CsPosition Position): CsElement(Time);
public record CsSound(string Path, CsTime Time, bool Loop = false): CsElement(Time);