#region

using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace Leblanc.Common
{
    public static class CommonFontExtension
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
                    null, text, (int)(position.X + 1 - measure.Width * 0.5f),
                    (int)(position.Y + 1 - measure.Height * 0.5f), Color.Black);
                font.DrawText(
                    null, text, (int)(position.X - 1 - measure.Width * 0.5f),
                    (int)(position.Y - 1 - measure.Height * 0.5f), Color.Black);
                font.DrawText(
                    null, text, (int)(position.X + 1 - measure.Width * 0.5f),
                    (int)(position.Y - measure.Height * 0.5f), Color.Black);
                font.DrawText(
                    null, text, (int)(position.X - 1 - measure.Width * 0.5f),
                    (int)(position.Y - measure.Height * 0.5f), Color.Black);
            }
            font.DrawText(
                null, text, (int)(position.X - measure.Width * 0.5f), (int)(position.Y - measure.Height * 0.5f), color);
        }

        public static void DrawTextCentered(this Font font, string text, int x, int y, Color color)
        {
            DrawTextCentered(font, text, new Vector2(x, y), color);
        }

        public static void DrawTextLeft(this Font font, string text, Vector2 position, Color color)
        {
            var measure = GetMeasured(font, text);
            font.DrawText(
                null, text, (int)(position.X - measure.Width), (int)(position.Y - measure.Height * 0.5f), color);
        }

        public static void DrawTextLeft(this Font font, string text, int x, int y, Color color)
        {
            DrawTextLeft(font, text, new Vector2(x, y), color);
        }

        public static void DrawTextRight(this Font font, string text, Vector2 position, Color color)
        {
            var measure = GetMeasured(font, text);
            font.DrawText(
                null, text, (int)(position.X + measure.Width), (int)(position.Y - measure.Height * 0.5f), color);
        }

        public static void DrawTextRight(this Font font, string text, int x, int y, Color color)
        {
            DrawTextRight(font, text, new Vector2(x, y), color);
        }
    }
}