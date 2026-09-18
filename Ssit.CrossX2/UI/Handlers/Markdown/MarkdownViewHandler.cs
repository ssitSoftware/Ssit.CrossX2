using System.Numerics;
using Ssit.CrossX2.Content;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Font;
using Ssit.CrossX2.Text;
using Ssit.CrossX2.UI.Parameters;
using Ssit.CrossX2.UI.Values;
using Ssit.CrossX2.UI.Views;
using Ssit.CrossX2.UI.Views.Markdown;

namespace Ssit.CrossX2.UI.Handlers.Markdown;

// MarkdownView handler class created with Claude Code assistance
public class MarkdownViewHandler<TMarkdownView> : BackgroundHandler<TMarkdownView> where TMarkdownView: MarkdownView
{
    private readonly IFontsManager _fontsManager;
    private readonly IContentManager _contentManager;
    private readonly IColorSource _colorSource;

    private readonly List<LayoutLine> _layoutLines = new();
    private readonly Dictionary<string, ResourceHandle<ITexture>> _textures = new();

    private List<MarkdownBlock> _blocks = new();
    private float _totalHeight;
    private float _lastLayoutWidth = -1;

    public MarkdownViewHandler(
        CreateHandlerParameters parameters,
        IFontsManager fontsManager,
        IContentManager contentManager)
        : base(parameters)
    {
        _fontsManager = fontsManager;
        _contentManager = contentManager;
        _colorSource = parameters.Parent?.GetParent<IColorSource>(true);

        if (AttachedView.Text != null)
        {
            AttachedView.Text.TextChanged += OnTextChanged;
        }
    }

    public override void Init() => OnTextChanged();

    private void OnTextChanged()
    {
        _blocks = MarkdownParser.Parse(AttachedView.Text?.ToString() ?? "");
        _lastLayoutWidth = -1;
        _layoutLines.Clear();
        _totalHeight = 0;

        Parent?.RecalculateLayout(AttachedView);
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        width = AttachedView.Width ?? Length.Fill;

        if (AttachedView.Height is { IsAuto: false })
        {
            height = AttachedView.Height.Value;
            return;
        }

        // Resolve a reference pixel width for height pre-computation.
        // Fixed explicit widths are known immediately; Fill widths become known
        // only after SetBounds has run once and updated ScreenBounds.
        float refPixelWidth;
        if (width is { IsAuto: false, Percent: 0, Value: > 0 })
            refPixelWidth = width.Calculate(CurrentScale, 0);
        else if (ScreenBounds.Width > 0)
            refPixelWidth = ScreenBounds.Width;
        else
            refPixelWidth = 0;

        if (refPixelWidth > 0)
        {
            var (padL, padR) = GetHorizontalPadding(refPixelWidth);
            var contentWidth = MathF.Max(refPixelWidth - padL - padR, 0f);
            if (contentWidth > 0 && MathF.Abs(contentWidth - _lastLayoutWidth) > 0.5f)
            {
                LayoutContent(contentWidth);
            }
        }

        if (_totalHeight > 0)
        {
            var (padT, padB) = GetVerticalPadding();
            height = new Length(pixels: _totalHeight + padT + padB);
        }
        else
        {
            height = Length.Auto;
        }
    }

