// using System.Numerics;
// using Ssit.CrossX.Graphics.Font;
// using Ssit.CrossX.Text;
//
// namespace Ssit.CrossX.Graphics.Internal;
//
// public abstract class RendererBase : IRenderer, IUnsafeRenderer
// {
//     public int DrawCalls { get; protected set; }
//     
//     public IUnsafeRenderer Unsafe => this;
//
//     public RendererStateManager StateManager { get; } = new();
//     public abstract IRenderTarget CurrentRenderTarget { get; }
//     
//     protected enum BatchMode
//     {
//         None,
//         ColorBuffer,
//         TextureBuffer
//     }
//     
//     protected DynamicVertexBuffer<VertexPositionColor> ColorVertexBuffer { get; } = new(64 * 1024, VertexPositionColor.Size);
//     protected DynamicVertexBuffer<VertexPositionColorTexture> TextureVertexBuffer { get; } = new(64 * 1024, VertexPositionColorTexture.Size);
//     
//     protected BatchMode CurrentBatchMode { get; set; }
//     
//     public abstract Size TargetSize { get; }
//
//     protected Matrix4x4? WorldTransform => StateManager.WorldTransform;
//
//     protected BlendMode BlendMode = BlendMode.AlphaBlend;
//     
//     public abstract void SetRenderTarget(IRenderTarget renderTarget);
//
//     protected RendererBase()
//     {
//         StateManager.ShouldFlush += UpdateStates;
//     }
//
//     private void UpdateStates()
//     {
//         Flush();
//         SetClipRect(StateManager.ClipRect);
//     }
//
//     public void SetBlendMode(BlendMode blendMode)
//     {
//         Flush();
//         BlendMode = blendMode;
//     }
//
//     public abstract void Clear(RgbaColor color);
//
//     public abstract void Flush();
//
//     public abstract void SetClipRect(Rectangle? rect);
//
//     protected abstract void PrepareRendering(ITexture texture, IEffect effect, VertexMode vertexMode,
//         TextureFilter filter, RenderMode renderMode);
//
//     public void DrawText(IFont font, TextSource text, Vector2 position, ContentAlign align = ContentAlign.Left, float scale = 1, RgbaColor? color = null,
//         TextSpacing spacing = TextSpacing.Normal, float depth = 0, RgbaColor? outlineColor = null, TextRenderingContext context = null)
//     {
//         if (font is not IGlyphFont glyphFont)
//         {
//             return;
//         }
//         
//         GlyphFontRenderer.RenderText(this, glyphFont, text, position, align, scale, color ?? RgbaColor.White, outlineColor ?? RgbaColor.Black, spacing, depth, context);
//     }
//
//     public void DrawText(IFont font, TextSource text, RectangleF position, ContentAlign align = ContentAlign.Left, float scale = 1,
//         RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, float paragraphSpacing = -1, float depth = 0, RgbaColor? outlineColor = null, TextRenderingContext context = null)
//     {
//         if (font is not IGlyphFont glyphFont)
//         {
//             return;
//         }
//
//         if (paragraphSpacing < 0)
//         {
//             paragraphSpacing = glyphFont.Metrics.LineHeight / 4f;
//         }
//         
//         GlyphFontRenderer.RenderText(this, glyphFont, text, position, align, scale, color ?? RgbaColor.White, outlineColor ?? RgbaColor.Black, spacing, paragraphSpacing, depth, context);
//     }
//
//     public void BeginRender(ITexture texture, TextureFilter textureFilter)
//     {
//         if (CurrentBatchMode != BatchMode.TextureBuffer)
//         {
//             Flush();
//         }
//         
//         PrepareRendering(texture, null, VertexPositionColorTexture.Mode, textureFilter, RenderMode.Normal);
//         
//         CurrentBatchMode = BatchMode.TextureBuffer;
//     }
//
//     public void FastDrawQuad(ITexture texture, Rectangle target, Rectangle source, RgbaColor color, float depth = 0)
//     {
//         if (TextureVertexBuffer.Offset + 6 > TextureVertexBuffer.Size)
//         {
//             Flush();
//         }
//         
//         var t00 = new Vector2((float) source.X / texture.Size.Width, (float) source.Y / texture.Size.Height);
//         var t10 = new Vector2((float) source.Right / texture.Size.Width, (float) source.Y / texture.Size.Height);
//         var t11 = new Vector2((float) source.Right / texture.Size.Width, (float) source.Bottom / texture.Size.Height);
//         var t01 = new Vector2((float) source.X / texture.Size.Width, (float) source.Bottom / texture.Size.Height);
//         
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(target.X, target.Y, depth), color, t00));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(target.X, target.Y + target.Height, depth), color, t01));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(target.X + target.Width, target.Y + target.Height, depth), color, t11));
//         
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(target.X, target.Y, depth), color, t00));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(target.X + target.Width, target.Y + target.Height, depth), color, t11));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(target.X + target.Width, target.Y, depth), color, t10));
//     }
//
//     public virtual void DrawTexture(ITexture texture, RectangleF targetRectangle,
//         Rectangle? sourceRectangle = null, RgbaColor? color = null,
//         ImageTransform imageTransform = ImageTransform.None,
//         TextureFilter textureFilter = TextureFilter.Nearest, IEffect effect = null, float depth = 0)
//     {
//         if (CurrentBatchMode != BatchMode.TextureBuffer)
//         {
//             Flush();
//         }
//
//         if (TextureVertexBuffer.Offset + 6 > TextureVertexBuffer.Size)
//         {
//             Flush();
//         }
//         
//         PrepareRendering(texture, effect, VertexPositionColorTexture.Mode, textureFilter, RenderMode.Normal);
//
//         CurrentBatchMode = BatchMode.TextureBuffer;
//
//         var col = color ?? RgbaColor.White;
//
//         var t00 = new Vector2(0, 0);
//         var t10 = new Vector2(1, 0);
//         var t11 = new Vector2(1, 1);
//         var t01 = new Vector2(0, 1);
//
//         if (sourceRectangle.HasValue)
//         {
//             t00 = new Vector2((float) sourceRectangle.Value.X / texture.Size.Width, (float) sourceRectangle.Value.Y / texture.Size.Height);
//             t10 = new Vector2((float) sourceRectangle.Value.Right / texture.Size.Width, (float) sourceRectangle.Value.Y / texture.Size.Height);
//             t11 = new Vector2((float) sourceRectangle.Value.Right / texture.Size.Width, (float) sourceRectangle.Value.Bottom / texture.Size.Height);
//             t01 = new Vector2((float) sourceRectangle.Value.X / texture.Size.Width, (float) sourceRectangle.Value.Bottom / texture.Size.Height);
//         }
//
//         switch (imageTransform)
//         {
//             case ImageTransform.None:
//                 break;
//             
//             case ImageTransform.Rotate90:
//                 (t01, t11, t10, t00) = (t00, t01, t11, t10);
//                 break;
//             
//             case ImageTransform.Rotate180:
//                 (t11, t10, t00, t01) = (t00, t01, t11, t10);
//                 break;
//             
//             case ImageTransform.Rotate270:
//                 (t10, t00, t01, t11) = (t00, t01, t11, t10);
//                 break;
//             
//             case ImageTransform.FlipHorizontal:
//                 (t00, t01, t11, t10) = (t10, t11, t01, t00);
//                 break;
//             
//             case ImageTransform.FlipVertical:
//                 (t00, t01, t11, t10) = (t01, t00, t10, t11);
//                 break;
//         }
//         
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X, targetRectangle.Y, depth), col, t00));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X, targetRectangle.Y + targetRectangle.Height, depth), col, t01));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X + targetRectangle.Width, targetRectangle.Y + targetRectangle.Height, depth), col, t11));
//         
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X, targetRectangle.Y, depth), col, t00));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X + targetRectangle.Width, targetRectangle.Y + targetRectangle.Height, depth), col, t11));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X + targetRectangle.Width, targetRectangle.Y, depth), col, t10));
//     }
//
//     public virtual void DrawTexture(ITexture texture, Vector2 position, Rectangle? sourceRectangle = null, Vector2? origin = null,
//         float rotation = 0, float scale = 1, RgbaColor? color = null,
//         ImageTransform imageTransform = ImageTransform.None,
//         TextureFilter textureFilter = TextureFilter.Nearest,
//         IEffect effect = null, float depth = 0, RenderMode mode = RenderMode.Normal)
//     {
//         if (CurrentBatchMode != BatchMode.TextureBuffer)
//         {
//             Flush();
//         }
//
//         if (TextureVertexBuffer.Offset + 6 > TextureVertexBuffer.Size)
//         {
//             Flush();
//         }
//         
//         PrepareRendering(texture, effect, VertexPositionColorTexture.Mode, textureFilter, mode);
//
//         CurrentBatchMode = BatchMode.TextureBuffer;
//
//         var col = color ?? RgbaColor.White;
//
//         var t00 = new Vector2(0, 0);
//         var t10 = new Vector2(1, 0);
//         var t11 = new Vector2(1, 1);
//         var t01 = new Vector2(0, 1);
//
//         if (sourceRectangle.HasValue)
//         {
//             t00 = new Vector2((float) sourceRectangle.Value.X / texture.Size.Width, (float) sourceRectangle.Value.Y / texture.Size.Height);
//             t10 = new Vector2((float) sourceRectangle.Value.Right / texture.Size.Width, (float) sourceRectangle.Value.Y / texture.Size.Height);
//             t11 = new Vector2((float) sourceRectangle.Value.Right / texture.Size.Width, (float) sourceRectangle.Value.Bottom / texture.Size.Height);
//             t01 = new Vector2((float) sourceRectangle.Value.X / texture.Size.Width, (float) sourceRectangle.Value.Bottom / texture.Size.Height);
//         }
//
//         if (origin.HasValue)
//         {
//             var offset = origin.Value;
//             offset *= scale;
//             position -= offset;
//         }
//         
//         var targetRectangle = new RectangleF(position.X, position.Y, (sourceRectangle?.Width ?? texture.Size.Width) * scale, (sourceRectangle?.Height ?? texture.Size.Height) * scale);
//         var matrix = RenderingHelpers.CreateTransform(targetRectangle, imageTransform);
//         
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X, targetRectangle.Y, depth).Prepare(ref matrix), col, t00));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X, targetRectangle.Y + targetRectangle.Height, depth).Prepare(ref matrix), col, t01));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X + targetRectangle.Width, targetRectangle.Y + targetRectangle.Height, depth).Prepare(ref matrix), col, t11));
//         
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X, targetRectangle.Y, depth).Prepare(ref matrix), col, t00));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X + targetRectangle.Width, targetRectangle.Y + targetRectangle.Height, depth).Prepare(ref matrix), col, t11));
//         TextureVertexBuffer.AddVertex(new VertexPositionColorTexture(new Vector3(targetRectangle.X + targetRectangle.Width, targetRectangle.Y, depth).Prepare(ref matrix), col, t10));
//     }
//
//     public virtual void FillRectangle(RectangleF rectangle, RgbaColor color, float depth = 0)
//     {
//         if (CurrentBatchMode != BatchMode.ColorBuffer)
//         {
//             Flush();
//         }
//
//         if (ColorVertexBuffer.Offset + 6 > ColorVertexBuffer.Size)
//         {
//             Flush();
//         }
//         
//         PrepareRendering(null, null, VertexPositionColor.Mode, TextureFilter.Nearest, RenderMode.Normal);
//         
//         CurrentBatchMode = BatchMode.ColorBuffer;
//         
//         ColorVertexBuffer.AddVertex(new VertexPositionColor(new Vector3(rectangle.X, rectangle.Y, depth), color));
//         ColorVertexBuffer.AddVertex(new VertexPositionColor(new Vector3(rectangle.X, rectangle.Y + rectangle.Height, depth), color));
//         ColorVertexBuffer.AddVertex(new VertexPositionColor(new Vector3(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height, depth), color));
//         
//         ColorVertexBuffer.AddVertex(new VertexPositionColor(new Vector3(rectangle.X, rectangle.Y, depth), color));
//         ColorVertexBuffer.AddVertex(new VertexPositionColor(new Vector3(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height, depth), color));
//         ColorVertexBuffer.AddVertex(new VertexPositionColor(new Vector3(rectangle.X + rectangle.Width, rectangle.Y, depth), color));
//     }
//
//     public abstract void DrawPrimitives(IVertexBuffer vertexBuffer, int vertexStart, int vertexCount,
//         ITexture texture = null, RgbaColor? color = null, 
//         TextureFilter textureFilter = TextureFilter.Nearest, Matrix4x4? transform = null, IEffect effect = null, RenderMode renderMode = RenderMode.Normal);
// }