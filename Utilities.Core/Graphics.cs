/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Utilities.Core
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct BITMAPINFOHEADER
  {
    public UInt32 biSize;
    public Int32 biWidth;
    public Int32 biHeight;
    public Int16 biPlanes;
    public Int16 biBitCount;
    public UInt32 biCompression;
    public UInt32 biSizeImage;
    public Int32 biXPelsPerMeter;
    public Int32 biYPelsPerMeter;
    public UInt32 biClrUsed;
    public UInt32 biClrImportant;
  }

  public static class GraphicsUtils
  {
    private static String GetFileExtension(String filename)
    {
      return Path.GetExtension(filename).ToLower().TrimStart(".".ToCharArray());
    }

    public static String GetContentTypeFromFileExtension(String filename)
    {
      filename.Name("filename").NotNullEmptyOrOnlyWhitespace();

      var fileExtension = GetFileExtension(filename);

      if (String.IsNullOrWhiteSpace(fileExtension))
        throw new Exception(String.Format(Properties.Resources.Graphics_EmptyFileExtension, filename));
      else
        return "image/" + fileExtension;
    }

    public static ImageFormat GetImageFormatFromFileExtension(String filename)
    {
      filename.Name("filename").NotNullEmptyOrOnlyWhitespace();

      var fileExtension = GetFileExtension(filename);

      switch (fileExtension)
      {
        case "bmp":
          return ImageFormat.Bmp;

        case "emf":
          return ImageFormat.Emf;

        case "exif":
          return ImageFormat.Exif;

        case "gif":
          return ImageFormat.Gif;

        case "icon":
          return ImageFormat.Icon;

        case "jpg":
        case "jpeg":
          return ImageFormat.Jpeg;

        case "png":
          return ImageFormat.Png;

        case "tif":
        case "tiff":
          return ImageFormat.Tiff;

        case "wmf":
          return ImageFormat.Wmf;

        default:
          throw new Exception(String.Format(Properties.Resources.Graphics_BadFileExtension, fileExtension));
      }
    }

    public static Bitmap GetResizedImage(String filename, Size boundingRectangle)
    {
      using (var bitmap = new Bitmap(filename))
        return GetResizedImage(bitmap, boundingRectangle);
    }

    public static Bitmap GetResizedImage(Bitmap originalImage, Size boundingRectangle)
    {
      Size newRectangle = GetNewSize(new Size(originalImage.Width, originalImage.Height), boundingRectangle);

      var thumbnail = new Bitmap(newRectangle.Width, newRectangle.Height, originalImage.PixelFormat);
      using (var gfx = Graphics.FromImage(thumbnail))
      {
        gfx.CompositingQuality = CompositingQuality.HighQuality;
        gfx.SmoothingMode = SmoothingMode.HighQuality;
        gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var rectangle = new Rectangle(0, 0, newRectangle.Width, newRectangle.Height);
        gfx.DrawImage(originalImage, rectangle);
        return thumbnail;
      }
    }

    public static Size GetNewSize(Size originalDimensions, Size boundingRectangle)
    {
      var result = new Size();
      var percentDifferenceInWidth = Convert.ToDouble(originalDimensions.Width) / Convert.ToDouble(boundingRectangle.Width);

      result.Width = boundingRectangle.Width;
      result.Height = Convert.ToInt32(Convert.ToDouble(originalDimensions.Height) / percentDifferenceInWidth);

      // The aspect ratios of the original and bounding rectangles are on opposite sides of 1.0.
      // That means the result height is too big to fit in the bounding rectangle.
      // The result needs to be calculated again to make the height fit, and the
      // width needs to be scaled down to maintain the original rectangle's aspect ratio.
      if (result.Height > boundingRectangle.Height)
      {
        var percentDifferenceInHeight = Convert.ToDouble(result.Height) / Convert.ToDouble(boundingRectangle.Height);
        result.Width = Convert.ToInt32(Convert.ToDouble(boundingRectangle.Width) / percentDifferenceInHeight);
        result.Height = boundingRectangle.Height;
      }

      return result;
    }

    public static Bitmap GetTextImage(String message)
    {
      return GetTextImage(message, new Font("Arial", 12), Color.White, Color.Black);
    }

    public static Bitmap GetTextImage(String message, Font font)
    {
      return GetTextImage(message, font, Color.White, Color.Black);
    }

    public static Bitmap GetTextImage(String message, Color backgroundColor, Color foregroundColor)
    {
      return GetTextImage(message, new Font("Arial", 12), backgroundColor, foregroundColor);
    }

    public static Bitmap GetTextImage(String message, Font font, Color backgroundColor, Color foregroundColor)
    {
      var rectangle = GetRectangleForText(message, font);
      var bmp = new Bitmap(rectangle.Width, rectangle.Height);

      using (var gfx = Graphics.FromImage(bmp))
      {
        gfx.CompositingQuality = CompositingQuality.HighQuality;
        gfx.SmoothingMode = SmoothingMode.HighQuality;
        gfx.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        gfx.FillRectangle(new SolidBrush(backgroundColor), 0, 0, bmp.Width, bmp.Height);
        gfx.DrawString(message, font, new SolidBrush(foregroundColor), 20, 5);
        return bmp;
      }
    }

    public static Rectangle GetRectangleForText(String text, Font font)
    {
      return GetRectangleForText(text, font.Name, font.Size);
    }

    public static Rectangle GetRectangleForText(String text, String fontName, Single fontSize)
    {
      using (var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
      {
        using (var gfx = Graphics.FromImage(bmp))
        {
          SizeF stringSize = gfx.MeasureString(text, new Font(fontName, fontSize));

          // MeasureString has some quirks.  Adding these padding factors seems to make
          // the returned dimensions correct for the given text.  Tested with
          // several fonts, and both single-line and multi-line text.
          // (No, I'm not happy with this kludge...)
          return new Rectangle() { Width = ((Int32) stringSize.Width) + 20, Height = ((Int32) stringSize.Height) + 10 };
        }
      }
    }

    public static Double GetImageAspectRatio(Size size)
    {
      return GetImageAspectRatio(size.Width, size.Height);
    }

    public static Double GetImageAspectRatio(Image image)
    {
      return GetImageAspectRatio(image.Width, image.Height);
    }

    public static Double GetImageAspectRatio(Int32 width, Int32 height)
    {
      return Convert.ToDouble(width) / Convert.ToDouble(height);
    }

    public static void CaptureScreen(String fileName, ImageFormat imageFormat)
    {
      Int32 hdcSrc = Win32Api.GetWindowDC(Win32Api.GetDesktopWindow());
      Int32 hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);
      Int32 hBitmap = Gdi32.CreateCompatibleBitmap(hdcSrc, Gdi32.GetDeviceCaps(hdcSrc, Gdi32.HorzRes), Gdi32.GetDeviceCaps(hdcSrc, Gdi32.VertRes));

      try
      {
        Gdi32.SelectObject(hdcDest, hBitmap);
        Gdi32.BitBlt(hdcDest, 0, 0, Gdi32.GetDeviceCaps(hdcSrc, Gdi32.HorzRes), Gdi32.GetDeviceCaps(hdcSrc, Gdi32.VertRes), hdcSrc, 0, 0, 0x00CC0020);
        SaveImageAs(hBitmap, fileName, imageFormat);
      }
      finally
      {
        Win32Api.ReleaseDC(Win32Api.GetDesktopWindow(), hdcSrc);
        Gdi32.DeleteDC(hdcDest);
        Gdi32.DeleteObject(hBitmap);
      }
    }

    private static void SaveImageAs(Int32 hBitmap, String fileName, ImageFormat imageFormat)
    {
      new Bitmap(
        Image.FromHbitmap(new IntPtr(hBitmap)),
        Image.FromHbitmap(new IntPtr(hBitmap)).Width,
        Image.FromHbitmap(new IntPtr(hBitmap)).Height).Save(fileName, imageFormat);
    }
  }

  public sealed class Gdi32
  {
    private Gdi32() : base() { }

    /* Device Parameters for GetDeviceCaps(). Translated from WinGDI.h. */

    public const Int32 DriverVersion = 0;     /* Device driver version                    */
    public const Int32 Technology = 2;        /* Device classification                    */
    public const Int32 HorSize = 4;           /* Horizontal size in millimeters           */
    public const Int32 VerSize = 6;           /* Vertical size in millimeters             */
    public const Int32 HorzRes = 8;           /* Horizontal width in pixels               */
    public const Int32 VertRes = 10;          /* Vertical height in pixels                */
    public const Int32 BitsPixel = 12;        /* Number of bits per pixel                 */
    public const Int32 Planes = 14;           /* Number of planes                         */
    public const Int32 NumBrushes = 16;       /* Number of brushes the device has         */
    public const Int32 NumPens = 18;          /* Number of pens the device has            */
    public const Int32 NumMarkers = 20;       /* Number of markers the device has         */
    public const Int32 NumFonts = 22;         /* Number of fonts the device has           */
    public const Int32 NumColors = 24;        /* Number of colors the device supports     */
    public const Int32 PDeviceSize = 26;      /* Size required for device descriptor      */
    public const Int32 CurveCaps = 28;        /* Curve capabilities                       */
    public const Int32 LineCaps = 30;         /* Line capabilities                        */
    public const Int32 PolygonalCaps = 32;    /* Polygonal capabilities                   */
    public const Int32 TextCaps = 34;         /* Text capabilities                        */
    public const Int32 ClipCaps = 36;         /* Clipping capabilities                    */
    public const Int32 RasterCaps = 38;       /* Bitblt capabilities                      */
    public const Int32 AspectX = 40;          /* Length of the X leg                      */
    public const Int32 AspectY = 42;          /* Length of the Y leg                      */
    public const Int32 AspectXY = 44;         /* Length of the hypotenuse                 */

    public const Int32 LogPixelsX = 88;       /* Logical pixels/inch in X                 */
    public const Int32 LogPixelsY = 90;       /* Logical pixels/inch in Y                 */

    public const Int32 SizePalette = 104;     /* Number of entries in physical palette    */
    public const Int32 NumReserved = 106;     /* Number of reserved entries in palette    */
    public const Int32 ColorRes = 108;        /* Actual color resolution                  */

    // Printing related DeviceCaps. These replace the appropriate Escapes

    public const Int32 PhysicalWidth = 110;   /* Physical Width in device units           */
    public const Int32 PhysicalHeight = 111;  /* Physical Height in device units          */
    public const Int32 PhysicalOffsetX = 112; /* Physical Printable Area x margin         */
    public const Int32 PhysicalOffsetY = 113; /* Physical Printable Area y margin         */
    public const Int32 ScalingFactorX = 114;  /* Scaling factor x                         */
    public const Int32 ScalingFactorY = 115;  /* Scaling factor y                         */

    // Display driver specific

    public const Int32 VRefresh = 116;        /* Current vertical refresh rate of the display device (for displays only) in Hz*/
    public const Int32 DesktopVertRes = 117;  /* Horizontal width of entire desktop in pixels */
    public const Int32 DesktopHorzRes = 118;  /* Vertical height of entire desktop in pixels */
    public const Int32 BltAlignment = 119;    /* Preferred blt alignment                 */

    public const Int32 ShadeBlendCaps = 120;  /* Shading and blending caps               */
    public const Int32 ColorMgmtCaps = 121;   /* Color Management caps                   */

    /* Device Capability Masks: */

    /* Device Technologies */

    public const Int32 DtPlotter = 0;               /* Vector plotter                   */
    public const Int32 DtRasDisplay = 1;            /* Raster display                   */
    public const Int32 DtRasPrinter = 2;            /* Raster printer                   */
    public const Int32 DtRasCamera = 3;             /* Raster camera                    */
    public const Int32 DtCharStream = 4;            /* Character-stream, PLP            */
    public const Int32 DtMetaFile = 5;              /* Metafile, VDM                    */
    public const Int32 DtDispFile = 6;              /* Display-file                     */

    /* Curve Capabilities */

    public const Int32 CcNone = 0;                  /* Curves not supported             */
    public const Int32 CcCircles = 1;               /* Can do circles                   */
    public const Int32 CcPie = 2;                   /* Can do pie wedges                */
    public const Int32 CcChord = 4;                 /* Can do chord arcs                */
    public const Int32 CcEllipses = 8;              /* Can do ellipses                  */
    public const Int32 CcWide = 16;                 /* Can do wide lines                */
    public const Int32 CcStyled = 32;               /* Can do styled lines              */
    public const Int32 CcWideStyled = 64;           /* Can do wide styled lines         */
    public const Int32 CcInteriors = 128;           /* Can do interiors                 */
    public const Int32 CcRoundRect = 256;

    /* Line Capabilities */

    public const Int32 LcNone = 0;                  /* Lines not supported              */
    public const Int32 LcPolyLine = 2;              /* Can do polylines                 */
    public const Int32 LcMarker = 4;                /* Can do markers                   */
    public const Int32 LcPolyMarker = 8;            /* Can do polymarkers               */
    public const Int32 LcWide = 16;                 /* Can do wide lines                */
    public const Int32 LcStyled = 32;               /* Can do styled lines              */
    public const Int32 LcWideStyled = 64;           /* Can do wide styled lines         */
    public const Int32 LcInteriors = 128;           /* Can do interiors                 */

    /* Polygonal Capabilities */

    public const Int32 PcNone = 0;                  /* Polygonals not supported         */
    public const Int32 PcPolygon = 1;               /* Can do polygons                  */
    public const Int32 PcRectangle = 2;             /* Can do rectangles                */
    public const Int32 PcWindPolygon = 4;           /* Can do winding polygons          */
    public const Int32 PcTrapezoid = 4;             /* Can do trapezoids                */
    public const Int32 PcScanline = 8;              /* Can do scanlines                 */
    public const Int32 PcWide = 16;                 /* Can do wide borders              */
    public const Int32 PcStyled = 32;               /* Can do styled borders            */
    public const Int32 PcWideStyled = 64;           /* Can do wide styled borders       */
    public const Int32 PcInteriors = 128;           /* Can do interiors                 */
    public const Int32 PcPolyPolygon = 256;         /* Can do polypolygons              */
    public const Int32 PcPaths = 512;               /* Can do paths                     */

    /* Clipping Capabilities */

    public const Int32 CpNone = 0;                  /* No clipping of output            */
    public const Int32 CpRectangle = 1;             /* Output clipped to rects          */
    public const Int32 CpRegion = 2;                /* obsolete                         */

    /* Text Capabilities */
    
    public const Int32 TcOpCharacter = 0x00000001; /* Can do OutputPrecision   CHARACTER      */
    public const Int32 TcOpStroke = 0x00000002;    /* Can do OutputPrecision   STROKE         */
    public const Int32 TcCpStroke = 0x00000004;    /* Can do ClipPrecision     STROKE         */
    public const Int32 TcCr90 = 0x00000008;        /* Can do CharRotAbility    90             */
    public const Int32 TcCrAny = 0x00000010;       /* Can do CharRotAbility    ANY            */
    public const Int32 TcSfXyIndep = 0x00000020;   /* Can do ScaleFreedom      X_YINDEPENDENT */
    public const Int32 TcSaDouble = 0x00000040;    /* Can do ScaleAbility      DOUBLE         */
    public const Int32 TcSaInteger = 0x00000080;   /* Can do ScaleAbility      INTEGER        */
    public const Int32 TcSaContin = 0x00000100;    /* Can do ScaleAbility      CONTINUOUS     */
    public const Int32 TcEaDouble = 0x00000200;    /* Can do EmboldenAbility   DOUBLE         */
    public const Int32 TcIaAble = 0x00000400;      /* Can do ItalisizeAbility  ABLE           */
    public const Int32 TcUaAble = 0x00000800;      /* Can do UnderlineAbility  ABLE           */
    public const Int32 TcSoAble = 0x00001000;      /* Can do StrikeOutAbility  ABLE           */
    public const Int32 TcRaAble = 0x00002000;      /* Can do RasterFontAble    ABLE           */
    public const Int32 TcVaAble = 0x00004000;      /* Can do VectorFontAble    ABLE           */
    public const Int32 TcReserved = 0x00008000;
    public const Int32 TcScrollBlt = 0x00010000;   /* Don't do text scroll with blt           */

    /* Raster Capabilities */
    
    public const Int32 RcNone = 0;
    public const Int32 RcBitBlt = 1;                /* Can do standard BLT.             */
    public const Int32 RcBanding = 2;               /* Device requires banding support  */
    public const Int32 RcScaling = 4;               /* Device requires scaling support  */
    public const Int32 RcBitmap64 = 8;              /* Device can support >64K bitmap   */
    public const Int32 RcGdi20Output = 0x0010;      /* has 2.0 output calls         */
    public const Int32 RcGdi20State = 0x0020;
    public const Int32 RcSaveBitmap = 0x0040;
    public const Int32 RcDiBitmap = 0x0080;         /* supports DIB to memory       */
    public const Int32 RcPalette = 0x0100;          /* supports a palette           */
    public const Int32 RcDibToDev = 0x0200;         /* supports DIBitsToDevice      */
    public const Int32 RcBigFont = 0x0400;          /* supports >64K fonts          */
    public const Int32 RcStretchBlt = 0x0800;       /* supports StretchBlt          */
    public const Int32 RcFloodFill = 0x1000;        /* supports FloodFill           */
    public const Int32 RcStretchDib = 0x2000;       /* supports StretchDIBits       */
    public const Int32 RcOpDxOutput = 0x4000;
    public const Int32 RcDevBits = 0x8000;

    /* Shading and blending caps */
    
    public const Int32 SbNone = 0x00000000;
    public const Int32 SbConstAlpha = 0x00000001;
    public const Int32 SbPixelAlpha = 0x00000002;
    public const Int32 SbPremultAlpha = 0x00000004;
    public const Int32 SbGradRect = 0x00000010;
    public const Int32 SbGradTri = 0x00000020;

    /* Color Management caps */
    
    public const Int32 CmNone = 0x00000000;
    public const Int32 CmDeviceIcm = 0x00000001;
    public const Int32 CmGammaRamp = 0x00000002;
    public const Int32 CmCmykColor = 0x00000004;

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "BitBlt", SetLastError = true)]
    public static extern Boolean BitBlt(Int32 hdcDest, Int32 nXDest, Int32 nYDest, Int32 nWidth, Int32 nHeight, Int32 hdcSrc, Int32 nXSrc, Int32 nYSrc, Int32 dwRop);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "CreateCompatibleBitmap", SetLastError = true)]
    public static extern Int32 CreateCompatibleBitmap(Int32 hdc, Int32 nWidth, Int32 nHeight);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "CreateCompatibleDC", SetLastError = true)]
    public static extern Int32 CreateCompatibleDC(Int32 hdc);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "DeleteDC", SetLastError = true)]
    public static extern Boolean DeleteDC(Int32 hdc);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "DeleteObject", SetLastError = true)]
    public static extern Boolean DeleteObject(Int32 hObject);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "GetDeviceCaps", SetLastError = true)]
    public static extern Int32 GetDeviceCaps(Int32 hdc, Int32 nIndex);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "SelectObject", SetLastError = true)]
    public static extern Int32 SelectObject(Int32 hdc, Int32 hgdiobj);
  }

  // BitmapHasher provides one static method to determine the
  // 32-bit hash (unique) value of a given bitmap.
  // The code for the class was ported directly from a C code
  // algorithm for the PNG graphics format at http://www.libpng.org/pub/png/spec/PNG-CRCAppendix.html.
  // A few C# specific constructs were added, and the code was shortened
  // somewhat, but otherwise remains as close as possible to the original C code.
  
  // The code is very slow in C#.  It needs to be refactored to use pointers and fixed memory structures
  // to bring it up to native C speeds.
  public sealed class BitmapHasher
  {
    private BitmapHasher() : base() { }

    // Use a lookup table for speed.
    private static readonly UInt32[] _CRCTable = new UInt32[256];

    static BitmapHasher()
    {
      // Populate the CRC table.
      for (UInt32 n = 0; n < 256; n++)
      {
        UInt32 c = n;
        for (UInt32 k = 0; k < 8; k++)
        {
          if ((c & 1) == 1)
            c = 0xEDB88320 ^ (c >> 1);
          else
            c = c >> 1;
        }
        BitmapHasher._CRCTable[n] = c;
      }
    }

    /// <summary>
    /// Static method which returns the unsigned 32-bit integer hash value
    /// of a bitmap.
    /// </summary>
    /// <param name="image">A <see cref="System.Drawing.Bitmap"/> 
    /// object.</param>
    /// <returns>A <c>UInt32 (System.UInt32)</c> hash value for the 
    /// <c>image</c> bitmap.</returns>
    /// <remarks>
    /// I'm not going to pretend that I know exactly how this code works.
    /// I didn't write it (I only ported it from C), and all the bit-shifting
    /// and exclusive-OR (^) operations leaves me dizzy :).  
    /// <para>
    /// However, it does appear to work, although it's much slower than
    /// the native C implementation.  Refactoring pointers and fixed memory
    /// data structures into the code would be needed to give it acceptable
    /// performance.
    /// </para>
    /// <para>
    /// Note that this method is not Common Language Specification (CLS) compliant 
    /// because it returns an unsigned 32-bit integer.  If you want to call
    /// this code from a non-C# language that does not support the
    /// System.UInt32 data type, the call probably won't work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code lang="C#">
    ///   // Example 1:
    ///   UInt32 hash = BitmapHasher.GetBitmapHashValue(myImageList.Images[1] as Bitmap);
    /// <para>
    ///   // Example 2:
    ///   Icon icon = new Icon("C:\IMAGES\FLOWER.ICO");
    ///   UInt32 hash = BitmapHasher.GetBitmapHashValue(icon.ToBitmap());
    /// </para>
    /// </code>
    /// </example>
    public static UInt32 GetBitmapHashValue(Bitmap image)
    {
      const UInt32 c = 0xFFFFFFFF;
      var result = c;

      for (var x = 0; x < image.Width; x++)
        for (var y = 0; y < image.Height; y++)
          result = BitmapHasher._CRCTable[(result ^ image.GetPixel(x, y).ToArgb()) & 0xFF] ^ (result >> 8);

      return result ^ c;
    }
  }
}
