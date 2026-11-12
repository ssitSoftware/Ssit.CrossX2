using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Cached text layout (lines and size). Reused between frames so text is laid out again only when
/// text, font, spacing, width or line spacing change (see <see cref="IsValid"/>).
/// </summary>
public class TextRenderingContext
{
    /// <summary>
    /// One laid-out line.
    /// </summary>
    public struct LineDefinition
    {
        /// <summary>
        /// Line width in pixels.
        /// </summary>
        public float Width;
        /// <summary>
        /// Text of the line.
        /// </summary>
        public TextSource Text;
        /// <summary>
        /// Number of whitespace characters (used for justification).
        /// </summary>
        public int Whitespaces;
        /// <summary>
        /// True if the line ends a paragraph (not justified).
        /// </summary>
        public bool EndOfParagraph;
        /// <summary>
        /// Extra vertical space after the line (paragraph/line spacing).
        /// </summary>
        public float Spacing;
    }

    private List<char> _text;
    private IGlyphFont _font;
    private TextSpacing _spacing;
    private int _textHashCode = -1;
    private int _targetWidth;
    
    /// <summary>
    /// Laid-out lines.
    /// </summary>
    public List<LineDefinition> Lines { get; } = new ();

    private float _width;
    private float _height;
    private int _lineSpacing;

    internal IGlyphFont Font => _font;
    
    /// <summary>
    /// Wrapping width used for the layout (0 = no wrapping).
    /// </summary>
    public float TargetWidth => _targetWidth;
    
    /// <summary>
    /// Width of the widest line (computed lazily).
    /// </summary>
    public float Width
    {
        get
        {
            lock (this)
            {
                if (_width < 0)
                {
                    var width = 0f;
                    for (int i = 0; i < Lines.Count; i++)
                    {
                        width = MathF.Max(Lines[i].Width, width);
                    }

                    _width = width;
                }

                return _width;
            }
        }
    }
    
    /// <summary>
    /// Total height of all lines including spacing (computed lazily).
    /// </summary>
    public float Height
    {
        get
        {
            lock (this)
            {
                if (_height <= 0)
                {
                    float height = Lines.Count * _font?.Metrics?.LineHeight ?? 0;
                    for (int idx = 0; idx < Lines.Count; idx++)
                    {
                        height += Lines[idx].Spacing;
                    }

                    _height = height;
                }

                return _height;
            }
        }
    }

    /// <summary>
    /// When enabled, a copy of the text is kept and compared character by character in <see cref="IsValid"/>;
    /// otherwise only hash code and length are compared (cheaper, may miss rare changes).
    /// </summary>
    public bool FullStringCheckEnabled
    {
        get;
        set
        {
            field = value;
            if (!field)
            {
                _text = null;
            }
        }
    }

    /// <summary>
    /// Starts a new layout for the given inputs: stores them and clears the lines (filled in by the layout code).
    /// </summary>
    public void Update(TextSource text, IGlyphFont font, TextSpacing spacing, int targetWidth = 0, int lineSpacing = 0)
    {
        if (FullStringCheckEnabled)
        {
            _text ??= new List<char>();
            _text.Clear();
            
            for (var idx = 0; idx < text.Length; idx++)
            {
                _text.Add(text[idx]);
            }
        }

        _targetWidth = targetWidth;
        _lineSpacing = lineSpacing;
        _font = font;
        Lines.Clear();
        _spacing = spacing;
        _width = -1;
        _height = -1;

        _textHashCode = text.GetHashCode();
    }

    private bool IsTextEqualQuick(TextSource text)
    {
        if (_text is null)
        {
            return false;
        }
        
        if (text.Length != _text.Count)
        {
            return false;
        }

        return true;
    }

    private bool IsTextEqual(TextSource text)
    {
        if (!IsTextEqualQuick(text))
        {
            return false;
        }
        
        for(var idx =0; idx < text.Length; idx++)
        {
            if (text[idx] != _text[idx])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Invalidates the cached layout.
    /// </summary>
    public void Reset()
    {
        _targetWidth = -1;
        _textHashCode = 0;
        _text = null;
        _font = null;
        _spacing = default;
        _width = -1;
        _height = -1;
        Lines.Clear();
    }

    /// <summary>
    /// True if the cached layout can be reused for these inputs.
    /// </summary>
    public bool IsValid(TextSource text, IGlyphFont font, TextSpacing spacing, int targetWidth = 0, int lineSpacing = 0)
    {
        if (_targetWidth != targetWidth)
        {
            // A layout without wrapping is still valid for a wider target as long as the text fits
            if(_targetWidth != 0) return false;

            if (targetWidth > 0 && Width > targetWidth + font.Metrics.WhitespaceWidth)
            {
                return false;
            }
        }

        if (_lineSpacing != lineSpacing)
        {
            return false;
        }
        
        if (spacing != _spacing)
        {
            return false;
        }
        
        if (!ReferenceEquals(font, _font))
        {
            return false;
        }

        if (_textHashCode != text.GetHashCode())
        {
            return false;
        }
        
        if (FullStringCheckEnabled)
        {
            if (!IsTextEqual(text))
            {
                return false;
            }
        }
        else if (!IsTextEqualQuick(text))
        {
            return false;
        }

        return true;
    }
}