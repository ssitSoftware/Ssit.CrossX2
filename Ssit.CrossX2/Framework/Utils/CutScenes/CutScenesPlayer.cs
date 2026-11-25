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
        /// Time (seconds) of the image fade out and of the fade in when the image changes - see <see cref="ImageOpacity"/>.
        /// 0 or less = the images change at once. Default: 0.5 s.
        /// </summary>
        public float ImageFadeTime { get; init; } = 0.5f;

        /// <summary>
        /// Part of the typing of the scene text (0 - 1) after which a new image of the scene starts to show (fade in):
        /// 0 = when the typing starts, 1 = when the whole text is typed. The previous image fades out right when the
        /// scene starts; an image the scene shares with the previous one stays. Default: 0.5.
        /// </summary>
        public float ImageShowAtTypedFraction { get; init; } = 0.5f;

        /// <summary>
        /// Time (seconds) the text of the previous scene fades out before the text of the new scene starts (see
        /// <see cref="TextOpacity"/>). The new text doesn't fade in - its cursor shows right away. 0 or less = no fading.
        /// Default: 0.5 s.
        /// </summary>
        public float TextFadeTime { get; init; } = 0.5f;

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
    private readonly float _imageFadeTime = parameters.ImageFadeTime;
    private readonly float _imageShowAtTypedFraction = Math.Clamp(parameters.ImageShowAtTypedFraction, 0f, 1f);
    private readonly float _textFadeTime = parameters.TextFadeTime;
    
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

    // Fade out time of the previous text at the start of the current scene (0 = none) - the text of the scene is set
    // (and starts typing) after it.
    private double _textDelay;
    private bool _textApplied;

    // Path set to Image last (kept loaded while the following scenes use the same image).
    private string _imagePath;

    // Image of the current scene - becomes the target when enough of the text is typed.
    private string _sceneImage;

    // Image wanted now and the image displayed now - they differ while the displayed one fades out.
    private string _targetImage;
    private string _shownImage;

    /// <summary>
    /// Image of the current scene. Hidden (see <see cref="HasImage"/>) when the scene has no image.
    /// </summary>
    public ImageSource<ITexture> Image { get; } = new();

    /// <summary>
    /// True when the current scene has an image - bind it to the visibility of the image view.
    /// </summary>
    public SharedBool HasImage => _hasImage;

    /// <summary>
    /// Opacity of the image (0 - 1) - bind it to the opacity of the image view. When the scene changes the image,
    /// the displayed image fades out and the new one fades in, each in <see cref="Parameters.ImageFadeTime"/>.
    /// </summary>
    public SharedValue<float> ImageOpacity { get; } = new(0f);

    /// <summary>
    /// Whole text of the current scene - set once when the scene starts.
    /// </summary>
    public SharedString Text => _text;

    /// <summary>
    /// Number of the typed characters of <see cref="Text"/>. Stops at the text length when the whole text is typed.
    /// </summary>
    public SharedValue<int> VisibleCharacters { get; } = new(0);

    /// <summary>
    /// Opacity of the text (0 - 1) - bind it to the opacity of the text view. When a scene starts, the text of the
    /// previous scene fades out (<see cref="Parameters.TextFadeTime"/>); the new text is fully opaque from the start.
    /// </summary>
    public SharedValue<float> TextOpacity { get; } = new(1f);
    
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
    private double CurrentSceneTime => _textDelay + _timings[_sceneIndex].Time + _skippedTime;
    
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
        UpdateImage(dt);
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

        // Fading out the previous text - jump to the start of the new one (the scene time isn't extended).
        if (_sceneTime < _textDelay)
        {
            _sceneTime = _textDelay;
            UpdateTypedText();
            return;
        }

        var typedTime = _textDelay + _timings[_sceneIndex].TypedTime;
        
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

        // The previous text (if any) fades out first - UpdateTypedText sets the new one after that.
        _textDelay = _textFadeTime > 0 && _text.Length > 0 ? _textFadeTime : 0;
        _textApplied = false;

        if (_textDelay <= 0)
        {
            ApplySceneText();
        }

        if (_scenes[index].Playlist is { } playlist)
        {
            musicPlayer?.ChangePlaylist(playlist);
        }

        // The same image stays. Another one (or none): the displayed image fades out now and the new one is shown
        // by UpdateSceneImage when enough of the text is typed.
        _sceneImage = ResolveImagePath(_scenes[index].Image?.Path);
        _targetImage = _sceneImage == _shownImage ? _sceneImage : null;
    }

    /// <summary>
    /// Makes the image of the scene the target (UpdateImage fades it in) once <see cref="Parameters.ImageShowAtTypedFraction"/>
    /// of the text is typed.
    /// </summary>
    private void UpdateSceneImage()
    {
        if (IsFinished || _targetImage == _sceneImage || !_textApplied)
            return;

        var typingTime = _sceneTime - _textDelay;

        if (typingTime >= _imageShowAtTypedFraction * _timings[_sceneIndex].TypedTime)
        {
            _targetImage = _sceneImage;
        }
    }

    /// <summary>
    /// Fades the displayed image out while the scene wants another one (or none), then switches to the wanted image
    /// and fades it in.
    /// </summary>
    private void UpdateImage(float dt)
    {
        if (IsFinished)
            return;

        var step = _imageFadeTime > 0 ? dt / _imageFadeTime : 1f;

        if (_shownImage != _targetImage)
        {
            var opacity = _shownImage is null ? 0 : Math.Max(0, ImageOpacity.Value - step);
            ImageOpacity.Value = opacity;

            if (opacity > 0)
                return;

            _shownImage = _targetImage;

            // The same image as before stays loaded.
            if (_shownImage is not null && _shownImage != _imagePath)
            {
                Image.SetSource(_shownImage);
                _imagePath = _shownImage;
            }

            _hasImage.SetValue(_shownImage is not null);

            // Fading in starts with the next update.
            return;
        }

        if (_shownImage is not null)
        {
            ImageOpacity.Value = Math.Min(1, ImageOpacity.Value + step);
        }
    }

    /// <summary>
    /// Sets the text of the current scene - fully opaque, nothing typed yet (the cursor shows at once).
    /// </summary>
    private void ApplySceneText()
    {
        _textApplied = true;

        // Nothing visible before the new text is set - the old count would show a part of it at once.
        VisibleCharacters.Value = 0;
        _text.SetText(_timings[_sceneIndex].Text is { } text ? text : string.Empty);
        TextOpacity.Value = 1;
    }

    private void UpdateTypedText()
    {
        if (IsFinished)
            return;

        if (!_textApplied)
        {
            // Fading out the previous text.
            if (_sceneTime < _textDelay)
            {
                TextOpacity.Value = (float)(1 - _sceneTime / _textDelay);
                return;
            }

            ApplySceneText();
        }
        
        var length = _text.Length;
        var typingTime = _sceneTime - _textDelay;
        
        // Clamped to the text length - the count doesn't change after the whole text is typed (the cursor blinks then).
        var letters = _lettersPerSecond > 0
            ? (int)Math.Min(typingTime * _lettersPerSecond, length)
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
