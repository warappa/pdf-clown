﻿
using BitMiracle.LibTiff.Classic;
using FreeImageAPI;
using PdfClown.Documents.Contents.ColorSpaces;
using PdfClown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PdfClown.Documents.Contents
{
    public class ImageLoader
    {
        public static SKBitmap Load(IImageObject imageObject, ContentScanner.GraphicsState state)
        {
            var buffer = imageObject.Data;
            var data = buffer.ToByteArray();
            var image = SKBitmap.Decode(data);
            if (image == null)
            {
                var filter = imageObject.Filter;
                if (filter is PdfArray filterArray)
                {
                    var parameterArray = imageObject.Parameters as PdfArray;
                    for (int i = 0; i < filterArray.Count; i++)
                    {
                        var filterItem = filterArray[i];
                        var parameterItem = parameterArray?[i];
                        Bytes.Buffer.Decode(buffer, filterItem, parameterItem ?? imageObject.Header);
                        data = buffer.ToByteArray();
                        image = SKBitmap.Decode(data);
                        if (image != null)
                            break;
                    }
                }
                else if (filter != null)
                {
                    Bytes.Buffer.Decode(buffer, filter, imageObject.Parameters ?? imageObject.Header);
                    data = buffer.ToByteArray();
                    image = SKBitmap.Decode(data);
                }

            }
            if (image == null)
            {
                if (IsTiff(data))
                {
                    image = LoadTiff(data);
                }
                else if (IsJPEG(data))
                {
                    image = LoadJPEG(data);
                }
                else
                {
                    var imageLoader = new ImageLoader(imageObject, data, state);
                    image = imageLoader.Load();
                }
            }
            return image;
        }

        private ContentScanner.GraphicsState state;
        private IImageObject image;
        private ColorSpace colorSpace;
        private ICCBasedColorSpace iccColorSpace;
        private int bitPerComponent;
        private PdfArray matte;
        private SKSize size;
        private bool indexed;
        private int componentsCount;
        private double maximum;
        private double min;
        private double max;
        private byte[] buffer;
        private bool imageMask;
        private PdfArray decode;
        private IImageObject sMask;
        private ImageLoader sMaskLoader;

        public ImageLoader(IImageObject image, ContentScanner.GraphicsState state)
        {
            var buffer = image.Data;
            var data = buffer.ToByteArray();
            var filter = image.Filter;
            if (filter != null)
            {
                Bytes.Buffer.Decode(buffer, filter, image.Parameters ?? image.Header);
                data = buffer.ToByteArray();
            }
            Init(image, data, state);
        }

        public ImageLoader(IImageObject image, byte[] buffer, ContentScanner.GraphicsState state)
        {
            Init(image, buffer, state);
        }

        private void Init(IImageObject image, byte[] buffer, ContentScanner.GraphicsState state)
        {
            this.state = state;
            this.image = image;
            this.buffer = buffer;
            imageMask = image.ImageMask;
            decode = image.Decode;
            matte = image.Matte;
            size = image.Size;
            bitPerComponent = image.BitsPerComponent;

            maximum = Math.Pow(2, bitPerComponent) - 1;
            min = 0D;
            max = indexed ? maximum : 1D;

            sMask = image.SMask;
            if (sMask != null)
            {
                sMaskLoader = new ImageLoader(sMask, state);
            }
            if (imageMask)
            {
                return;
            }
            colorSpace = image.ColorSpace;
            componentsCount = colorSpace.ComponentCount;
            iccColorSpace = colorSpace as ICCBasedColorSpace;
            if (colorSpace is IndexedColorSpace indexedColorSpace)
            {
                indexed = true;
                iccColorSpace = indexedColorSpace.BaseSpace as ICCBasedColorSpace;
            }
        }

        public Color GetColor(int x, int y)
        {
            return GetColor((y * (int)size.Width + x));
        }

        public Color GetColor(int index)
        {
            var componentIndex = index * componentsCount;
            var components = new List<PdfDirectObject>();
            for (int i = 0; i < componentsCount; i++)
            {
                var value = componentIndex < buffer.Length ? buffer[componentIndex] : 0;
                var interpolate = min + (value * ((max - min) / maximum));//indexed ? value : 
                components.Add(indexed
                    ? (PdfDirectObject)new PdfInteger((int)interpolate)
                    : (PdfDirectObject)new PdfReal(interpolate));
                componentIndex++;
            }
            return colorSpace.GetColor(components, null);
        }

        public SKBitmap Load()
        {
            if (imageMask)
            {
                return LoadImageMask();
            }
            var info = new SKImageInfo((int)size.Width, (int)size.Height)
            {
                AlphaType = SKAlphaType.Premul,
            };
            if (iccColorSpace != null)
            {
                info.ColorSpace = iccColorSpace.GetSKColorSpace();
            }

            // create the buffer that will hold the pixels
            var raster = new int[info.Width * info.Height];//var bitmap = new SKBitmap();

            for (int y = 0; y < info.Height; y++)
            {
                for (int x = 0; x < info.Width; x++)
                {
                    var index = (y * info.Width + x);
                    var color = GetColor(index);

                    var skColor = colorSpace.GetColor(color, null);
                    if (sMaskLoader != null)
                    {
                        var sMaskColor = sMaskLoader.GetColor(index);
                        //alfa
                        skColor = skColor.WithAlpha((byte)(((IPdfNumber)sMaskColor.Components[0]).DoubleValue * 255));
                        //shaping
                        //for (int i = 0; i < color.Components.Count; i++)
                        //{
                        //    var m = sMaskLoader.matte == null ? 0D : ((IPdfNumber)sMaskLoader.matte[i]).DoubleValue;
                        //    var a = ((IPdfNumber)sMaskColor.Components[sMaskColor.Components.Count == color.Components.Count ? i : 0]).DoubleValue;
                        //    var c = ((IPdfNumber)color.Components[i]).DoubleValue;
                        //    color.Components[i] = new PdfReal(m + a * (c - m));
                        //}
                    }
                    raster[index] = (int)(uint)skColor;//bitmap.SetPixel(x, y, skColor);
                }
            }

            // get a pointer to the buffer, and give it to the bitmap
            var ptr = GCHandle.Alloc(raster, GCHandleType.Pinned);
            var bitmap = new SKBitmap();
            bitmap.InstallPixels(info, ptr.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => ptr.Free(), null);

            return bitmap;
        }

        private SKBitmap LoadImageMask()
        {
            var info = new SKImageInfo((int)size.Width, (int)size.Height)
            {
                AlphaType = SKAlphaType.Premul,
            };
            var skColor = state.FillColorSpace.GetColor(state.FillColor);
            var raster = new int[info.Width * info.Height];

            for (int y = 0; y < info.Height; y++)
            {
                for (int x = 0; x < info.Width; x++)
                {
                    var index = (y * info.Width + x);
                    var value = index < buffer.Length ? buffer[index] : (byte)0;
                    raster[index] = (int)(uint)skColor.WithAlpha(value);
                }
            }

            // get a pointer to the buffer, and give it to the bitmap
            var ptr = GCHandle.Alloc(raster, GCHandleType.Pinned);
            var bitmap = new SKBitmap();
            bitmap.InstallPixels(info, ptr.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => ptr.Free(), null);

            return bitmap;
        }

        public static SKBitmap LoadJPEG(byte[] jpegStream)
        {
            using (var output = new MemoryStream())
            using (var input = new MemoryStream(jpegStream))
            {
                var bmp = FreeImage.LoadFromStream(input);

                FreeImage.SaveToStream(bmp, output, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_OPTIMIZE);
                FreeImage.Unload(bmp);

                return SKBitmap.Decode(output.ToArray());
            }
        }

        //https://stackoverflow.com/a/50370515/4682355
        public static SKBitmap LoadTiff(byte[] tiffStream)
        {
            // open a TIFF stored in the stream
            using (var memeoryStream = new MemoryStream(tiffStream))
            using (var tifImg = Tiff.ClientOpen("in-memory", "r", memeoryStream, new TiffStream()))
            {
                // read the dimensions
                var width = tifImg.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                var height = tifImg.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                // create the bitmap
                var info = new SKImageInfo(width, height)
                {
                    ColorType = SKColorType.Rgba8888
                };

                // create the buffer that will hold the pixels
                var raster = new int[width * height];
                // read the image into the memory buffer
                if (!tifImg.ReadRGBAImageOriented(width, height, raster, Orientation.TOPLEFT))
                {
                    // not a valid TIF image.
                    return null;
                }

                // get a pointer to the buffer, and give it to the bitmap
                var ptr = GCHandle.Alloc(raster, GCHandleType.Pinned);

                var bitmap = new SKBitmap();
                bitmap.InstallPixels(info, ptr.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => ptr.Free(), null);

                return bitmap;
            }
        }

        public static bool IsImage(byte[] buf)
        {
            if (buf != null)
            {
                if (IsBitmap(buf)
                    || IsGif(buf)
                    || IsTiff(buf)
                    || IsPng(buf)
                    || IsJPEG(buf))
                    return true;
            }
            return false;
        }

        private static bool IsBitmap(byte[] buf)
        {
            return Encoding.ASCII.GetString(buf, 0, 2) == "BM";
        }

        private static bool IsPng(byte[] buf)
        {
            return (buf[0] == 137 && buf[1] == 80 && buf[2] == 78 && buf[3] == (byte)71); //png
        }

        private static bool IsJPEG(byte[] buf)
        {
            return (buf[0] == 255 && buf[1] == 216 && buf[2] == 255 && buf[3] == 224) //jpeg
                    || (buf[0] == 255 && buf[1] == 216 && buf[2] == 255 && buf[3] == 225); //jpeg canon
        }

        private static bool IsGif(byte[] buf)
        {
            return Encoding.ASCII.GetString(buf, 0, 3) == "GIF";
        }

        private static bool IsTiff(byte[] buf)
        {
            return (buf[0] == 73 && buf[1] == 73 && buf[2] == 42) // TIFF
                || (buf[0] == 77 && buf[1] == 77 && buf[2] == 42); // TIFF2;
        }
    }
}