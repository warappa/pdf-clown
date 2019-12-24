using SharpFont;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Linq;

namespace PdfClown.SkiaSharpUtils
{
    public static class CanvasExtensions
    {
        public static void DrawTextWithFreeTypeFace(this SKCanvas canvas, SharpFont.Face face, string text, float x, float y, SKPaint paint)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            if (face is null)
            {
                throw new ArgumentNullException(nameof(face));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (paint == null)
            {
                throw new ArgumentNullException(nameof(paint));
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // shape the text
            // draw the text
            using (var paintClone = paint.Clone())
            {
                paintClone.TextEncoding = SKTextEncoding.GlyphId;
                paintClone.BlendMode = SKBlendMode.SrcATop;
                
                var codepoints = text
                    .Select(c => face.GetCharIndex(c))
                    .ToArray();
                for (var i = 0; i < codepoints.Length; ++i)
                {
                    face.LoadGlyph(codepoints[i], LoadFlags.NoBitmap, LoadTarget.Normal);
                    face.Glyph.RenderGlyph(RenderMode.Normal);

                    var skPath = GetPathForGlyph(face);
                    canvas.DrawPath(skPath, paintClone);
                }
            }
        }

        private static SKPath GetPathForGlyph(SharpFont.Face face)
        {
            var skPath = new SKPath();
            var factor = 1;
            var outlineFuncs = new OutlineFuncs(
                new MoveToFunc((ref FTVector to, IntPtr user) =>
                {
                    skPath.MoveTo(new SKPoint((float)to.X * factor, -(float)to.Y * factor));
                    return 0; // 0 is success
                }),
                new LineToFunc((ref FTVector to, IntPtr user) =>
                {
                    skPath.LineTo(new SKPoint((float)to.X * factor, -(float)to.Y * factor));
                    return 0;
                }),
                new ConicToFunc((ref FTVector control, ref FTVector to, IntPtr user) =>
                {

                    skPath.ConicTo(new SKPoint((float)control.X * factor, -(float)control.Y * factor), new SKPoint((float)to.X * factor, -(float)to.Y * factor), 1f);
                    return 0;
                }),
                new CubicToFunc((ref FTVector control1, ref FTVector control2, ref FTVector to, IntPtr user) =>
                {
                    skPath.CubicTo(
                        new SKPoint((float)control1.X * factor, -(float)control1.Y * factor),
                        new SKPoint((float)control2.X * factor, -(float)control2.Y * factor),
                        new SKPoint((float)to.X * factor, -(float)to.Y * factor));
                    return 0;
                }),
                0, 0
            );

            face.Glyph.Outline.Decompose(outlineFuncs, IntPtr.Zero);

            var svg = skPath.ToSvgPathData();
            return skPath;
        }
    }
}
