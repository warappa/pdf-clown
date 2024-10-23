/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfClown.Objects;
using PdfClown.Util.Parsers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PdfClown.Tokens
{
    /// <summary>PDF file reader.</summary>
    public sealed class Reader : IDisposable
    {
        public sealed class FileInfo
        {
            private PdfDictionary trailer;
            private PdfVersion version;
            private IDictionary<int, XRefEntry> xrefEntries;

            internal FileInfo(PdfVersion version, PdfDictionary trailer, IDictionary<int, XRefEntry> xrefEntries)
            {
                this.version = version;
                this.trailer = trailer;
                this.xrefEntries = xrefEntries;
            }

            public PdfDictionary Trailer => trailer;

            public PdfVersion Version => version;

            public IDictionary<int, XRefEntry> XrefEntries => xrefEntries;
        }

        private FileParser parser;

        internal Reader(IInputStream stream, PdfFile file, string password = null, Stream keyStoreInputStream = null)
        { this.parser = new FileParser(stream, file, password, keyStoreInputStream); }

        ~Reader()
        { Dispose(false); }

        public override int GetHashCode()
        {
            return parser.GetHashCode();
        }

        public FileParser Parser => parser;

        /// <summary>Retrieves the file information.</summary>
        public FileInfo ReadInfo()
        {
            //TODO:hybrid xref table/stream
            PdfVersion version = PdfVersion.Get(parser.RetrieveVersion());
            PdfDictionary trailer = null;
            var xrefEntries = new Dictionary<int, XRefEntry>();
            long sectionOffset = parser.RetrieveXRefOffset();
            long? xrefStm = null;
            while (sectionOffset > -1)
            {
                // Move to the start of the xref section!
                parser.Seek(sectionOffset);

                PdfDictionary sectionTrailer = null;
                if (parser.MoveNextComplex()
                    && parser.TokenType == TokenTypeEnum.Keyword
                    && MemoryExtensions.Equals(parser.CharsToken, Keyword.XRef, StringComparison.Ordinal)) // XRef-table section.
                {
                    ReadXRefTable(xrefEntries);
                    // Get the previous trailer!
                    sectionTrailer = (PdfDictionary)parser.ParsePdfObject(1);
                }
                else // XRef-stream section.
                {
                    var obj = parser.ParsePdfObject(1);
                    if (obj is PdfDictionary dictinary
                        && dictinary.ContainsKey(PdfName.Linearized))
                    {
                        var xrefOffcet = dictinary.GetInt(PdfName.T, -1);
                        if (xrefOffcet > -1)
                        {
                            parser.Seek(xrefOffcet);
                            ReadXRefTable(xrefEntries);
                            // Get the previous trailer!
                            sectionTrailer = (PdfDictionary)parser.ParsePdfObject(1);
                        }
                    }
                    else if (obj is XRefStream stream)
                    {
                        try
                        {
                            // XRef-stream subsection entries.
                            stream.ReadEntries();
                        }
                        catch (ParseException)
                        {
                            RecoveryXRefStream(stream);
                        }

                        foreach (XRefEntry xrefEntry in stream.Values)
                        {
                            if (xrefEntries.ContainsKey(xrefEntry.Number)) // Already-defined entry.
                                continue;

                            // Define entry!
                            xrefEntries[xrefEntry.Number] = xrefEntry;
                        }

                        // Get the previous trailer!
                        sectionTrailer = stream.Header;
                    }
                    else if (xrefStm != null)
                    {
                        sectionOffset = xrefStm.Value;
                        continue;
                    }
                }

                if (trailer == null)
                { trailer = sectionTrailer; }

                // Get the previous xref-table section's offset!
                sectionOffset = sectionTrailer?.GetInt(PdfName.Prev, -1) ?? -1;
                xrefStm = sectionTrailer?.GetNInt(PdfName.XRefStm);                
            }

            return new FileInfo(version, trailer, xrefEntries);
        }

        private void RecoveryXRefStream(XRefStream stream)
        {
            Debug.WriteLine("Restore XRef stream");
            stream.Header[PdfName.Prev] = null;
            stream.Entries.Clear();
            parser.Stream.Position = 0;
            while (parser.MoveNextComplex())
            {
                if (parser.TokenType == TokenTypeEnum.InderectObject)
                {
                    var reference = parser.ReferenceToken;
                    var pdfObject = parser.ParsePdfObject(1);
                    if (pdfObject is XRefStream)
                        continue;

                    var entryIndex = reference.ObjectNumber;
                    stream.Entries[entryIndex] = new XRefEntry(entryIndex, reference.GenerationNumber, (int)reference.Offset, XRefEntry.UsageEnum.InUse);

                    if (pdfObject is ObjectStream oStream)
                    {
                        foreach (var oEntry in oStream.Entries)
                        {
                            stream.Entries[oEntry.Key] = new XRefEntry(oEntry.Key, oEntry.Value.offset, reference.ObjectNumber);
                        }
                    }
                }
            }
        }

        private void ReadXRefTable(IDictionary<int, XRefEntry> xrefEntries)
        {
            // Looping sequentially across the subsections inside the current xref-table section...
            while (true)
            {
                // NOTE: Each iteration of this block represents the scanning of one subsection.
                // We get its bounds (first and last object numbers within its range) and then collect
                // its entries.
                // 1. First object number.
                parser.MoveNext();
                if (parser.TokenType == TokenTypeEnum.Keyword
                    && parser.CharsToken.Equals(Keyword.Trailer, StringComparison.Ordinal)) // XRef-table section ended.
                    break;
                else if (parser.TokenType != TokenTypeEnum.Integer)
                    throw new PostScriptParseException("Neither object number of the first object in this xref subsection nor end of xref section found.", parser);

                // Get the object number of the first object in this xref-table subsection!
                int startObjectNumber = parser.IntegerToken;

                // 2. Last object number.
                parser.MoveNext();
                if (parser.TokenType != TokenTypeEnum.Integer)
                    throw new PostScriptParseException("Number of entries in this xref subsection not found.", parser);

                // Get the object number of the last object in this xref-table subsection!
                int endObjectNumber = parser.IntegerToken + startObjectNumber;

                // 3. XRef-table subsection entries.
                for (int index = startObjectNumber; index < endObjectNumber; index++)
                {
                    if (xrefEntries.ContainsKey(index)) // Already-defined entry.
                    {
                        // Skip to the next entry!
                        parser.MoveNext(3);
                        continue;
                    }

                    // Get the indirect object offset!
                    int offset = parser.MoveNext() ? parser.IntegerToken : 0;
                    // Get the object generation number!
                    int generation = parser.MoveNext() ? parser.IntegerToken : 0;
                    // Get the usage tag!
                    XRefEntry.UsageEnum usage;
                    {
                        var usageToken = parser.MoveNext() ? parser.CharsToken : ReadOnlySpan<char>.Empty;
                        if (MemoryExtensions.Equals(usageToken, Keyword.InUseXrefEntry, StringComparison.Ordinal))
                            usage = XRefEntry.UsageEnum.InUse;
                        else if (MemoryExtensions.Equals(usageToken, Keyword.FreeXrefEntry, StringComparison.Ordinal))
                            usage = XRefEntry.UsageEnum.Free;
                        else
                            throw new PostScriptParseException("Invalid xref entry.", parser);
                    }

                    // Define entry!
                    xrefEntries[index] = new XRefEntry(index, generation, offset, usage);
                }
            }
        }

        internal void PrepareDecryption()
        {
            parser.PrepareDecryption();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (parser != null)
                {
                    parser.Dispose();
                    parser = null;
                }
            }
        }
    }
}