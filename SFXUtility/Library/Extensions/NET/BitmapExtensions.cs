#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 BitmapExtensions.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

#endregion

namespace SFXUtility.Library.Extensions.NET
{
    public static class BitmapExtensions
    {
        /// <exception cref="Exception">The operation failed.</exception>
        public static Bitmap Grayscale(this Bitmap source)
        {
            var newBitmap = new Bitmap(source.Width, source.Height);
            using (var g = Graphics.FromImage(newBitmap))
            {
                var colorMatrix =
                    new ColorMatrix(
                        new[]
                        {
                            new[] { .3f, .3f, .3f, 0, 0 }, new[] { .59f, .59f, .59f, 0, 0 },
                            new[] { .11f, .11f, .11f, 0, 0 }, new float[] { 0, 0, 0, 1, 0 },
                            new float[] { 0, 0, 0, 0, 1 }
                        });
                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(
                        source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height,
                        GraphicsUnit.Pixel, attributes);
                }
            }
            return newBitmap;
        }

        /// <exception cref="Exception">The operation failed.</exception>
        public static Bitmap Resize(this Bitmap source, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(source.HorizontalResolution, source.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(
                        source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }
    }
}