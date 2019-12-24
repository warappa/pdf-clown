using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using Buffer = HarfBuzzSharp.Buffer;

namespace PdfClown.SkiaSharpUtils
{
    public class SKShaper2 : IDisposable
    {
        internal const int FONT_SIZE_SCALE = 512;

        private Buffer buffer;

        public SKShaper2(Font font, SharpFont.Face sfFace)
        {
            font.SetScale(FONT_SIZE_SCALE, FONT_SIZE_SCALE);
            Font = font;
            SfFace = sfFace;

            //font.SetFunctionsOpenType();

            buffer = new Buffer();
            buffer.Direction = Direction.LeftToRight;
            buffer.Script = Script.Latin;

        }

        public SKTypeface Typeface { get; private set; }
        public Font Font { get; }
        public SharpFont.Face SfFace { get; }

        public void Dispose()
        {
            //Font?.Dispose();
            buffer?.Dispose();
        }

        public Result Shape(Buffer buffer, SKPaint paint, double fontSize) =>
            Shape(buffer, 0, 0, paint, fontSize);

        public Result Shape(Buffer buffer, float xOffset, float yOffset, SKPaint paint, double fontSize)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (paint == null)
            {
                throw new ArgumentNullException(nameof(paint));
            }
            buffer.Direction = Direction.LeftToRight;
            //buffer.Script = Script.MaxValue;
            buffer.GuessSegmentProperties();
            // do the shaping
            //SfFace.SetCharSize(0, fontSize, 72, 72);
            Font.ShapeBetter(buffer);

            // get the shaping results
            var len = buffer.Length;
            var info = buffer.GlyphInfos;
            var pos = buffer.GlyphPositions;

            // get the sizes
            var textSizeY = paint.TextSize / FONT_SIZE_SCALE;
            var textSizeX = textSizeY * paint.TextScaleX;

            var points = new SKPoint[len];
            var clusters = new uint[len];
            var codepoints = new uint[len];

            for (var i = 0; i < len; i++)
            {
                codepoints[i] = info[i].Codepoint;

                clusters[i] = info[i].Cluster;

                points[i] = new SKPoint(
                    xOffset + pos[i].XOffset * textSizeX,
                    yOffset - pos[i].YOffset * textSizeY);

                // move the cursor
                xOffset += pos[i].XAdvance * textSizeX;
                yOffset += pos[i].YAdvance * textSizeY;
            }

            return new Result(codepoints, clusters, points);
        }

        public Result Shape(string text, SKPaint paint, int fontSize) =>
            Shape(text, 0, 0, paint, fontSize);

        public Result Shape(string text, float xOffset, float yOffset, SKPaint paint, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new Result();
            }

            using (var buffer = new Buffer())
            {
                switch (paint.TextEncoding)
                {
                    case SKTextEncoding.Utf8:
                        buffer.AddUtf8(text);
                        break;
                    case SKTextEncoding.Utf16:
                        buffer.AddUtf16(text);
                        break;
                    case SKTextEncoding.Utf32:
                        buffer.AddUtf32(text);
                        break;
                    default:
                        throw new NotSupportedException("TextEncoding of type GlyphId is not supported.");
                }

                buffer.Direction = Direction.LeftToRight;
                buffer.Script = Script.Latin;
                buffer.GuessSegmentProperties();

                return Shape(buffer, xOffset, yOffset, paint, fontSize);
            }
        }

        public class Result
        {
            public Result()
            {
                Codepoints = new uint[0];
                Clusters = new uint[0];
                Points = new SKPoint[0];
            }

            public Result(uint[] codepoints, uint[] clusters, SKPoint[] points)
            {
                Codepoints = codepoints;
                Clusters = clusters;
                Points = points;
            }

            public uint[] Codepoints { get; private set; }

            public uint[] Clusters { get; private set; }

            public SKPoint[] Points { get; private set; }
        }
    }
}
