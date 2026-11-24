namespace Ssit.CrossX2.Framework.Utils.CutScenes;

public record CsImage(string Path);
public record CsSprite(string Path, string Sequence) : CsImage(Path);
public record CsSound(string Path, double Time = 0);