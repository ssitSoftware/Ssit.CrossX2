namespace Ssit.CrossX2.Framework.Utils.CutScenes;

public record CsImage(string Path);
public record CsSprite(string Path, string Sequence) : CsImage(Path);
public record CsSound(string Path, double Time = 0);

/// <summary>
/// Scene which only switches the music playlist (see <see cref="CsScene.Playlist"/>) - zero time, the next scene starts
/// right away. Use it in a scene list: <c>new ChangePlaylist("Intro")</c>.
/// </summary>
public readonly record struct ChangePlaylist(string Name)
{
    public static implicit operator CsScene(ChangePlaylist playlist) =>
        new(null, null, null) { Playlist = playlist.Name };
}