using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Common.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Utils.CutScenes;

/// <summary>
/// Plays the cut scenes one after another: sets the whole (translated) text of the scene, types it letter by letter by
/// increasing <see cref="VisibleCharacters"/> (for a <c>CutsceneText</c> view) and shows the image of the scene.
/// Each scene lasts the longer of its own <see cref="CsScene.Time"/> and the time its text needs - the typing time
/// (letters / <see cref="Parameters.LettersPerSecond"/>) plus <see cref="Parameters.DisplayFullTextTime"/> for reading
/// the fully typed text. The first <see cref="Update"/> starts the first scene; <see cref="Parameters.OnFinished"/> is
/// invoked once after the last scene. The key sound (<see cref="Parameters.KeySoundPath"/>, optional) is played whenever
/// a new letter is about to show. A scene with <see cref="CsScene.Playlist"/> switches the music when it starts.
/// <para>
/// Create it with <c>container.IoCConstruct&lt;CutScenesPlayer&gt;(new CutScenesPlayer.Parameters { ... })</c> - the
/// services come from the container.
/// </para>
/// </summary>
public class CutScenesPlayer(CutScenesPlayer.Parameters parameters, ITranslator translator, IContentManager contentManager,
    IMusicPlayer musicPlayer) : IDisposable
{
    public class Parameters
    {
        /// <summary>
        /// Scenes to play.
        /// </summary>
        public IReadOnlyList<CsScene> Scenes { get; init; }

        /// <summary>
        /// Typing speed of the scene texts. 0 or less = the whole text shows at once.
        /// </summary>
        public float LettersPerSecond { get; init; }

        /// <summary>
        /// How long the fully typed text stays displayed (unless the scene time is longer).
        /// </summary>
        public float DisplayFullTextTime { get; init; }

        /// <summary>
        /// Sound played whenever a new letter shows. Null = no sound.
        /// </summary>
        public string KeySoundPath { get; init; }

        /// <summary>
        /// Base directory of the scene images (e.g. "assets:/Game/Cutscenes/"). A relative <see cref="CsImage.Path"/>
        /// is resolved against it; a path with a scheme ("assets:/...") or starting with '/' is used as it is.
        /// Null = image paths are used as they are.
        /// </summary>
        public string ImagesBaseDirectory { get; init; }

        /// <summary>
        /// Part of the typing of the scene text (0 - 1) after which the image of the scene shows (<see cref="HasImage"/>
        /// becomes true - bind it to the visibility of the image view, its Show transition runs then): 0 = when the
        /// scene starts, 1 = when the whole text is typed. Default: 0.5.
        /// </summary>
        public float ImageShowAtTypedFraction { get; init; } = 0.5f;

        /// <summary>
        /// Invoked once after the last scene.
        /// </summary>
        public Action OnFinished { get; init; }
    }

    /// <summary>
    /// How long the fully typed text must be displayed before <see cref="Skip"/> moves to the next scene.
    /// </summary>
    private const double SkipToNextSceneDelay = 0.5;
    
    private readonly float _lettersPerSecond = parameters.LettersPerSecond;
    private readonly Action _onFinished = parameters.OnFinished;
    private readonly string _imagesBaseDirectory = parameters.ImagesBaseDirectory;
    private readonly float _imageShowAtTypedFraction = Math.Clamp(parameters.ImageShowAtTypedFraction, 0f, 1f);
    
    private readonly IReadOnlyList<CsScene> _scenes = parameters.Scenes ?? [];

    // Translated text and times of the scenes.
    private readonly SceneTiming[] _timings = CalculateTimings(translator, parameters.Scenes ?? [],
        parameters.LettersPerSecond, parameters.DisplayFullTextTime);
    
    // Played when a new letter shows; released when the cut scenes finish (or on dispose).
    private ResourceHandle<ISoundEffect> _keySound = LoadSound(contentManager, parameters.KeySoundPath);
    
    private readonly SharedStringValue _text = new();
    private readonly SharedBoolMutable _hasImage = new(false);

    private int _sceneIndex = -1;
    private double _sceneTime;

    // Typing time skipped in the current scene - added to the scene time, so the reading time isn't shortened.
    private double _skippedTime;

    // Path set to Image last (kept loaded while the following scenes use the same image).
    private string _imagePath;

    // Image of the current scene (null = none) - shown when enough of its text is typed.
    private string _sceneImage;

    /// <summary>
    /// Image of the current scene. Hidden (see <see cref="HasImage"/>) when the scene has no image or doesn't show it
    /// yet.
    /// </summary>
    public ImageSource<ITexture> Image { get; } = new();

    /// <summary>
    /// True when the image of the current scene shows - bind it to the visibility of the image view. Turns false when
    /// a scene starts and true after <see cref="Parameters.ImageShowAtTypedFraction"/> of its text is typed (when the
    /// scene has an image) - so the Show transition of the view runs in every scene, even with the same image.
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

    /// <summary>
    /// Time of the current scene including the skipped typing time.
    /// </summary>
    private double CurrentSceneTime => _timings[_sceneIndex].Time + _skippedTime;
    
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
        while (!IsFinished && _sceneTime >= CurrentSceneTime)
        {
            NextScene(_sceneTime - CurrentSceneTime);
        }

        UpdateTypedText();
        UpdateSceneImage();
    }

    /// <summary>
    /// While the text is being typed, types it all at once - the skipped typing time is added to the scene, so the text
    /// stays displayed for its whole reading time plus the skipped time. When it's fully typed and has been displayed for
    /// <see cref="SkipToNextSceneDelay"/>, moves to the next scene (finishes after the last one).
    /// </summary>
    public void Skip()
    {
        if (IsFinished || _sceneIndex < 0)
            return;

        var typedTime = _timings[_sceneIndex].TypedTime;
        
        if (_sceneTime < typedTime)
        {
            _skippedTime += typedTime - _sceneTime;
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
        _skippedTime = 0;
        
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

        if (_scenes[index].Playlist is { } playlist)
        {
            musicPlayer?.ChangePlaylist(playlist);
        }

        // Hidden at once; shown again by UpdateSceneImage. Loaded now, while hidden (the same image stays loaded).
        _hasImage.SetValue(false);
        _sceneImage = ResolveImagePath(_scenes[index].Image?.Path);

        if (_sceneImage is not null && _sceneImage != _imagePath)
        {
            Image.SetSource(_sceneImage);
            _imagePath = _sceneImage;
        }
    }

    /// <summary>
    /// Shows the image of the scene once <see cref="Parameters.ImageShowAtTypedFraction"/> of its text is typed.
    /// </summary>
    private void UpdateSceneImage()
    {
        if (IsFinished || _sceneImage is null || _hasImage.Value)
            return;

        if (_sceneTime >= _imageShowAtTypedFraction * _timings[_sceneIndex].TypedTime)
        {
            _hasImage.SetValue(true);
        }
    }

    private void UpdateTypedText()
    {
        if (IsFinished)
            return;
        
        var length = _text.Length;
        
        // Clamped to the text length - the count doesn't change after the whole text is typed (the cursor blinks then).
        var letters = _lettersPerSecond > 0
            ? (int)Math.Min(_sceneTime * _lettersPerSecond, length)
            : length;

        var previous = VisibleCharacters.Value;
        VisibleCharacters.Value = letters;

        // Once per update, even if more letters show at once (a long frame, Skip).
        if (HasLetter(previous, letters))
        {
            _keySound?.Resource.PlayOnce();
        }
    }

    /// <summary>
    /// True if the characters of the text from <paramref name="start"/> (inclusive) to <paramref name="end"/>
    /// (exclusive) contain a letter - a visible character, not a whitespace.
    /// </summary>
    private bool HasLetter(int start, int end)
    {
        for (var idx = Math.Max(0, start); idx < end; idx++)
        {
            if (!char.IsWhiteSpace(_text[idx]))
                return true;
        }

        return false;
    }

    private void Finish()
    {
        IsFinished = true;
        ReleaseKeySound();
        _onFinished?.Invoke();
    }

    public void Dispose()
    {
        ReleaseKeySound();
    }

    private void ReleaseKeySound()
    {
        _keySound?.Dispose();
        _keySound = null;
    }

    /// <summary>
    /// Path of a scene image: a relative path is combined with <see cref="Parameters.ImagesBaseDirectory"/>.
    /// </summary>
    private string ResolveImagePath(string path)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(_imagesBaseDirectory))
            return path;

        if (path.Contains(":/") || path.StartsWith('/') || path.StartsWith('\\'))
            return path;

        return $"{_imagesBaseDirectory.TrimEnd('/', '\\')}/{path}";
    }

    /// <summary>
    /// Loads the sound, or returns null when there is no path or the file doesn't exist.
    /// </summary>
    private static ResourceHandle<ISoundEffect> LoadSound(IContentManager contentManager, string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            return contentManager.Get<ISoundEffect>(path);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
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
