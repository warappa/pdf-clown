/*
  Copyright 2007-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Tokens;

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Objects
{
    /// <summary>Local graphics state [PDF:1.6:4.3.1].</summary>
    [PDF(VersionEnum.PDF10)]
    public sealed class GraphicsLocalState : ContainerObject
    {
        private static readonly byte[] BeginChunk = BaseEncoding.Pdf.Encode(SaveGraphicsState.OperatorKeyword + Symbol.LineFeed);
        private static readonly byte[] EndChunk = BaseEncoding.Pdf.Encode(RestoreGraphicsState.OperatorKeyword + Symbol.LineFeed);

        public GraphicsLocalState()
        { }

        public GraphicsLocalState(IList<ContentObject> objects) : base(objects)
        { }

        public override void OnScanning(GraphicsState state) => state.Save();

        public override void OnScanned(GraphicsState state) => state.Restore();

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            stream.Write(BeginChunk);
            base.WriteTo(stream, context);
            stream.Write(EndChunk);
        }
    }
}