#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Draw.cs is part of SFXWard.

 SFXWard is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXWard is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXWard. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXWard.Library
{
    public class Draw
    {
        public static void Cross(Vector2 pos, float size, float thickness, Color color)
        {
            Drawing.DrawLine(pos.X - size, pos.Y - size, pos.X + size, pos.Y + size, thickness, color);
            Drawing.DrawLine(pos.X + size, pos.Y - size, pos.X - size, pos.Y + size, thickness, color);
        }

        public static void TextCentered(Vector2 pos, Color color, string content)
        {
            var rec = Drawing.GetTextExtent(content);
            Drawing.DrawText(pos.X - rec.Width / 2f, pos.Y - rec.Height / 2f, color, content);
        }

        public static void Rectangle(Vector2 pos, int width, int height, float thickness, Color color)
        {
            pos.Y = pos.Y - height / 2f;
            Drawing.DrawLine(pos.X, pos.Y - 1, pos.X + width, pos.Y - 1, thickness, color);
            Drawing.DrawLine(pos.X, pos.Y + height, pos.X + width, pos.Y + height, thickness, color);

            Drawing.DrawLine(pos.X, pos.Y, pos.X, pos.Y + height, thickness, color);
            Drawing.DrawLine(pos.X + width, pos.Y, pos.X + width, pos.Y + height, thickness, color);
        }

        public static void RectangleFilled(Vector2 pos, int width, int height, Color color)
        {
            pos.Y = pos.Y - height / 2f;
            Drawing.DrawLine(pos.X, pos.Y, pos.X + width, pos.Y, height, color);
        }

        public static void Line(Vector2 pos, int width, int height, Color color)
        {
            Drawing.DrawLine(pos, new Vector2(pos.X, pos.Y + height), width, color);
        }
    }
}