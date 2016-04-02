#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 MDrawing.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using SFXUtility.Library.Logger;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Classes
{
    internal class MDrawing
    {
        private static readonly Dictionary<int, Font> Fonts = new Dictionary<int, Font>();
        private static readonly HashSet<Line> Lines = new HashSet<Line>();
        private static Sprite _sprite;
        private static bool _unloaded;

        static MDrawing()
        {
            try
            {
                Drawing.OnPreReset += OnDrawingPreReset;
                Drawing.OnPostReset += OnDrawingPostReset;
                Global.SFX.OnUnload += OnUnload;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnUnload(object sender, UnloadEventArgs unloadEventArgs)
        {
            try
            {
                _unloaded = true;

                if (_sprite != null && !_sprite.IsDisposed)
                {
                    _sprite.Dispose();
                }

                foreach (var font in Fonts.Where(font => font.Value != null && !font.Value.IsDisposed))
                {
                    font.Value.Dispose();
                }

                foreach (var line in Lines.Where(line => line != null && !line.IsDisposed))
                {
                    line.Dispose();
                }

                Drawing.OnPreReset -= OnDrawingPreReset;
                Drawing.OnPostReset -= OnDrawingPostReset;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                if (_unloaded)
                {
                    return;
                }

                if (_sprite != null && !_sprite.IsDisposed)
                {
                    _sprite.OnResetDevice();
                }

                foreach (var font in Fonts.Where(font => font.Value != null && !font.Value.IsDisposed))
                {
                    font.Value.OnResetDevice();
                }

                foreach (var line in Lines.Where(line => line != null && !line.IsDisposed))
                {
                    line.OnResetDevice();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                if (_unloaded)
                {
                    return;
                }

                if (_sprite != null && !_sprite.IsDisposed)
                {
                    _sprite.OnLostDevice();
                }

                foreach (var font in Fonts.Where(font => font.Value != null && !font.Value.IsDisposed))
                {
                    font.Value.OnLostDevice();
                }

                foreach (var line in Lines.Where(line => line != null && !line.IsDisposed))
                {
                    line.OnLostDevice();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static Font GetFont(int fontSize)
        {
            Font font = null;
            try
            {
                if (!Fonts.TryGetValue(fontSize, out font))
                {
                    font = new Font(
                        Drawing.Direct3DDevice,
                        new FontDescription
                        {
                            FaceName = Global.DefaultFont,
                            Height = fontSize,
                            OutputPrecision = FontPrecision.Default,
                            Quality = FontQuality.Default
                        });
                    Fonts[fontSize] = font;
                }
                else
                {
                    if (!_unloaded && (font == null || font.IsDisposed))
                    {
                        Fonts.Remove(fontSize);
                        GetFont(fontSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return font;
        }

        public static Line GetLine(int width = -1)
        {
            Line line = null;
            try
            {
                line = new Line(Drawing.Direct3DDevice);
                if (width >= 0)
                {
                    line.Width = width;
                }
                Lines.Add(line);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return line;
        }

        public static Sprite GetSprite()
        {
            try
            {
                if (!_unloaded && (_sprite == null || _sprite.IsDisposed))
                {
                    _sprite = new Sprite(Drawing.Direct3DDevice);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return _sprite;
        }
    }
}