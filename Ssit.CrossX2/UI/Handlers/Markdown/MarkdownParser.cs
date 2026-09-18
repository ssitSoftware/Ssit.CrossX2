using Ssit.CrossX2.UI.Views.Markdown;

namespace Ssit.CrossX2.UI.Handlers.Markdown;

// Markdown parser class created with Claude Code assistance
internal static class MarkdownParser
{
    public static List<MarkdownBlock> Parse(string text)
    {
        var blocks = new List<MarkdownBlock>();
        if (string.IsNullOrEmpty(text)) return blocks;

        var lines = text.Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            if (line.StartsWith("### "))
            {
                blocks.Add(CreateTextBlock(MarkdownBlockType.Header3, line.Substring(4), 4f));
                i++;
                continue;
            }

            if (line.StartsWith("## "))
            {
                blocks.Add(CreateTextBlock(MarkdownBlockType.Header2, line.Substring(3), 6f));
                i++;
                continue;
            }

            if (line.StartsWith("# "))
            {
                blocks.Add(CreateTextBlock(MarkdownBlockType.Header1, line.Substring(2), 8f));
                i++;
                continue;
            }

            if (line == "---" || line == "***" || line == "___")
            {
                blocks.Add(new MarkdownBlock { Type = MarkdownBlockType.Rule, MarginBottom = 4f });
                i++;
                continue;
            }

            if (line.StartsWith("> "))
            {
                var sb = new System.Text.StringBuilder();
                while (i < lines.Length)
                {
                    var l = lines[i].TrimEnd('\r');
                    if (!l.StartsWith("> ")) break;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(l.Substring(2));
                    i++;
                }
                blocks.Add(CreateTextBlock(MarkdownBlockType.Quote, sb.ToString(), 4f));
                continue;
            }

            if (line.StartsWith("```"))
            {
                i++;
                var codeSb = new System.Text.StringBuilder();
                while (i < lines.Length)
                {
                    var l = lines[i].TrimEnd('\r');
                    i++;
                    if (l.StartsWith("```")) break;
                    if (codeSb.Length > 0) codeSb.Append('\n');
                    codeSb.Append(l);
                }
                var codeBlock = new MarkdownBlock { Type = MarkdownBlockType.Code, MarginBottom = 4f };
                codeBlock.Spans.Add(new InlineSpan { Text = codeSb.ToString(), Style = MarkdownStyle.Code });
                blocks.Add(codeBlock);
                continue;
            }

            // Standalone image: ![alt](path)
            if (line.StartsWith("![") && line.Contains("](") && line.EndsWith(")"))
            {
                var pathStart = line.IndexOf("](", System.StringComparison.Ordinal) + 2;
                var path = line.Substring(pathStart, line.Length - pathStart - 1);
                blocks.Add(new MarkdownBlock { Type = MarkdownBlockType.Image, ImagePath = path, MarginBottom = 4f });
                i++;
                continue;
            }

            // Paragraph: accumulate until blank line or another block element
            {
                var sb = new System.Text.StringBuilder();
                while (i < lines.Length)
                {
                    var l = lines[i].TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(l)) break;
                    if (l.StartsWith("#") || l.StartsWith(">") || l.StartsWith("```") ||
                        (l.StartsWith("![") && l.Contains("](") && l.EndsWith(")")) ||
                        l == "---" || l == "***" || l == "___") break;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(l);
                    i++;
                }
                var block = new MarkdownBlock { Type = MarkdownBlockType.Paragraph, MarginBottom = 4f };
                block.Spans.AddRange(ParseInline(sb.ToString()));
                if (block.Spans.Exists(s => s.ImagePath != null))
                    block.Type = MarkdownBlockType.InlineImage;
                blocks.Add(block);
            }
        }

        return blocks;
    }

    private static MarkdownBlock CreateTextBlock(MarkdownBlockType type, string text, float marginBottom)
    {
        var block = new MarkdownBlock { Type = type, MarginBottom = marginBottom };
        block.Spans.AddRange(ParseInline(text));
        return block;
    }

    public static List<InlineSpan> ParseInline(string text)
    {
        var spans = new List<InlineSpan>();
        if (string.IsNullOrEmpty(text)) return spans;

        var i = 0;
        var sb = new System.Text.StringBuilder();

        while (i < text.Length)
        {
            // Bold-italic: ***
            if (i + 2 < text.Length && text[i] == '*' && text[i + 1] == '*' && text[i + 2] == '*')
            {
                FlushSpan(spans, sb);
                i += 3;
                var end = text.IndexOf("***", i, System.StringComparison.Ordinal);
                if (end < 0) { sb.Append("***"); continue; }
                spans.Add(new InlineSpan { Text = text.Substring(i, end - i), Style = MarkdownStyle.BoldItalic });
                i = end + 3;
                continue;
            }

            // Bold: **
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                FlushSpan(spans, sb);
                i += 2;
                var end = text.IndexOf("**", i, System.StringComparison.Ordinal);
                if (end < 0) { sb.Append("**"); continue; }
                spans.Add(new InlineSpan { Text = text.Substring(i, end - i), Style = MarkdownStyle.Bold });
                i = end + 2;
                continue;
            }

            // Italic: *
            if (text[i] == '*')
            {
                FlushSpan(spans, sb);
                i += 1;
                var end = text.IndexOf('*', i);
                if (end < 0) { sb.Append('*'); continue; }
                spans.Add(new InlineSpan { Text = text.Substring(i, end - i), Style = MarkdownStyle.Italic });
                i = end + 1;
                continue;
            }

            // Inline code: `
            if (text[i] == '`')
            {
                FlushSpan(spans, sb);
                i += 1;
                var end = text.IndexOf('`', i);
                if (end < 0) { sb.Append('`'); continue; }
                spans.Add(new InlineSpan { Text = text.Substring(i, end - i), Style = MarkdownStyle.Code });
                i = end + 1;
                continue;
            }

            // HTML entity: &nbsp;
            if (text[i] == '&' && i + 5 < text.Length && text.Substring(i, 6) == "&nbsp;")
            {
                sb.Append('\u00A0');
                i += 6;
                continue;
            }

            // Inline image: ![alt](path)
            if (text[i] == '!' && i + 1 < text.Length && text[i + 1] == '[')
            {
                var closeB = text.IndexOf(']', i + 2);
                if (closeB > i + 2 && closeB + 1 < text.Length && text[closeB + 1] == '(')
                {
                    var closeP = text.IndexOf(')', closeB + 2);
                    if (closeP > closeB + 2)
                    {
                        FlushSpan(spans, sb);
                        var imgPath = text.Substring(closeB + 2, closeP - closeB - 2);
                        spans.Add(new InlineSpan { ImagePath = imgPath, Style = MarkdownStyle.Paragraph });
                        i = closeP + 1;
                        continue;
                    }
                }
            }

            // Link: [text](url) — renders link text with Link style, ignores URL
            if (text[i] == '[')
            {
                var closeB = text.IndexOf(']', i);
                if (closeB > i && closeB + 1 < text.Length && text[closeB + 1] == '(')
                {
                    var closeP = text.IndexOf(')', closeB + 2);
                    if (closeP > closeB + 2)
                    {
                        FlushSpan(spans, sb);
                        var linkText = text.Substring(i + 1, closeB - i - 1);
                        spans.Add(new InlineSpan { Text = linkText, Style = MarkdownStyle.Link });
                        i = closeP + 1;
                        continue;
                    }
                }
            }

            sb.Append(text[i]);
            i++;
        }

        FlushSpan(spans, sb);
        return spans;
    }

    private static void FlushSpan(List<InlineSpan> spans, System.Text.StringBuilder sb)
    {
        if (sb.Length > 0)
        {
            spans.Add(new InlineSpan { Text = sb.ToString(), Style = MarkdownStyle.Paragraph });
            sb.Clear();
        }
    }
}
