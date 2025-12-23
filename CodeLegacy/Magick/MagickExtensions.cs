using ImageMagick;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Flowframes.Magick
{
    public enum BitmapDensity
    {
        /// <summary>
        /// Ignore the density of the image when creating the bitmap.
        /// </summary>
        Ignore,

        /// <summary>
        /// Use the density of the image when creating the bitmap.
        /// </summary>
        Use,
    }

    public static class MagickExtensions
    {
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive.")]
        public static Bitmap ToBitmap(this MagickImage imageMagick, ImageFormat imageFormat, BitmapDensity bitmapDensity)
        {
            imageMagick.Format = InternalMagickFormatInfo.GetFormat(imageFormat);

            MemoryStream memStream = new MemoryStream();
            imageMagick.Write(memStream);
            memStream.Position = 0;

            /* Do not dispose the memStream, the bitmap owns it. */
            var bitmap = new Bitmap(memStream);

            SetBitmapDensity(imageMagick, bitmap, bitmapDensity);

            return bitmap;
        }

        public static void FromBitmap(this MagickImage imageMagick, Bitmap bitmap)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                if (IsSupportedImageFormat(bitmap.RawFormat))
                    bitmap.Save(memStream, bitmap.RawFormat);
                else
                    bitmap.Save(memStream, ImageFormat.Bmp);

                memStream.Position = 0;
                imageMagick.Read(memStream);
            }
        }

        private static bool IsSupportedImageFormat(ImageFormat format)
        {
            return
                format.Guid.Equals(ImageFormat.Bmp.Guid) ||
                format.Guid.Equals(ImageFormat.Gif.Guid) ||
                format.Guid.Equals(ImageFormat.Icon.Guid) ||
                format.Guid.Equals(ImageFormat.Jpeg.Guid) ||
                format.Guid.Equals(ImageFormat.Png.Guid) ||
                format.Guid.Equals(ImageFormat.Tiff.Guid);
        }

        private static void SetBitmapDensity(MagickImage imageMagick, Bitmap bitmap, BitmapDensity bitmapDensity)
        {
            if (bitmapDensity == BitmapDensity.Use)
            {
                var dpi = GetDpi(imageMagick, bitmapDensity);
                bitmap.SetResolution((float)dpi.X, (float)dpi.Y);
            }
        }

        private static Density GetDpi(MagickImage imageMagick, BitmapDensity bitmapDensity)
        {
            if (bitmapDensity == BitmapDensity.Ignore || (imageMagick.Density.Units == DensityUnit.Undefined && imageMagick.Density.X == 0 && imageMagick.Density.Y == 0))
                return new Density(96);

            return imageMagick.Density.ChangeUnits(DensityUnit.PixelsPerInch);
        }
    }

    public class InternalMagickFormatInfo
    {
        internal static MagickFormat GetFormat(ImageFormat format)
        {
            if (format == ImageFormat.Bmp || format == ImageFormat.MemoryBmp)
                return MagickFormat.Bmp;
            else if (format == ImageFormat.Gif)
                return MagickFormat.Gif;
            else if (format == ImageFormat.Icon)
                return MagickFormat.Icon;
            else if (format == ImageFormat.Jpeg)
                return MagickFormat.Jpeg;
            else if (format == ImageFormat.Png)
                return MagickFormat.Png;
            else if (format == ImageFormat.Tiff)
                return MagickFormat.Tiff;
            else
                throw new NotSupportedException("Unsupported image format: " + format.ToString());
        }
    }
}