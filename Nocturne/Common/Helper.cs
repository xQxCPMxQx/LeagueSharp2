using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace Nocturne.Common
{

    public static class Helper
    {
        public static Font Text, TextLittle;
        public static string Tab => "       ";

        public static void Initialize()
        {
            Text = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 19,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            TextLittle = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });
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

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }

        public static bool HasSheenBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.Name.ToLower() == "sheen");
        }

        public static bool HasNocturneUnspeakableHorror(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "NocturneUnspeakableHorror");
        }

        public static bool HasPassive(this Obj_AI_Base obj)
        {
            return obj.PassiveCooldownEndTime - (Game.Time - 15.5) <= 0;
        }

        public static bool HasNocturneParanoia(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "NocturneParanoiaTarget");
        }

        public static bool HasBuffInst(this Obj_AI_Base obj, string buffName)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == buffName);
        }

        public static bool HasBlueBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "CrestoftheAncientGolem");
        }

        public static bool HasRedBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "BlessingoftheLizardElder");
        }
    }

    public static class Colors
    {
        public static SharpDX.Color SubMenu => SharpDX.Color.GreenYellow;
        public static SharpDX.Color ColorMana => SharpDX.Color.Aquamarine;
        public static SharpDX.Color ColorItems => SharpDX.Color.Cornsilk;
        public static SharpDX.Color ColorWarning => SharpDX.Color.IndianRed;
        public static SharpDX.Color ColorPermaShow => SharpDX.Color.Aqua;

        public static SharpDX.Color MenuColor(this Spell spell)
        {
            switch (spell.Slot)
            {
                case SpellSlot.Q:
                {
                    return SharpDX.Color.LightSalmon;
                }

                case SpellSlot.W:
                {
                    return SharpDX.Color.DarkSeaGreen;
                }

                case SpellSlot.E:
                {
                    return SharpDX.Color.Aqua;
                }

                case SpellSlot.R:
                {
                    return SharpDX.Color.Yellow;
                }
            }

            return SharpDX.Color.Wheat;
        }

        public static void DrawRange(this Spell spell, System.Drawing.Color color, bool draw = true,
            bool checkCoolDown = false)
        {
            if (!draw)
            {
                return;
            }

            if (checkCoolDown)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range,
                    spell.IsReady() ? color : System.Drawing.Color.Gray,
                    spell.IsReady() ? 5 : 1);
            }
            else
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, color, 1);
            }
        }
    }
}