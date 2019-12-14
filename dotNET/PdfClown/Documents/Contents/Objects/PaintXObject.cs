/*
  Copyright 2007-2011 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfClown.Bytes;
using xObjects = PdfClown.Documents.Contents.XObjects;
using PdfClown.Objects;

using System.Collections.Generic;
using SkiaSharp;
using System;
using PdfClown.Documents.Contents.XObjects;
using System.Linq;
using System.Runtime.InteropServices;

namespace PdfClown.Documents.Contents.Objects
{
    /**
      <summary>'Paint the specified XObject' operation [PDF:1.6:4.7].</summary>
    */
    [PDF(VersionEnum.PDF10)]
    public sealed class PaintXObject : Operation, IResourceReference<xObjects::XObject>
    {
        #region static
        #region fields
        public static readonly string OperatorKeyword = "Do";
        public static readonly SKPaint ImagePaint = new SKPaint { FilterQuality = SKFilterQuality.High };
        #endregion
        #endregion

        #region dynamic
        #region constructors
        public PaintXObject(PdfName name) : base(OperatorKeyword, name)
        { }

        public PaintXObject(IList<PdfDirectObject> operands) : base(OperatorKeyword, operands)
        { }
        #endregion
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets the scanner for the contents of the painted external object.</summary>
          <param name="context">Scanning context.</param>
        */
        public ContentScanner GetScanner(ContentScanner context)
        {
            xObjects::XObject xObject = GetXObject(context.ContentContext);
            return xObject is xObjects::FormXObject
              ? new ContentScanner((xObjects::FormXObject)xObject, context)
              : null;
        }

        /**
          <summary>Gets the <see cref="xObjects::XObject">external object</see> resource to be painted.
          </summary>
          <param name="context">Content context.</param>
        */
        public xObjects::XObject GetXObject(IContentContext context)
        { return GetResource(context); }

        #region IResourceReference
        public xObjects::XObject GetResource(IContentContext context)
        { return context.Resources.XObjects[Name]; }

        public PdfName Name
        {
            get => (PdfName)operands[0];
            set => operands[0] = value;
        }
        public static void DumpImage(SKPicture picture)
        {
            using (var stream = new SKFileWStream($"dump_{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}.png"))
            {
                using (var b = new SKBitmap((int)Math.Ceiling(picture.CullRect.Width), (int)Math.Ceiling(picture.CullRect.Height)))
                using (var canvas = new SKCanvas(b))
                {
                    canvas.DrawPicture(picture);

                    SKPixmap.Encode(stream, b, SKEncodedImageFormat.Png, 100);
                }
            };
        }
        public static void DumpImage(SKBitmap b)
        {
            using (var stream = new SKFileWStream($"dump_{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}.png"))
            {
                SKPixmap.Encode(stream, b, SKEncodedImageFormat.Png, 100);
            }
        }
        public override void Scan(GraphicsState state)
        {
            var scanner = state.Scanner;
            var canvas = scanner.RenderContext;
            if (canvas == null)
                return;
            try
            {
                canvas.Save();
                var xObject = GetXObject(scanner.ContentContext);
                if (xObject is xObjects.ImageXObject imageObject)
                {
                    var image = imageObject.LoadImage(state);
                    if (image != null)
                    {
                        var size = imageObject.Size;
                        var imageMatrix = imageObject.Matrix;
                        imageMatrix.ScaleY *= -1;
                        SKMatrix.PreConcat(ref imageMatrix, SKMatrix.MakeTranslation(0, -size.Height));
                        canvas.Concat(ref imageMatrix);

                        if (state.BlendMode.Any())
                        {
                            switch (state.BlendMode[0])
                            {
                                case BlendModeEnum.Multiply:
                                    ImagePaint.BlendMode = SKBlendMode.Multiply;
                                    break;
                                case BlendModeEnum.Lighten:
                                    ImagePaint.BlendMode = SKBlendMode.Lighten;
                                    break;
                                case BlendModeEnum.Luminosity:
                                    ImagePaint.BlendMode = SKBlendMode.Luminosity;
                                    break;
                                case BlendModeEnum.Overlay:
                                    ImagePaint.BlendMode = SKBlendMode.Overlay;
                                    break;
                                case BlendModeEnum.Normal:
                                    ImagePaint.BlendMode = SKBlendMode.SrcOver;
                                    break;
                                case BlendModeEnum.ColorBurn:
                                    ImagePaint.BlendMode = SKBlendMode.ColorBurn;
                                    break;
                                case BlendModeEnum.Screen:
                                    ImagePaint.BlendMode = SKBlendMode.Screen;
                                    break;
                                case BlendModeEnum.Darken:
                                    ImagePaint.BlendMode = SKBlendMode.Darken;
                                    break;
                                case BlendModeEnum.ColorDodge:
                                    ImagePaint.BlendMode = SKBlendMode.ColorDodge;
                                    break;
                                case BlendModeEnum.Compatible:
                                    ImagePaint.BlendMode = SKBlendMode.SrcOver;
                                    break;
                                case BlendModeEnum.HardLight:
                                    ImagePaint.BlendMode = SKBlendMode.HardLight;
                                    break;
                                case BlendModeEnum.SoftLight:
                                    ImagePaint.BlendMode = SKBlendMode.SoftLight;
                                    break;
                                case BlendModeEnum.Difference:
                                    ImagePaint.BlendMode = SKBlendMode.Difference;
                                    break;
                                case BlendModeEnum.Exclusion:
                                    ImagePaint.BlendMode = SKBlendMode.Exclusion;
                                    break;
                                case BlendModeEnum.Hue:
                                    ImagePaint.BlendMode = SKBlendMode.Hue;
                                    break;
                                case BlendModeEnum.Saturation:
                                    ImagePaint.BlendMode = SKBlendMode.Saturation;
                                    break;
                                case BlendModeEnum.Color:
                                    ImagePaint.BlendMode = SKBlendMode.Color;
                                    break;
                            }
                        }

                        canvas.DrawBitmap(image, 0, 0, ImagePaint);
                        //using (var surf = SKSurface.Create(canvas.GRContext, true, new SKImageInfo(image.Width, image.Height)))
                        //{
                        //    surf.Canvas.DrawBitmap(original, 0, 0);
                        //    surf.Canvas.Flush();
                        //    intermediates[i] = surf.Snapshot();
                        //}
                    }
                }
                else if (xObject is xObjects.FormXObject formObject)
                {
                    var translate = SKMatrix.MakeTranslation(formObject.Box.Left, formObject.Box.Top);
                    canvas.Concat(ref translate);
                    var formMatrix = formObject.Matrix;
                    canvas.Concat(ref formMatrix);

                    var picture = formObject.Render();
                    // DumpImage(picture);

                    canvas.DrawPicture(picture);
                    if (state.AlphaShape is object)
                    {

                        var formObj = new FormXObject(state.AlphaShape);
                        using (var bitmap = new SKBitmap((int)Math.Floor(formObj.Box.Width), (int)Math.Floor(formObj.Box.Height)))
                        using (var tmpCanvas = new SKCanvas(bitmap))
                        {
                            var newState = new GraphicsState(new ContentScanner(formObj, tmpCanvas, formObj.Size));
                            state.CopyTo(newState);
                            //var img2 = img.LoadImage(newState);
                            newState.Scanner.Render(newState.Scanner.RenderContext, new SKSize(formObject.Box.Width, formObject.Box.Height));

                            tmpCanvas.Flush();
                            DumpImage(bitmap);
                            ApplyMask(canvas, bitmap, formObj.Box.Left, formObj.Box.Top);
                        }
                    }

                    //if (state.AlphaShape is object)
                    //{
                    //    System.IO.File.WriteAllBytes("raw alpha", state.AlphaShape);
                    //    using (var bitmap = SKBitmap.Decode(state.AlphaShape, new SKImageInfo
                    //    {
                    //        AlphaType = SKAlphaType.Opaque,
                    //        ColorType = SKColorType.Alpha8
                    //    }))
                    //    {
                    //        canvas.DrawBitmap(bitmap, 0, 0, new SKPaint()
                    //        {
                    //            BlendMode = SKBlendMode.SrcIn
                    //        });
                    //    }
                    //}

                }
            }
            finally
            {
                canvas.Restore();
            }
        }

        #endregion
        #endregion
        #endregion

        public SKBitmap ApplyMask(SKCanvas canvas, SKBitmap alphaMask, float left, float top)
        {
            SKBitmap target = new SKBitmap(alphaMask.Width, alphaMask.Height, false);
            var size = new SKSize(alphaMask.Width, alphaMask.Height);

            if (target.InstallMaskPixels(SKMask.Create(alphaMask.Bytes, new SKRectI(0, 0, alphaMask.Width, alphaMask.Height), (uint)alphaMask.RowBytes, SKMaskFormat.A8)))
            {

            }
            else
            {

            }
            DumpImage(target);
            canvas.DrawBitmap(target, 0, 0, new SKPaint
            {
                
            });
            return target;
        }
    }
}