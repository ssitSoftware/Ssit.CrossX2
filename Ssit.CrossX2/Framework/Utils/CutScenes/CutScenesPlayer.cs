using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Common.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Utils.CutScenes;

/// <summary>
/// Plays the cut scenes one after another: sets the whole (translated) text of the scene, types it letter by letter by
/// increasing <see cref="VisibleCharacters"/> (for a <c>CutsceneText</c> view) and shows the image of the scene.
/// Each scene lasts the longer of its own <see cref="CsScene.Time"/> and the time its text needs - the typing time
/// (letters / <c>lettersPerSecond</c>) plus <c>displayFullTextTime</c> for reading the fully typed text.
/// The first <see cref="Update"/> starts the first scene; <c>onFinished</c> is invoked once after the last scene.
/// </summary>
public class CutScenesPlayer(ITranslator translator, IReadOnlyList<CsScene> scenes, float lettersPerSecond, float displayFullTextTime, Action onFinished)
{
    /// <summary>
    /// How long the fully typed text must be displayed before <see cref="Skip"/> moves to the next scene.
    /// </summary>
    private const double SkipToNextSceneDelay = 0.5;
    
    private readonly IReadOnlyList<CsScene> _scenes = scenes ?? [];

    // Translated text and times of the scenes.
    private readonly SceneTiming[] _timings = CalculateTimings(translator, scenes ?? [], lettersPerSecond, displayFullTextTime);
    
    private readonly SharedStringValue _text = new();
    private readonly SharedBoolMutable _hasImage = new(false);

    private int _sceneIndex = -1;
    private double _sceneTime;
    private string _imagePath;

    /// <summary>
    /// Image of the current scene. Hidden (see <see cref="HasImage"/>) when the scene has no image.
    /// </summary>
    public ImageSource<ITexture> Image { get; } = new();

    /// <summary>
    /// True when the current scene has an image - bind it to the visibility of the image view.
    /// </summary>
    public SharedBool HasImage => _hasImage;

    /// <summary>
    /// Whole text of the current scene - set once when the scene starts.
    /// </summary>
    public SharedString Text => _text;

    /// <summary>
    /// Number of the typed characters of <see cref="Text"/>. Stops at the text length when the whole text is typed.
    /// </summary>
    public SharedValue<int> VisibleCharacters { get; } = new(0);
    
    /// <summary>
    /// Index of the current scene; -1 before the first <see cref="Update"/>.
    /// </summary>
    public int CurrentScene => _sceneIndex;
    
    public bool IsFinished { get; private set; }

    /// <summary>
    /// Calculated time of the scene (seconds).
    /// </summary>
    public double GetSceneTime(int index) => _timings[index].Time;
    
    public void Update(float dt)
    {
        if (IsFinished)
            return;

        if (_sceneIndex < 0)
        {
            if (_scenes.Count == 0)
            {
                Finish();
                return;
            }
            
            StartScene(0, 0);
        }
        else
        {
            _sceneTime += dt;
        }

        // A long frame (or a scene with zero time) may pass more than one scene.
        while (!IsFinished && _sceneTime >= _timings[_sceneIndex].Time)
        {
            NextScene(_sceneTime - _timings[_sceneIndex].Time);
        }

        UpdateTypedText();
    }

    /// <summary>
    /// While the text is being typed, types it all at once. When it's fully typed and has been displayed for
    /// <see cref="SkipToNextSceneDelay"/>, moves to the next scene (finishes after the last one).
    /// </summary>
    public void Skip()
    {
        if (IsFinished || _sceneIndex < 0)
            return;

        var typedTime = _timings[_sceneIndex].TypedTime;
        
        if (_sceneTime < typedTime)
        {
            _sceneTime = typedTime;
        }
        else if (_sceneTime >= typedTime + SkipToNextSceneDelay)
        {
            NextScene(0);
        }
        
        UpdateTypedText();
    }

    private void NextScene(double sceneTime)
    {
        if (_sceneIndex + 1 >= _scenes.Count)
        {
            Finish();
            return;
        }
        
        StartScene(_sceneIndex + 1, sceneTime);
    }

    private void StartScene(int index, double sceneTime)
    {
        _sceneIndex = index;
        _sceneTime = sceneTime;
        
        // Nothing visible before the new text is set - the old count would show a part of it at once.
        VisibleCharacters.Value = 0;

        if (_timings[index].Text is { } text)
        {
            _text.SetText(text);
        }
        else
        {
            _text.SetText(string.Empty);
        }

        var imagePath = _scenes[index].Image?.Path;
        
        // The same image in the following scene stays loaded.
        if (imagePath is not null && imagePath != _imagePath)
        {
            Image.SetSource(imagePath);
        }

        if (imagePath is not null)
        {
            _imagePath = imagePath;
        }
        
        _hasImage.SetValue(imagePath is not null);
    }

    private void UpdateTypedText()
    {
        if (IsFinished)
            return;
        
        var length = _text.Length;
        
        // Clamped to the text length - the count doesn't change after the whole text is typed (the cursor blinks then).
        var letters = lettersPerSecond > 0
            ? (int)Math.Min(_sceneTime * lettersPerSecond, length)
            : length;
        
        VisibleCharacters.Value = letters;
    }

    private void Finish()
    {
        IsFinished = true;
        onFinished?.Invoke();
    }

    /// <summary>
    /// Translates the texts of the scenes and calculates their times: the longer of the scene time and the time
    /// of its text (typing time + <paramref name="displayFullTextTime"/>).
    /// </summary>
    private static SceneTiming[] CalculateTimings(ITranslator translator, IReadOnlyList<CsScene> scenes,
        float lettersPerSecond, float displayFullTextTime)
    {
        var timings = new SceneTiming[scenes.Count];
        
        for (var idx = 0; idx < scenes.Count; idx++)
        {
            var scene = scenes[idx];
            
            // Shared string of the translator - follows the language changes.
            var text = string.IsNullOrEmpty(scene.Text) ? null : translator[scene.Text];
            var letters = text?.Length ?? 0;
            
            var typedTime = lettersPerSecond > 0 ? letters / (double)lettersPerSecond : 0;
            var textTime = letters > 0 ? typedTime + displayFullTextTime : 0;
            
            timings[idx] = new SceneTiming(text, typedTime, Math.Max(scene.Time?.Time ?? 0, textTime));
        }

        return timings;
    }

    /// <param name="Text">Translated text of the scene; null when the scene has no text.</param>
    /// <param name="TypedTime">Time from the start of the scene when all the letters of the text are typed.</param>
    /// <param name="Time">Total time of the scene.</param>
    private readonly record struct SceneTiming(SharedString Text, double TypedTime, double Time);
}
