using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Resources;
using LeagueSharp;
using LeagueSharp.Common;
using Leblanc.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using PrimitiveType = SharpDX.Direct3D9.PrimitiveType;
using Rectangle = SharpDX.Rectangle;
using Texture = SharpDX.Direct3D9.Texture;

namespace Leblanc
{
    static class CommonL
    {
        public static bool IsOnScreen(Vector3 vector)
        {
            Vector2 screen = Drawing.WorldToScreen(vector);
            if (screen[0] < 0 || screen[0] > Drawing.Width || screen[1] < 0 || screen[1] > Drawing.Height)
                return false;
            return true;
        }

        public static bool IsOnScreen(Vector2 vector)
        {
            Vector2 screen = vector;
            if (screen[0] < 0 || screen[0] > Drawing.Width || screen[1] < 0 || screen[1] > Drawing.Height)
                return false;
            return true;
        }

        public static Size ScaleSize(this Size size, float scale, Vector2 mainPos = default(Vector2))
        {
            size.Height = (int)(((size.Height - mainPos.Y) * scale) + mainPos.Y);
            size.Width = (int)(((size.Width - mainPos.X) * scale) + mainPos.X);
            return size;
        }

        public static bool IsInside(Vector2 mousePos, Size windowPos, int width, int height)
        {
            return Utils.IsUnderRectangle(mousePos, windowPos.Width, windowPos.Height, width, height);
        }
    }
    
    static class SpriteHelper
    {
        static readonly Dictionary<String, byte[]> MyResources = new Dictionary<String, byte[]>();

        //private static List<SpriteRef> Sprites = new List<SpriteRef>();
        static SpriteHelper()
        {
            ResourceSet resourceSet = Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                MyResources.Add(entry.Key.ToString().ToLower(), (byte[])entry.Value);
            }
        }

        public enum TextureType
        {
            Default
        }

        public static void LoadTexture(String name, ref Texture texture, TextureType type)
        {
            Game.PrintChat(name.ToLower());
            if ((type == TextureType.Default) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower()]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
        }
    }

    static class DirectXDrawer
    {

        public struct PositionColored
        {
            public static readonly VertexFormat FVF = VertexFormat.Position | VertexFormat.Diffuse;
            public static readonly int Stride = Vector3.SizeInBytes + sizeof(int);

            public Vector3 Position;
            public int Color;

            public PositionColored(Vector3 pos, int col)
            {
                Position = pos;
                Color = col;
            }
        }

        private static void InternalRender(Vector3 target)
        {
            Drawing.Direct3DDevice.VertexShader = null;
            Drawing.Direct3DDevice.PixelShader = null;
            Drawing.Direct3DDevice.SetRenderState(RenderState.AlphaBlendEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
            Drawing.Direct3DDevice.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            Drawing.Direct3DDevice.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            Drawing.Direct3DDevice.SetRenderState(RenderState.Lighting, 0);
            Drawing.Direct3DDevice.SetRenderState(RenderState.ZEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.AntialiasedLineEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.Clipping, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.EnableAdaptiveTessellation, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.MultisampleAntialias, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.ShadeMode, ShadeMode.Gouraud);
            Drawing.Direct3DDevice.SetTexture(0, null);
            Drawing.Direct3DDevice.SetRenderState(RenderState.CullMode, Cull.None);
        }

        public static void DrawLine(Vector3 from, Vector3 to, System.Drawing.Color color)
        {
            var vertices = new PositionColored[2];
            vertices[0] = new PositionColored(Vector3.Zero, color.ToArgb());
            from = from.SwitchYZ();
            to = to.SwitchYZ();
            vertices[1] = new PositionColored(to - from, color.ToArgb());

            InternalRender(from);

            Drawing.Direct3DDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices.Length / 2, vertices);
        }

        public static void DrawLine(Line line, Vector3 from, Vector3 to, ColorBGRA color, Size size = default(Size), float[] scale = null, float rotation = 0.0f)
        {
            if (line != null)
            {
                from = from.SwitchYZ();
                to = to.SwitchYZ();
                Matrix nMatrix = (scale != null ? Matrix.Scaling(scale[0], scale[1], 0) : Matrix.Scaling(1)) *
                                 Matrix.RotationZ(rotation) * Matrix.Translation(from);
                Vector3[] vec = { from, to };
                line.DrawTransform(vec, nMatrix, color);
            }

        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, float[] scale = null,
            float rotation = 0.0f)
        {
            DrawSprite(sprite, texture, size, Color.White, scale, rotation);
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, Color color, float[] scale = null,
            float rotation = 0.0f)
        {
            if (sprite != null && !sprite.IsDisposed && texture != null && !texture.IsDisposed)
            {
                Matrix matrix = sprite.Transform;
                Matrix nMatrix = (scale != null ? Matrix.Scaling(scale[0], scale[1], 0) : Matrix.Scaling(1)) *
                                 Matrix.RotationZ(rotation) * Matrix.Translation(size.Width, size.Height, 0);
                sprite.Transform = nMatrix;
                Matrix mT = Drawing.Direct3DDevice.GetTransform(TransformState.World);

                if (CommonL.IsOnScreen(new Vector2(size.Width, size.Height)))
                    sprite.Draw(texture, color);
                sprite.Transform = matrix;
            }
        }

        public static void DrawTransformSprite(Sprite sprite, Texture texture, Color color, Size size, float[] scale,
            float rotation, Rectangle? spriteResize)
        {
            if (sprite != null && texture != null)
            {
                Matrix matrix = sprite.Transform;
                Matrix nMatrix = Matrix.Scaling(scale[0], scale[1], 0) * Matrix.RotationZ(rotation) *
                                 Matrix.Translation(size.Width, size.Height, 0);
                sprite.Transform = nMatrix;
                sprite.Draw(texture, color);
                sprite.Transform = matrix;
            }
        }

        public static void DrawTransformedSprite(Sprite sprite, Texture texture, Rectangle spriteResize, Color color)
        {
            if (sprite != null && texture != null)
            {
                sprite.Draw(texture, color);
            }
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, Color color, Rectangle? spriteResize)
        {
            if (sprite != null && texture != null)
            {
                sprite.Draw(texture, color, spriteResize, new Vector3(size.Width, size.Height, 0));
            }
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, Color color)
        {
            if (sprite != null && texture != null)
            {
                DrawSprite(sprite, texture, size, color: color, spriteResize: null);
            }
        }
    }
}