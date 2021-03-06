/*
  Copyright 2006-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents;
using PdfClown.Files;
using PdfClown.Objects;

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfClown.Documents.Contents.ColorSpaces
{
    /**
      <summary>ICC-based color space [PDF:1.6:4.5.4].</summary>
    */
    // TODO:IMPL improve profile support (see ICC.1:2003-09 spec)!!!
    [PDF(VersionEnum.PDF13)]
    public sealed class ICCBasedColorSpace : ColorSpace
    {
        private SKColorSpace skColorSpace;
        private SKMatrix44 xyzD50 = SKMatrix44.CreateIdentity();
        private ICCProfile iccProfile;
        private SKColorSpaceTransferFn transfer;
        #region dynamic
        #region constructors
        //TODO:IMPL new element constructor!

        internal ICCBasedColorSpace(PdfDirectObject baseObject) : base(baseObject) { }
        #endregion

        #region interface
        #region public
        public override object Clone(Document context)
        {
            throw new NotImplementedException();
        }

        public override int ComponentCount => N;

        public override Color DefaultColor => DeviceGrayColor.Default;

        public override Color GetColor(IList<PdfDirectObject> components, IContentContext context)
        {
            if (components.Count == 1)
                return new DeviceGrayColor(components);
            else if (components.Count == 3)
                return new DeviceRGBColor(components); // FIXME:temporary hack...
            else if (components.Count == 4)
                return new DeviceCMYKColor(components);
            return null;
        }

        public override bool IsSpaceColor(Color color)
        { return color is DeviceColor; }

        public override SKColor GetSKColor(Color color, double? alpha = null)
        {
            // GetIccProfile();
            GetSKColorSpace();

            var skColor = SKColors.Black;
            // FIXME: temporary hack
            if (color is DeviceRGBColor devRGB)
            {
                var point = XYZtoRGB((float)devRGB.R, (float)devRGB.G, (float)devRGB.B);
                skColor = new SKColor(
                   (byte)(point.X * 255),
                   (byte)(point.Y * 255),
                   (byte)(point.Z * 255));
            }
            else if (color is DeviceCMYKColor devCMYK)
            {
                //TODO

            }
            else if (color is DeviceGrayColor devGray)
            {
                var point = XYZtoRGB((float)devGray.G, (float)devGray.G, (float)devGray.G);
                skColor = new SKColor(
                   (byte)(point.X * 255),
                   (byte)(point.Y * 255),
                   (byte)(point.Z * 255));
            }

            if (alpha != null)
            {
                skColor = skColor.WithAlpha((byte)(alpha.Value * 255));
            }
            return skColor;
        }

        public override SKColor GetSKColor(double[] components, double? alpha = null)
        {
            var skColor = SKColors.Black;
            if (components.Length == 3)
            {
                skColor = new SKColor(
                   (byte)(components[0] * 255),
                   (byte)(components[1] * 255),
                   (byte)(components[2] * 255));
            }
            else if (components.Length == 4)
            {
                //TODO
                skColor = new SKColor(
                   (byte)(components[0] * 255),
                   (byte)(components[1] * 255),
                   (byte)(components[2] * 255));
            }
            else if (components.Length == 1)
            {
                skColor = new SKColor(
                   (byte)(components[0] * 255),
                   (byte)(components[0] * 255),
                   (byte)(components[0] * 255));
            }

            if (alpha != null)
            {
                skColor = skColor.WithAlpha((byte)(alpha.Value * 255));
            }
            return skColor;
        }


        public PdfStream Profile => (PdfStream)((PdfArray)BaseDataObject).Resolve(1);

        public PdfName Alternate => Profile?.Header.Resolve(PdfName.Alternate) as PdfName;

        public int N => ((PdfInteger)Profile?.Header.Resolve(PdfName.N))?.RawValue ?? 0;


        public SKColorSpace GetSKColorSpace()
        {
            if (skColorSpace == null)
            {
                skColorSpace = SKColorSpace.CreateIcc(Profile.GetBody(true).ToByteArray());
                xyzD50 = skColorSpace.FromXyzD50();
                skColorSpace.GetNumericalTransferFunction(out var spaceTransfer);
                transfer = spaceTransfer.Invert();
            }
            return skColorSpace;
        }

        private void GetIccProfile()
        {
            if (iccProfile == null)
            {
                iccProfile = ICCProfile.Load(Profile.GetBody(true).ToByteArray());

            }
        }

        private SKPoint3 XYZtoRGB(float x, float y, float z)
        {
            return new SKPoint3(x, y, z);
            //var result = xyzD50.MapScalars(x, y, z, 1);
            ////float r = result[0], g = result[1], b = result[2];
            //float r = transfer.Transform(Math.Min(1F, Math.Max(0F, result[0]))), 
            //    g = transfer.Transform(Math.Min(1F, Math.Max(0F, result[1]))), 
            //    b = transfer.Transform(Math.Min(1F, Math.Max(0F, result[2])));

            //return new SKPoint3(r, g, b);
        }



        #endregion
        #endregion
        #endregion
    }



}