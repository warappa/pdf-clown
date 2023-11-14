/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Documents;
using multimedia = PdfClown.Documents.Multimedia;
using PdfClown.Objects;

using System;
using SkiaSharp;

namespace PdfClown.Documents.Interaction.Annotations
{
    /**
      <summary>Movie annotation [PDF:1.6:8.4.5].</summary>
    */
    [PDF(VersionEnum.PDF12)]
    public sealed class Movie : Annotation
    {
        public Movie(Page page, SKRect box, string text, multimedia::Movie content)
            : base(page, PdfName.Movie, box, text)
        { Content = content; }

        internal Movie(PdfDirectObject baseObject) : base(baseObject)
        { }

        /**
          <summary>Gets/Sets the movie to be played.</summary>
        */
        public multimedia::Movie Content
        {
            get => Wrap<multimedia::Movie>(BaseDataObject[PdfName.Movie]);
            set
            {
                if (value == null)
                    throw new ArgumentException("Movie MUST be defined.");

                BaseDataObject[PdfName.Movie] = value.BaseObject;
            }
        }
    }
}