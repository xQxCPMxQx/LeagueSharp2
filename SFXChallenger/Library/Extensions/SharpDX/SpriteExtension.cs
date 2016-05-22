#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SpriteExtension.cs is part of SFXLibrary.

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

using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXChallenger.Library.Extensions.SharpDX
{
    public static class SpriteExtension
    {
        public static void DrawCentered(this Sprite sprite,
            Texture texture,
            Vector2 position,
            Rectangle? rectangle = null)
        {
            var desc = texture.GetLevelDescription(0);
            sprite.Draw(
                texture, new ColorBGRA(255, 255, 255, 255), rectangle,
                new Vector3(-(position.X - desc.Width / 2f), -(position.Y - desc.Height / 2f), 0));
        }

        public static void DrawCentered(this Sprite sprite, Texture texture, int x, int y, Rectangle? rectangle = null)
        {
            DrawCentered(sprite, texture, new Vector2(x, y), rectangle);
        }
    }
}