    public override void SetBounds(RectangleF rectangleF)
    {
        base.SetBounds(rectangleF);

        var contentRect = ContentRect;
        var w = contentRect.Width;

        if (w > 0 && MathF.Abs(w - _lastLayoutWidth) > 0.5f)
        {
            LayoutContent(w);
            Parent?.RecalculateLayout(AttachedView);
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);

        var textColor = AttachedView.TextColor?.GetColor(renderer, _colorSource) ?? RgbaColor.White;
        var outlineColor = AttachedView.TextOutlineColor?.GetColor(renderer, _colorSource);

        var align = AttachedView.TextAlign;
        var cr = ContentRect;
        var contentWidth = cr.Width;

        var originX = cr.X;
        var originY = cr.Y;

        // Vertical offset — only when height is explicitly set
        if (AttachedView.Height.HasValue && _totalHeight < cr.Height)
        {
            if ((align & ContentAlign.VCenter) == ContentAlign.VCenter)
                originY += (cr.Height - _totalHeight) / 2f;
            else if ((align & ContentAlign.Bottom) == ContentAlign.Bottom)
                originY += cr.Height - _totalHeight;
        }

        var isJustified = (align & ContentAlign.Justified) == ContentAlign.Justified;
        var isRight     = (align & ContentAlign.Right)     == ContentAlign.Right;
        var isCenter    = (align & ContentAlign.Center)    == ContentAlign.Center;

        foreach (var line in _layoutLines)
        {
            // Horizontal offset for this line
            float lineOffsetX;
            if (isJustified && !line.IsLastInBlock && line.Pieces.Count > 1)
                lineOffsetX = 0; // justified: handled per-piece below
            else if (isRight)
                lineOffsetX = contentWidth - line.Width;
            else if (isCenter)
                lineOffsetX = (contentWidth - line.Width) / 2f;
            else
                lineOffsetX = 0;

            var justifiedGap = isJustified && !line.IsLastInBlock && line.Pieces.Count > 1
                ? (contentWidth - line.Width) / (line.Pieces.Count - 1)
                : 0f;

            for (var pi = 0; pi < line.Pieces.Count; pi++)
            {
                var piece = line.Pieces[pi];
                var pieceX = piece.X + lineOffsetX + pi * justifiedGap;

                if (piece.Texture != null)
                {
                    var targetRect = new RectangleF(
                        originX + pieceX,
                        originY + line.Y + (line.Height - piece.TextureSize.Height) / 2f,
                        piece.TextureSize.Width,
                        piece.TextureSize.Height);
                    renderer.SpriteRenderer.Draw(piece.Texture, targetRect);
                }
                else if (piece.Text != null && piece.Font != null)
                {
                    var pos = new Vector2(originX + pieceX, originY + line.Y + line.Height / 2f);
                    renderer.TextRenderer.DrawText(
                        font: piece.Font,
                        text: (TextSource)piece.Text,
                        position: pos,
                        align: ContentAlign.Left | ContentAlign.VCenter,
                        scale: piece.FontScale,
                        color: textColor,
                        outlineColor: outlineColor);
                }
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (AttachedView.Text != null)
        {
            AttachedView.Text.TextChanged -= OnTextChanged;
        }

        foreach (var handle in _textures.Values)
        {
            handle.Dispose();
        }
        _textures.Clear();
    }

    // ── Padding helpers ───────────────────────────────────────────────────────

    private RectangleF ContentRect
    {
        get
        {
            var x = ScreenBounds.X;
            var y = ScreenBounds.Y;
            var w = ScreenBounds.Width;
            var h = ScreenBounds.Height;

            var padding = AttachedView.Padding;
            if (padding.HasValue)
            {
                var p = padding.Value;
                var pl = p.Left?.Calculate(CurrentScale, w) ?? 0f;
                var pr = p.Right?.Calculate(CurrentScale, w) ?? 0f;
                var pt = p.Top?.Calculate(CurrentScale, h) ?? 0f;
                var pb = p.Bottom?.Calculate(CurrentScale, h) ?? 0f;
                x += pl; y += pt; w -= pl + pr; h -= pt + pb;
            }

            return new RectangleF(x, y, MathF.Max(w, 0f), MathF.Max(h, 0f));
        }
    }

    private (float top, float bottom) GetVerticalPadding()
    {
        var padding = AttachedView.Padding;
        if (!padding.HasValue) return (0f, 0f);
        var p = padding.Value;
        return (p.Top?.Calculate(CurrentScale, 0f) ?? 0f,
                p.Bottom?.Calculate(CurrentScale, 0f) ?? 0f);
    }

    private (float left, float right) GetHorizontalPadding(float refWidth)
    {
        var padding = AttachedView.Padding;
        if (!padding.HasValue) return (0f, 0f);
        var p = padding.Value;
        return (p.Left?.Calculate(CurrentScale, refWidth) ?? 0f,
                p.Right?.Calculate(CurrentScale, refWidth) ?? 0f);
    }

    // ── Font helpers ──────────────────────────────────────────────────────────

    private (IFont font, float scale) GetStyledFont(MarkdownStyle style)
    {
        var mapper = AttachedView.Mapper;
        FontDesc desc = mapper != null
            ? mapper.GetFont(style)
            : new FontDesc { FontFamily = "Default", FontSize = 12 };

        var requestedSize = desc.FontSize > 0 ? desc.FontSize : 12f;

        if (AttachedView.Scaling == TextScaling.Default)
        {
            requestedSize = MathF.Ceiling(requestedSize * CurrentScale);
        }

        var font = _fontsManager.GetFont(desc.FontFamily ?? "Default", requestedSize);

        float scale = AttachedView.Scaling == TextScaling.Pixel
            ? CurrentScale
            : requestedSize / MathF.Max(1f, font.Size);

        return (font, scale);
    }

    private static MarkdownStyle BlockTypeToStyle(MarkdownBlockType type) => type switch
    {
        MarkdownBlockType.Header1  => MarkdownStyle.Header1,
        MarkdownBlockType.Header2  => MarkdownStyle.Header2,
        MarkdownBlockType.Header3  => MarkdownStyle.Header3,
        MarkdownBlockType.Quote    => MarkdownStyle.Quote,
        MarkdownBlockType.Code     => MarkdownStyle.Code,
        _                          => MarkdownStyle.Paragraph,
    };

    // ── Layout ────────────────────────────────────────────────────────────────

    private void LayoutContent(float maxWidth)
    {
        _lastLayoutWidth = maxWidth;
        _layoutLines.Clear();
        _totalHeight = 0;

        if (maxWidth <= 0) return;

        var y = 0f;
        var linePieces = new List<LayoutPiece>();
        var currentX = 0f;
        var lineHeight = 0f;

        var lineSpacing = AttachedView.LineSpacing >= 0f ? AttachedView.LineSpacing : 0f;

        void FinishLine(float extraSpacing = 0f)
        {
            if (linePieces.Count > 0 || lineHeight > 0)
            {
                var hasImage = linePieces.Exists(p => p.Texture != null);
                _layoutLines.Add(new LayoutLine
                {
                    Y = y,
                    Height = lineHeight,
                    Width = currentX,
                    Pieces = new List<LayoutPiece>(linePieces),
                });
                y += lineHeight + (hasImage ? 0f : extraSpacing);
                linePieces.Clear();
                currentX = 0f;
                lineHeight = 0f;
            }
        }

        void LayoutSpanWords(string spanText, IFont font, float fontScale, float spanLineH)
        {
            var lines = spanText.Split('\n');
            var spaceWidth = font.TextSize((TextSource)" ").Width * fontScale;

            for (var li = 0; li < lines.Length; li++)
            {
                var wordParts = lines[li].Split(' ');

                foreach (var wordPart in wordParts)
                {
                    if (wordPart.Length == 0) continue;

                    var wordWidth = font.TextSize((TextSource)NormalizeSpaces(wordPart)).Width * fontScale;
                    var spaceBefore = currentX > 0 ? spaceWidth : 0f;

                    if (currentX + spaceBefore + wordWidth > maxWidth + 0.5f && currentX > 0)
                    {
                        lineHeight = MathF.Max(lineHeight, spanLineH);
                        FinishLine(lineSpacing);
                        spaceBefore = 0f;
                    }

                    currentX += spaceBefore;

                    linePieces.Add(new LayoutPiece
                    {
                        X = currentX,
                        Width = wordWidth,
                        Text = NormalizeSpaces(wordPart),
                        Font = font,
                        FontScale = fontScale,
                    });
                    currentX += wordWidth;
                    lineHeight = MathF.Max(lineHeight, spanLineH);
                }

                // Explicit newline between lines (not after last)
                if (li < lines.Length - 1)
                {
                    lineHeight = MathF.Max(lineHeight, spanLineH);
                    FinishLine(lineSpacing);
                }
            }
        }

        void LayoutSpanImage(string imagePath, bool includeImageHeightToLineHeight)
        {
            var texture = GetOrLoadTexture(imagePath);
            if (texture == null) return;

            var textureWidth = (float)texture.Size.Width;
            var textureHeight = (float)texture.Size.Height;

            var s = AttachedView.Scaling != TextScaling.None ? CurrentScale : 1f;
            var displayWidth = textureWidth * s;
            var displayHeight = textureHeight * s;

            if (displayWidth > maxWidth)
            {
                s = maxWidth / textureWidth;
                displayWidth = maxWidth;
                displayHeight = textureHeight * s;
            }

            if (currentX + displayWidth > maxWidth + 0.5f && currentX > 0)
            {
                FinishLine(lineSpacing);
            }

            linePieces.Add(new LayoutPiece
            {
                X = currentX,
                Width = displayWidth,
                Texture = texture,
                TextureSize = new SizeF(displayWidth, displayHeight),
            });
            currentX += displayWidth;

            if (includeImageHeightToLineHeight)
            {
                lineHeight = MathF.Max(lineHeight, displayHeight);
            }
        }

        for (var blockIdx = 0; blockIdx < _blocks.Count; blockIdx++)
        {
            var block = _blocks[blockIdx];

            if (block.Type == MarkdownBlockType.Image)
            {
                var texture = GetOrLoadTexture(block.ImagePath);
                if (texture != null)
                {
                    var textureWidth = (float)texture.Size.Width;
                    var textureHeight = (float)texture.Size.Height;
                    var s = AttachedView.Scaling != TextScaling.None ? CurrentScale : 1f;
                    var displayWidth = textureWidth * s;
                    var displayHeight = textureHeight * s;

                    if (displayWidth > maxWidth)
                    {
                        s = maxWidth / textureWidth;
                        displayWidth = maxWidth;
                        displayHeight = textureHeight * s; }

                    _layoutLines.Add(new LayoutLine
                    {
                        Y = y,
                        Height = displayHeight,
                        Width = displayWidth,
                        Pieces =
                        {
                            new LayoutPiece
                            {
                                X = 0,
                                Width = displayWidth,
                                Texture = texture,
                                TextureSize = new SizeF(displayWidth, displayHeight),
                            }
                        },
                    });
                    y += displayHeight;
                }

                y += AttachedView.ParagraphSpacing >= 0f ? AttachedView.ParagraphSpacing : block.MarginBottom;
                continue;
            }

            if (block.Type == MarkdownBlockType.Rule)
            {
                _layoutLines.Add(new LayoutLine { Y = y, Height = 2f });
                y += 2f + (AttachedView.ParagraphSpacing >= 0f ? AttachedView.ParagraphSpacing : block.MarginBottom);
                continue;
            }

            // Add top spacing for header blocks (half of paragraph spacing)
            var isHeader = block.Type is MarkdownBlockType.Header1 or MarkdownBlockType.Header2 or MarkdownBlockType.Header3;
            if (isHeader && blockIdx > 0)
            {
                var paragraphSpacing = AttachedView.ParagraphSpacing >= 0f ? AttachedView.ParagraphSpacing : block.MarginBottom;
                y += paragraphSpacing / 2f;
            }

            // Text block
            var blockStyle = BlockTypeToStyle(block.Type);
            var blockStartLineIdx = _layoutLines.Count;
            var yBeforeBlock = y;

            foreach (var span in block.Spans)
            {
                if (span.ImagePath != null)
                {
                    LayoutSpanImage(span.ImagePath, false);
                    continue;
                }

                var style = span.Style == MarkdownStyle.Paragraph ? blockStyle : span.Style;
                var (font, fontScale) = GetStyledFont(style);
                var spanLineH = font.LineSize * fontScale;

                if (block.Type == MarkdownBlockType.Code)
                {
                    var codeLines = span.Text.Split('\n');
                    for (var cli = 0; cli < codeLines.Length; cli++)
                    {
                        var codeLine = codeLines[cli];
                        if (codeLine.Length > 0)
                        {
                            var cw = font.TextSize((TextSource)codeLine).Width * fontScale;
                            linePieces.Add(new LayoutPiece { X = 0, Width = cw, Text = codeLine, Font = font, FontScale = fontScale });
                        }
                        lineHeight = MathF.Max(lineHeight, spanLineH);
                        FinishLine(cli < codeLines.Length - 1 ? lineSpacing : 0f);
                    }
                }
                else
                {
                    LayoutSpanWords(span.Text, font, fontScale, spanLineH);
                }
            }

            FinishLine();

            if (_layoutLines.Count > 0)
                _layoutLines[^1].IsLastInBlock = true;

            // Equalize all line heights within this block to the maximum.
            var blockLineCount = _layoutLines.Count - blockStartLineIdx;
            if (blockLineCount > 1)
            {
                var maxH = 0f;
                for (var li = blockStartLineIdx; li < _layoutLines.Count; li++)
                    maxH = MathF.Max(maxH, _layoutLines[li].Height);

                var newY = yBeforeBlock;
                for (var li = blockStartLineIdx; li < _layoutLines.Count; li++)
                {
                    _layoutLines[li].Y = newY;
                    _layoutLines[li].Height = maxH;
                    newY += maxH + (_layoutLines[li].IsLastInBlock ? 0f : lineSpacing);
                }
                y = newY;
            }

            y += AttachedView.ParagraphSpacing >= 0f ? AttachedView.ParagraphSpacing : block.MarginBottom;
        }

        _totalHeight = y;
    }

    // ── Image loading ─────────────────────────────────────────────────────────

    private ITexture GetOrLoadTexture(string path)
    {
        if (path == null) 
            return null;
        
        if (_textures.TryGetValue(path, out var cached)) 
            return cached.Resource;

        try
        {
            var resourcePath = AttachedView?.Mapper?.GetImagePath(path) ?? path;
            var handle = _contentManager.Get<ITexture>(resourcePath);
            _textures[path] = handle;
            return handle.Resource;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeSpaces(string s) => s.Replace('\u00A0', ' ');
}
