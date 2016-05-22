#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 OtherExtensions.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LeagueSharp;
using SharpDX.Direct3D9;

#endregion

namespace SFXChallenger.Library.Extensions.NET
{
    public static class OtherExtensions
    {
        public static bool Is24Hrs(this CultureInfo cultureInfo)
        {
            return cultureInfo.DateTimeFormat.ShortTimePattern.Contains("H");
        }

        public static bool IsNumber(this object value)
        {
            return value is sbyte || value is byte || value is short || value is ushort || value is int || value is uint ||
                   value is long || value is ulong || value is float || value is double || value is decimal;
        }

        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> list)
        {
            return Task.Run(() => list.ToList());
        }

        /// <exception cref="Exception">A delegate callback throws an exception. </exception>
        public static void RaiseEvent(this EventHandler @event, object sender, EventArgs e)
        {
            if (@event != null)
            {
                @event(sender, e);
            }
        }

        /// <exception cref="Exception">A delegate callback throws an exception. </exception>
        public static void RaiseEvent<T>(this EventHandler<T> @event, object sender, T e) where T : EventArgs
        {
            if (@event != null)
            {
                @event(sender, e);
            }
        }

        public static Texture ToTexture(this Bitmap bitmap)
        {
            return Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(bitmap, typeof(byte[])), bitmap.Width,
                bitmap.Height, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
        }

        /// <exception cref="Exception">The operation failed.</exception>
        public static Bitmap Scale(this Bitmap bitmap, float scale)
        {
            var scaled = new Bitmap((int) Math.Ceiling(bitmap.Width * scale), (int) Math.Ceiling(bitmap.Height * scale));
            using (var graphics = Graphics.FromImage(scaled))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(bitmap, new Rectangle(0, 0, scaled.Width, scaled.Height));
            }
            return scaled;
        }
    }
}