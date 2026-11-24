namespace Ssit.CrossX2.Framework.Utils.CutScenes;

public record CsScene(CsTime? Time, string Text, CsImage Image, params CsSound[] Sounds)
{
    public CsTextParameters TextParameters { get; init; }

    /// <summary>
    /// Name of the music playlist to switch to when the scene starts. Null = the music doesn't change.
    /// </summary>
    public string Playlist { get; init; }

    public static implicit operator CsScene((CsTime Time, string Text) scene) =>
        new(scene.Time, scene.Text, null);
    
    public static implicit operator CsScene((CsTime Time, string Text, CsImage Image) scene) =>
        new(scene.Time, scene.Text, scene.Image);

    public static implicit operator CsScene((CsTime Time, string Text, CsImage Image, CsSound[] Sounds) scene) =>
        new(scene.Time, scene.Text, scene.Image, scene.Sounds);

    public static implicit operator CsScene((CsTime Time, string Text, CsTextParameters TextParameters, CsImage Image) scene) =>
        new(scene.Time, scene.Text, scene.Image) { TextParameters = scene.TextParameters };
    
    public static implicit operator CsScene((CsTime Time, string Text, CsTextParameters TextParameters) scene) =>
        new(scene.Time, scene.Text, null) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene(string text) =>
        new(null, text, null);

    public static implicit operator CsScene(CsImage image) =>
        new(null, null, image);

    public static implicit operator CsScene(CsSound[] sounds) =>
        new(null, null, null, sounds);

    public static implicit operator CsScene((string Text, CsTextParameters TextParameters) scene) =>
        new(null, scene.Text, null) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene((string Text, CsImage Image) scene) =>
        new(null, scene.Text, scene.Image);

    public static implicit operator CsScene((string Text, CsSound[] Sounds) scene) =>
        new(null, scene.Text, null, scene.Sounds);

    public static implicit operator CsScene((CsTextParameters TextParameters, CsImage Image) scene) =>
        new(null, null, scene.Image) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene((CsTextParameters TextParameters, CsSound[] Sounds) scene) =>
        new(null, null, null, scene.Sounds) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene((CsImage Image, CsSound[] Sounds) scene) =>
        new(null, null, scene.Image, scene.Sounds);

    public static implicit operator CsScene((string Text, CsTextParameters TextParameters, CsImage Image) scene) =>
        new(null, scene.Text, scene.Image) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene((string Text, CsTextParameters TextParameters, CsSound[] Sounds) scene) =>
        new(null, scene.Text, null, scene.Sounds) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene((string Text, CsImage Image, CsSound[] Sounds) scene) =>
        new(null, scene.Text, scene.Image, scene.Sounds);

    public static implicit operator CsScene((CsTextParameters TextParameters, CsImage Image, CsSound[] Sounds) scene) =>
        new(null, null, scene.Image, scene.Sounds) { TextParameters = scene.TextParameters };

    public static implicit operator CsScene((string Text, CsTextParameters TextParameters, CsImage Image, CsSound[] Sounds) scene) =>
        new(null, scene.Text, scene.Image, scene.Sounds) { TextParameters = scene.TextParameters };
}
