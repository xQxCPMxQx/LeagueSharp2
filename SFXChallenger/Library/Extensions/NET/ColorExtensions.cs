#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ColorExtensions.cs is part of SFXLibrary.

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

using System.Drawing;

#endregion

namespace SFXChallenger.Library.Extensions.NET
{
    public static class ColorExtensions
    {
        public static Color ToArgb(this Color color, int opacity)
        {
            return Color.FromArgb(opacity, color);
        }
    }
}