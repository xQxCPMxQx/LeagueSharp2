#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 FontExtension.cs is part of SFXLibrary.

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

using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXChallenger.Library.Extensions.SharpDX
{
    public static class FontExtension
    {
        private static readonly Dictionary<string, Rectangle> Measured = new Dictionary<string, Rectangle>();

        private static Rectangle GetMeasured(Font font, string text)
        {
            Rectangle rec;
            var key = font.Description.FaceName + font.Description.Width + font.Description.Height +
                      font.Description.Weight + text;
            if (!Measured.TryGetValue(key, out rec))
            {
                rec = font.MeasureText(null, text, FontDrawFlags.Center);
                Measured.Add(key, rec);
            }
            return rec;
        }

        public static void DrawTextCentered(this Font font,
            string text,
            Vector2 position,
            Color color,
            bool outline = false)
        {
            var measure = GetMeasured(font, text);
            if (outline)
            {
                font.DrawText(
                    null, text, (int) (position.X + 1 - measure.Width * 0.5f),
                    (int) (position.Y + 1 - measure.Height * 0.5f), Color.Black);
                font.DrawText(
                    null, text, (int) (position.X - 1 - measure.Width * 0.5f),
                    (int) (position.Y - 1 - measure.Height * 0.5f), Color.Black);
                font.DrawText(
                    null, text, (int) (position.X + 1 - measure.Width * 0.5f),
                    (int) (position.Y - measure.Height * 0.5f), Color.Black);
                font.DrawText(
                    null, text, (int) (position.X - 1 - measure.Width * 0.5f),
                    (int) (position.Y - measure.Height * 0.5f), Color.Black);
            }
            font.DrawText(
                null, text, (int) (position.X - measure.Width * 0.5f), (int) (position.Y - measure.Height * 0.5f), color);
        }

        public static void DrawTextCentered(this Font font, string text, int x, int y, Color color)
        {
            DrawTextCentered(font, text, new Vector2(x, y), color);
        }

        public static void DrawTextLeft(this Font font, string text, Vector2 position, Color color)
        {
            var measure = GetMeasured(font, text);
            font.DrawText(
                null, text, (int) (position.X - measure.Width), (int) (position.Y - measure.Height * 0.5f), color);
        }

        public static void DrawTextLeft(this Font font, string text, int x, int y, Color color)
        {
            DrawTextLeft(font, text, new Vector2(x, y), color);
        }

        public static void DrawTextRight(this Font font, string text, Vector2 position, Color color)
        {
            var measure = GetMeasured(font, text);
            font.DrawText(
                null, text, (int) (position.X + measure.Width), (int) (position.Y - measure.Height * 0.5f), color);
        }

        public static void DrawTextRight(this Font font, string text, int x, int y, Color color)
        {
            DrawTextRight(font, text, new Vector2(x, y), color);
        }
    }
}