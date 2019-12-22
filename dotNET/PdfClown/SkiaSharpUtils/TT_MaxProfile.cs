using System;
using System.Runtime.InteropServices;

namespace PdfClown.SkiaSharpUtils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TT_MaxProfile
    {
        public long Version;
        public ushort NumGlyphs;
        public ushort MaxPoints;
        public ushort MaxContours;
        public ushort MaxCompositePoints;
        public ushort MaxCompositeContours;
        public ushort MaxZones;
        public ushort MaxTwilightPoints;
        public ushort MaxStorage;
        public ushort MaxFunctionDefs;
        public ushort MaxInstructionDefs;
        public ushort MaxStackElements;
        public ushort MaxSizeOfInstructions;
        public ushort MaxComponentElements;
        public ushort MaxComponentDepth;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TT_Header
    {
        public long Table_Version;
        public long Font_Revision;

        public long CheckSum_Adjust;
        public long Magic_Number;

        public ushort Flags;
        public ushort Units_Per_EM;

        public ulong[] Created;
        //public ulong[] Modified; = Arrays.InitializeWithDefaultInstances<ulong>(2);

        public short xMin;
        public short yMin;
        public short xMax;
        public short yMax;

        public ushort Mac_Style;
        public ushort Lowest_Rec_PPEM;

        public short Font_Direction;
        public short Index_To_Loc_Format;
        public short Glyph_Data_Format;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TT_HoriHeader
    {
        public long Version;
        public short Ascender;
        public short Descender;
        public short Line_Gap;

        public ushort advance_Width_Max; // advance width maximum

        public short min_Left_Side_Bearing; // minimum left-sb
        public short min_Right_Side_Bearing; // minimum right-sb
        public short xMax_Extent; // xmax extents
        public short caret_Slope_Rise;
        public short caret_Slope_Run;
        public short caret_Offset;

        //public short[] Reserved = new short[4];

        public short metric_Data_Format;
        public ushort number_Of_HMetrics;

        /* The following fields are not defined by the OpenType specification */
        /* but they are used to connect the metrics header to the relevant    */
        /* 'hmtx' table.                                                      */

        //public IntPtr long_metrics;
        //public IntPtr short_metrics;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TT_VertHeader
    {
        long Version;
        short Ascender;
        short Descender;
        short Line_Gap;

        ushort advance_Height_Max;      /* advance height maximum */

        short min_Top_Side_Bearing;    /* minimum top-sb          */
        short min_Bottom_Side_Bearing; /* minimum bottom-sb       */
        short yMax_Extent;             /* ymax extents            */
        short caret_Slope_Rise;
        short caret_Slope_Run;
        short caret_Offset;

        //short Reserved[4];

        short metric_Data_Format;
        ushort number_Of_VMetrics;

        /* The following fields are not defined by the OpenType specification */
        /* but they are used to connect the metrics header to the relevant    */
        /* 'vmtx' table.                                                      */

        //void* long_metrics;
        //void* short_metrics;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TT_OS2
    {
        ushort version;                /* 0x0001 - more or 0xFFFF */
        short xAvgCharWidth;
        ushort usWeightClass;
        ushort usWidthClass;
        ushort fsType;
        short ySubscriptXSize;
        short ySubscriptYSize;
        short ySubscriptXOffset;
        short ySubscriptYOffset;
        short ySuperscriptXSize;
        short ySuperscriptYSize;
        short ySuperscriptXOffset;
        short ySuperscriptYOffset;
        short yStrikeoutSize;
        short yStrikeoutPosition;
        short sFamilyClass;

        //byte panose[10];

        ulong ulUnicodeRange1;        /* Bits 0-31   */
        ulong ulUnicodeRange2;        /* Bits 32-63  */
        ulong ulUnicodeRange3;        /* Bits 64-95  */
        ulong ulUnicodeRange4;        /* Bits 96-127 */

        //char achVendID[4];

        ushort fsSelection;
        ushort usFirstCharIndex;
        ushort usLastCharIndex;
        short sTypoAscender;
        short sTypoDescender;
        short sTypoLineGap;
        ushort usWinAscent;
        ushort usWinDescent;

        /* only version 1 and higher: */

        ulong ulCodePageRange1;       /* Bits 0-31   */
        ulong ulCodePageRange2;       /* Bits 32-63  */

        /* only version 2 and higher: */

        short sxHeight;
        short sCapHeight;
        ushort usDefaultChar;
        ushort usBreakChar;
        ushort usMaxContext;

        /* only version 5 and higher: */

        ushort usLowerOpticalPointSize;       /* in twips (1/20th points) */
        ushort usUpperOpticalPointSize;       /* in twips (1/20th points) */

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TT_Postscript
    {
        long FormatType;
        long italicAngle;
        short underlinePosition;
        short underlineThickness;
        ulong isFixedPitch;
        ulong minMemType42;
        ulong maxMemType42;
        ulong minMemType1;
        ulong maxMemType1;

        /* Glyph names follow in the 'post' table, but we don't */
        /* load them by default.                                */
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct TT_PCLT
    {
        long Version;
        ulong FontNumber;
        ushort Pitch;
        ushort xHeight;
        ushort Style;
        ushort TypeFamily;
        ushort CapHeight;
        ushort SymbolSet;
        fixed char TypeFace[16];
        fixed char CharacterComplement[8];
        fixed char FileName[6];
        char StrokeWeight;
        char WidthType;
        byte SerifStyle;
        byte Reserved;

    }
}