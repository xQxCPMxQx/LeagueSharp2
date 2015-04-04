using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using LeagueSharp;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;

namespace KaiHelper
{
    public static class Helper
    {
        public static void DrawText(Font font, String text, int posX, int posY, Color color)
        {
            Rectangle rec = font.MeasureText(null, text, FontDrawFlags.Center);
            font.DrawText(null, text, posX + 1 + rec.X, posY + 1, Color.Black);
            font.DrawText(null, text, posX + rec.X, posY + 1, Color.Black);
            font.DrawText(null, text, posX - 1 + rec.X, posY - 1, Color.Black);
            font.DrawText(null, text, posX + rec.X, posY - 1, Color.Black);
            font.DrawText(null, text, posX + rec.X, posY, color);
        }

        public static Bitmap CropCircleImage(Bitmap image)
        {
            var cropRect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            using (Bitmap cropImage = image.Clone(cropRect, image.PixelFormat))
            {
                using (var tb = new TextureBrush(cropImage))
                {
                    var target = new Bitmap(cropRect.Width, cropRect.Height);
                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.FillEllipse(tb, new System.Drawing.Rectangle(0, 0, cropRect.Width, cropRect.Height));
                        var p = new Pen(System.Drawing.Color.Red, 8) { Alignment = PenAlignment.Inset };
                        g.DrawEllipse(p, 0, 0, cropRect.Width, cropRect.Width);
                        return target;
                    }
                }
            }
        }

        /// <summary>
        ///     http://www.codeproject.com/Tips/201129/Change-Opacity-of-Image-in-C
        /// </summary>
        /// <returns></returns>
        public static Bitmap ChangeOpacity(Bitmap image, float opacity)
        {
            var bmp = new Bitmap(image.Width, image.Height);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                var matrix = new ColorMatrix { Matrix33 = opacity };
                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                gfx.DrawImage(
                    image, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height,
                    GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        public static string FormatTime(double time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);
            if (t.Minutes > 0)
            {
                return string.Format("{0:D1}:{1:D2}", t.Minutes, t.Seconds);
            }
            return string.Format("{0:D}", t.Seconds);
        }

        public static Stream Download(string url)
        {
            WebRequest req = WebRequest.Create(url);
            WebResponse response = req.GetResponse();
            return response.GetResponseStream();
        }

        public static string ReadFile(string path)
        {
            using (var sr = new StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }

        private static string GetLastVersion(string assemblyName)
        {
            WebRequest request =
                WebRequest.Create(
                    String.Format(
                        "https://raw.githubusercontent.com/kaigan05/LeagueSharp/master/{0}/Properties/AssemblyInfo.cs",
                        assemblyName));
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            string version;
            using (var sr = new StreamReader(data))
            {
                version = sr.ReadToEnd();
            }
            const string pattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}";
            return new Regex(pattern).Match(version).Groups[0].Value;
        }

        public static bool UnitTrenManHinh(Obj_AI_Base o)
        {
            Vector2 viTri = Drawing.WorldToScreen(o.Position);
            return viTri.X > 0 && viTri.X < Drawing.Width && viTri.Y > 0 && viTri.Y < Drawing.Height;
        }
        public static float PredictedHealth(this Obj_AI_Hero champion, int secondTime)
        {
            var predictedhealth = champion.Health + champion.HPRegenRate * secondTime;
            return predictedhealth > champion.MaxHealth ? champion.MaxHealth : predictedhealth;
        }
        public static float PredictedMana(this Obj_AI_Hero champion, int secondTime)
        {
            var predictedMana = champion.Mana + champion.PARRegenRate * secondTime;
            return predictedMana > champion.MaxMana ? champion.MaxMana : predictedMana;
        }
        public static int GetPercent(float cur, float max)
        {
            return (int)((cur * 1.0) / max * 100);
        }
        public static bool HasNewVersion(string assemblyName)
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString() != GetLastVersion(assemblyName);
        }
    }
}