using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace Leblanc.Common
{

    public static class CommonHelper
    {
        public static Font Text, TextLittle;
        public static string Tab => "       ";

        public static void Init()
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

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;

        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            Text.Dispose();
            TextLittle.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            Text.OnResetDevice();
            TextLittle.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            Text.OnLostDevice();
            TextLittle.OnLostDevice();
        }

        public static string FormatTime(double time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);
            if (t.Minutes > 0)
            {
                return $"{t.Minutes:D1}:{t.Seconds:D2}";
            }
            return $"{t.Seconds:D}";
        }

        public static Vector3 CenterOfVectors(Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            if (vectors == null || vectors.Length == 0)
                return sum;

            sum = vectors.Aggregate(sum, (current, vec) => current + vec);
            return sum / vectors.Length;
        }

        public enum SpellRName
        {
            None,
            R2xQ,
            R2xW,
            R2xE
        }

        public static SpellRName SpellRStatus
        {
            get
            {
                if (!Champion.PlayerSpells.R.IsReady())
                {
                    return SpellRName.None;
                }

                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Equals("Leblancchaosorbm", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SpellRName.R2xQ;
                }

                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Equals("Leblancslidem", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SpellRName.R2xW;
                }

                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Equals("Leblancsoulshacklem", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SpellRName.R2xE;
                }

                return SpellRName.None;
            }
        }

        public static bool StillJumped(this Spell spell)
        {
            if (spell == Champion.PlayerSpells.W)
            {
                return ObjectManager.Player.HasBuff("LeblancSlide");
                return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W)
                    .Name.Equals("Leblancslidereturn", StringComparison.InvariantCultureIgnoreCase);
            }

            if (spell == Champion.PlayerSpells.W2)
            {
                return ObjectManager.Player.HasBuff("LeblancSlideM");
            }

            return false;
            //    return spell == Champion.PlayerSpells.W2 &&
            //           ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R)
            //               .Name.ToLower()
            //               .Equals("Leblancslidereturnm", StringComparison.InvariantCultureIgnoreCase);
        }
        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }

        public static bool ShouldCastSpell(Obj_AI_Base t)
        {
            return !t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(t) + 65) || !ObjectManager.Player.HasSheenBuff();
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