#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 FloatExtensions.cs is part of SFXLibrary.

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

#endregion

namespace SFXUtility.Library.Extensions.NET
{
    public static class FloatExtensions
    {
        /// <exception cref="OverflowException">
        ///     <paramref name="value" /> is less than <see cref="F:System.TimeSpan.MinValue" /> or
        ///     greater than <see cref="F:System.TimeSpan.MaxValue" />.-or-<paramref name="value" /> is
        ///     <see cref="F:System.Double.PositiveInfinity" />.-or-<paramref name="value" /> is
        ///     <see cref="F:System.Double.NegativeInfinity" />.
        /// </exception>
        public static string FormatTime(this float value, bool totalSeconds = false)
        {
            var ts = TimeSpan.FromSeconds(value);
            if (!totalSeconds && ts.TotalSeconds > 60)
            {
                return string.Format("{0}:{1:00}", (int) ts.TotalMinutes, ts.Seconds);
            }
            return string.Format("{0:0}", ts.TotalSeconds);
        }
    }
}