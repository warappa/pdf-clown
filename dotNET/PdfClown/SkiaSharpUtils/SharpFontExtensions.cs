using HarfBuzzSharp;
using PdfClown.SkiaSharpUtils;
using SharpFont;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PdfClown.SkiaSharpUtils
{
    public static class SharpFontExtensions
    {
        public static Library Library;

        static SharpFontExtensions()
        {
            Library = new SharpFont.Library();
        }

        /// <summary>
        ///     Constructs a new <see cref="Font" /> with <paramref name="face" />.
        /// </summary>
        public static IntPtr GetHandle(this SharpFont.Face face)
        {
            var faceHandleProperty = typeof(SharpFont.Face).GetProperty(@"Reference",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (faceHandleProperty == null)
                throw new InvalidOperationException(
                    @"Failed to get SharpFont.Face.Reference; might due to breaking internal changes in SharpFont.");
            return (IntPtr)faceHandleProperty.GetValue(face);
        }

        public static IntPtr GetHandle(this HarfBuzzSharp.Face face)
        {
            var faceHandleProperty = typeof(HarfBuzzSharp.Font).GetProperty(@"Handle",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (faceHandleProperty == null)
                throw new InvalidOperationException(
                    @"Failed to get SharpFont.Face.Reference; might due to breaking internal changes in SharpFont.");
            return (IntPtr)faceHandleProperty.GetValue(face);
        }

        public static void SetHandle(this HarfBuzzSharp.Font font, IntPtr handle)
        {
            var faceHandleProperty = typeof(HarfBuzzSharp.Font).GetProperty(@"Handle",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (faceHandleProperty == null)
                throw new InvalidOperationException(
                    @"Failed to get SharpFont.Face.Reference; might due to breaking internal changes in SharpFont.");
            faceHandleProperty.SetValue(font, handle);
        }

        public static IntPtr GetHandle(this HarfBuzzSharp.Font font)
        {
            var faceHandleProperty = typeof(HarfBuzzSharp.Font).GetProperty(@"Handle",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (faceHandleProperty == null)
                throw new InvalidOperationException(
                    @"Failed to get SharpFont.Face.Reference; might due to breaking internal changes in SharpFont.");
            return (IntPtr)faceHandleProperty.GetValue(font);
        }


        public static IntPtr GetHandle(this object face)
        {
            var faceHandleProperty = face.GetType().GetProperty(@"Reference",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (faceHandleProperty == null)
                throw new InvalidOperationException(
                    @"Failed to get SharpFont.Face.Reference; might due to breaking internal changes in SharpFont.");
            return (IntPtr)faceHandleProperty.GetValue(face);
        }
        //TODO get proper delegate type for "destroy" parameters
        [DllImport("libHarfBuzzSharp", CallingConvention = CallingConvention.Cdecl)]
        //[DllImport("libharfbuzz-0", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr hb_ft_face_create(IntPtr ft_face, IntPtr destroy);

        [DllImport("libHarfBuzzSharp", CallingConvention = CallingConvention.Cdecl)]
        //[DllImport("libharfbuzz-0", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr hb_ft_font_create(IntPtr ft_face, IntPtr destroy);

        [DllImport("libHarfBuzzSharp", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void hb_shape(IntPtr font, IntPtr buffer, IntPtr features, int num_features);

        public static void ShapeBetter(this HarfBuzzSharp.Font font, HarfBuzzSharp.Buffer buffer)
        {
            hb_shape(font.Handle, buffer.Handle, IntPtr.Zero, 0);
        }

        public static HarfBuzzSharp.Face ToHarfBuzzFace(this SharpFont.Face sharpFace)
        {
            var freeTypeFaceHandle = sharpFace.GetHandle();

            var hbFaceHandle = hb_ft_face_create(freeTypeFaceHandle, IntPtr.Zero);

            var constructor = typeof(HarfBuzzSharp.Face).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(IntPtr) }, null);
            var hbFace = (HarfBuzzSharp.Face)constructor.Invoke(new object[] { hbFaceHandle });

            //var tf = SKFontManager.Default.MatchCharacter('w');
            //var blob = tf.OpenStream(out var index).ToHarfBuzzBlob();
            //var hbFace = new HarfBuzzSharp.Face(blob, index);
            return hbFace;
        }
        public static IntPtr ToHarfBuzzFontHandle(this SharpFont.Face face)
        {
            var handle = face.GetHandle();

            return hb_ft_font_create(handle, IntPtr.Zero);
        }

        public static HarfBuzzSharp.Font ToHarfBuzzFont2(this SharpFont.Face sharpFace)
        {
            var freeTypeFaceHandle = sharpFace.GetHandle();

            var hbFontHandle = hb_ft_font_create(freeTypeFaceHandle, IntPtr.Zero);

            var hbFont = (HarfBuzzSharp.Font)FormatterServices.GetUninitializedObject(typeof(HarfBuzzSharp.Font));
            hbFont.SetHandle(hbFontHandle);

            var fields = typeof(HarfBuzzSharp.Font).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var backing = fields.Where(x => x.FieldType == typeof(OpenTypeMetrics)).FirstOrDefault();
            backing.SetValue(hbFont, new OpenTypeMetrics(hbFontHandle));

            return hbFont;
        }

        public static HarfBuzzSharp.Font ToHarfBuzzFont(this SharpFont.Face face)
        {
            //var hbFace = new HarfBuzzSharp.Face((f, t) => GetTable(face, t));
            //var hbFont = new HarfBuzzSharp.Font(hbFace);
            var hbFace = face.ToHarfBuzzFace();
            //hbFace.MakeImmutable();
            var hbFont = new HarfBuzzSharp.Font(hbFace);
            ////hbFont.SetScale(10, 10);

            var scale = hbFont.GetScale();
            hbFont.TryGetGlyphFromString("w", out uint glyph);
            //var fe = hbFont.GetFontExtentsForDirection(Direction.LeftToRight);
            //var fontHandle = face.ToHarfBuzzFontHandle();

            //var constructor = typeof(HarfBuzzSharp.Font).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
            //    null, new[] { typeof(IntPtr) }, null);
            //var hbFont = (HarfBuzzSharp.Font)constructor.Invoke(new object[] { fontHandle });

            //var hbFont = new FTFont(face, face.ToHarfBuzzFace());


            //var tf = SKFontManager.Default.MatchCharacter('w');
            //var blob = tf.OpenStream(out var index).ToHarfBuzzBlob();
            //var hbFace = new HarfBuzzSharp.Face(blob, index);
            return hbFont;
        }
        public enum SupportedTags : uint
        {
            Maxp = 1835104368,
            Head = 1751474532,
            Post = 1886352244,
            Gsub = 1196643650,
            OS2 = 1330851634,
            GPos = 1196445523,
            Morx = 1836020344,
            Mort = 1836020340,
            GDef = 1195656518,
            Kerx = 1801810552,
            Kern = 1801810542,
            Trak = 1953653099,
            CMap = 1668112752,
            HHea = 1751672161,
            VHea = 1985364306,
            Hmtx = 1752003704,
            HVar = 1213612370,
        }
        private static Blob GetTable(SharpFont.Face face, Tag tag)
        {
            uint length = 0;

            try
            {
                //var res = face.HasGlyphNames;
                //res = face.HasPSGlyphNames();
                //var na = face.GetPostscriptName();
                ////var c = face.GetSfntNameCount();
                ////face.GetPSFontPrivate();
                ////face.GetWinFntHeader();
                ////face.GetPSFontInfo();
                //var a = face.SfntTableInfo(0, 0);
                //var header = face.GetSfntTable(SharpFont.TrueType.SfntTag.VertHeader);
                //face.LoadSfntTable(0, 0, IntPtr.Zero, ref length);

                switch ((uint)tag)
                {
                    case (uint)SupportedTags.Maxp:
                        {
                            var sfData = face.GetSfntTable(SharpFont.TrueType.SfntTag.MaxProfile);
                            if (sfData is null)
                            {
                                return null;
                            }
                            var handle = sfData.GetHandle();
                            return new Blob((IntPtr)handle, 1024, MemoryMode.Duplicate);
                        }

                    case (uint)SupportedTags.Head:
                        {
                            var sfData = face.GetSfntTable(SharpFont.TrueType.SfntTag.Header);
                            if (sfData is null)
                            {
                                return null;
                            }
                            var handle = sfData.GetHandle();
                            return new Blob((IntPtr)handle, 1024, MemoryMode.Duplicate);
                        }
                    case (uint)SupportedTags.HHea:
                        {
                            var sfData = face.GetSfntTable(SharpFont.TrueType.SfntTag.HorizontalHeader);
                            if (sfData is null)
                            {
                                return null;
                            }
                            var handle = sfData.GetHandle();
                            return new Blob((IntPtr)handle, 1024, MemoryMode.Duplicate);
                        }
                    case (uint)SupportedTags.OS2:
                        {
                            var sfData = face.GetSfntTable(SharpFont.TrueType.SfntTag.OS2);
                            if (sfData is null)
                            {
                                return null;
                            }
                            var handle = sfData.GetHandle();
                            return new Blob((IntPtr)handle, 1024, MemoryMode.Duplicate);
                        }
                    case (uint)SupportedTags.Post:
                        {
                            var sfData = face.GetSfntTable(SharpFont.TrueType.SfntTag.Postscript);
                            if (sfData is null)
                            {
                                return null;
                            }
                            var handle = sfData.GetHandle();
                            return new Blob((IntPtr)handle, 1024, MemoryMode.Duplicate);
                        }
                    case (uint)SupportedTags.VHea:
                        {
                            var sfData = face.GetSfntTable(SharpFont.TrueType.SfntTag.VertHeader);
                            if (sfData is null)
                            {
                                return null;
                            }
                            var handle = sfData.GetHandle();
                            return new Blob((IntPtr)handle, 1024, MemoryMode.Duplicate);
                        }
                    default:
                        return null;
                }
            }
            catch
            {
                Debug.WriteLine("" +
                    (char)(((uint)tag >> 24) & 0xff) +
                    (char)(((uint)tag >> 16) & 0xff) +
                    (char)(((uint)tag >> 8) & 0xff) +
                    (char)(((uint)tag) & 0xff) + $" ({(uint)tag}) ({(((uint)tag >> 24) & 0xff)} {(((uint)tag >> 16) & 0xff)} {(((uint)tag >> 8) & 0xff)} {(((uint)tag) & 0xff)})");

            }
            //if (length == 0)
            //{
            //    throw new Exception("Not table!");
            //}

            var buffer = new byte[length];
            var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var bufferPtr = pinnedBuffer.AddrOfPinnedObject();

            return new Blob(bufferPtr, (int)length, MemoryMode.ReadOnly, () => pinnedBuffer.Free());
        }
    }


    public class FTFont : HarfBuzzSharp.Font
    {
        public FTFont(SharpFont.Face face, HarfBuzzSharp.Face dummy)
            : base(HarfBuzzSharp.Face.Empty)
        {
            var fontHandle = face.ToHarfBuzzFontHandle();
            this.Handle = fontHandle;
            //OpenTypeMetrics = new OpenTypeMetrics(Handle);
            var fields = typeof(HarfBuzzSharp.Font).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var backing = fields.Where(x => x.FieldType == typeof(OpenTypeMetrics)).FirstOrDefault();
            backing.SetValue(this, new OpenTypeMetrics(Handle));
            fields.Where(x => x.Name == "zero").First().SetValue(this, false);
        }
    }

    public static class CanvasExtensions
    {
        public static void DrawShapedText2(this SKCanvas canvas, SKShaper2 shaper, string text, float x, float y, SKPaint paint, double fontSize)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));
            if (shaper == null)
                throw new ArgumentNullException(nameof(shaper));
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));

            if (string.IsNullOrEmpty(text))
                return;

            // shape the text
            var result = shaper.Shape(text, x, y, paint, fontSize);
            canvas.Save();
            canvas.Scale(1 / canvas.TotalMatrix.ScaleX, 1 / canvas.TotalMatrix.ScaleY);
            // draw the text
            using (var paintClone = paint.Clone())
            {
                paintClone.TextEncoding = SKTextEncoding.GlyphId;
                paintClone.Typeface = shaper.Typeface;
                paintClone.BlendMode = SKBlendMode.SrcATop;

                var face = shaper.SfFace;
                var codepoints = result.Codepoints;
                //int penX = 0, penY = face.Size.Metrics.NominalHeight;
                for (int i = 0; i < codepoints.Length; ++i)
                {
                    face.LoadGlyph(codepoints[i], LoadFlags.Default, LoadTarget.Normal);
                    face.Glyph.RenderGlyph(RenderMode.Normal);
                    if (face.Glyph.Bitmap.Width == 0)
                    {
                        continue;
                    }

                    using (var cBmp = face.Glyph.Bitmap.ToSkBitmap(paintClone.Color))
                    {
                        var drawX = result.Points[i].X + face.Glyph.BitmapLeft; // penX + (result.Points[i].X >> 6) + face.Glyph.BitmapLeft;
                        var drawY = result.Points[i].Y - +face.Glyph.BitmapTop;// penY - (glyphPositions[i].yOffset >> 6) - face.Glyph.BitmapTop;

                        canvas.DrawBitmap(cBmp, drawX, drawY, paintClone);
                        
                        //penX += glyphPositions[i].xAdvance >> 6;
                        //penY -= glyphPositions[i].yAdvance >> 6;
                    }
                }
            }
            canvas.Restore();
        }

        public static SKBitmap ToSkBitmap(this FTBitmap b, SKColor skColor)
        {
            if (b.IsDisposed)
                throw new ObjectDisposedException("FTBitmap", "Cannot access a disposed object.");

            if (b.Width == 0 || b.Rows == 0)
                throw new InvalidOperationException("Invalid image size - one or both dimensions are 0.");

            //TODO deal with negative pitch
            switch (b.PixelMode)
            {
                case PixelMode.Mono:
                    {
                        var bmp = new SKBitmap(new SKImageInfo
                        {
                            Width = b.Width,
                            Height = b.Rows,
                            ColorType = SKColorType.Rgba8888,
                            AlphaType = SKAlphaType.Premul
                        });

                        var bytes = (int)Math.Ceiling((decimal)b.Rows * b.Width);
                        var x = 0;
                        var y = 0;
                        for (int i = 0; i < bytes; i++)
                        {
                            var colorsOf8Pixels = b.BufferData[i];
                            for (var k = 7; k >= 0; k--)
                            {
                                var color = (byte)(((colorsOf8Pixels >> k) & 1) == 1 ? 255 : 0);
                                //bmp.SetPixel(x++, y, new SKColor(color, color, color, color));
                                bmp.SetPixel(x++, y, skColor.WithAlpha(color));
                                if (x == b.Width)
                                {
                                    x = 0;
                                    y++;
                                }
                            }
                        }

                        return bmp;
                    }

                case PixelMode.Gray4:
                    {
                        var bmp = new SKBitmap(new SKImageInfo
                        {
                            Width = b.Width,
                            Height = b.Rows,
                            ColorType = SKColorType.Rgba8888,
                            AlphaType = SKAlphaType.Premul
                        });

                        var bytes = (int)Math.Ceiling((decimal)b.Rows * b.Width);
                        var x = 0;
                        var y = 0;
                        for (int i = 0; i < bytes; i++)
                        {
                            var colorsOf2Pixels = b.BufferData[i];
                            for (var k = 1; k >= 0; k--)
                            {
                                var color = (byte)(((colorsOf2Pixels >> (k * 4)) & 127) * 17);
                                bmp.SetPixel(x++, y, skColor.WithAlpha(color));
                                if (x == b.Width)
                                {
                                    x = 0;
                                    y++;
                                }
                            }
                        }

                        return bmp;
                    }

                case PixelMode.Gray:
                    {
                        var bmp = new SKBitmap(new SKImageInfo
                        {
                            Width = b.Width,
                            Height = b.Rows,
                            ColorType = SKColorType.Rgba8888,
                            AlphaType = SKAlphaType.Premul
                        });

                        for (int i = 0; i < b.Rows; i++)
                        {
                            for (var j = 0; j < b.Width; j++)
                            {
                                var color = b.BufferData[i * b.Width + j];
                                bmp.SetPixel(j, i, skColor.WithAlpha(color));
                            }
                        }

                        return bmp;
                    }

                case PixelMode.Lcd:
                    {
                        var bmp = new SKBitmap(new SKImageInfo
                        {
                            Width = b.Width / 3,
                            Height = b.Rows,
                            ColorType = SKColorType.Rgba8888,
                            AlphaType = SKAlphaType.Premul
                        });

                        for (int i = 0; i < b.Rows; i++)
                        {
                            for (var j = 0; j < b.Width; j++)
                            {
                                var red = b.BufferData[i * b.Width + j];
                                var green = b.BufferData[i * b.Width + j + 1];
                                var blue = b.BufferData[i * b.Width + j + 2];
                                bmp.SetPixel(j, i, skColor.WithAlpha(red));
                            }
                        }

                        return bmp;
                    }
                /*case PixelMode.VerticalLcd:
				{
					int bmpHeight = b.Rows / 3;
					Bitmap bmp = new Bitmap(b.Width, bmpHeight, PixelFormat.Format24bppRgb);
					var locked = bmp.LockBits(new Rectangle(0, 0, b.Width, bmpHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
					for (int i = 0; i < bmpHeight; i++)
						PInvokeHelper.Copy(Buffer, i * b.Pitch, locked.Scan0, i * locked.Stride, b.Width);
					bmp.UnlockBits(locked);

					return bmp;
				}*/

                default:
                    throw new InvalidOperationException("System.Drawing.Bitmap does not support this pixel mode.");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PostscriptRec
    {
        internal IntPtr FormatType;
        internal IntPtr italicAngle;
        internal short underlinePosition;
        internal short underlineThickness;
        internal UIntPtr isFixedPitch;
        internal UIntPtr minMemType42;
        internal UIntPtr maxMemType42;
        internal UIntPtr minMemType1;
        internal UIntPtr maxMemType1;
    }
}
