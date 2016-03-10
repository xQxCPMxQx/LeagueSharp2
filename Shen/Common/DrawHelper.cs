using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace Shen.Common
{
    internal class MobManager
    {
        public enum MobTypes
        {
            All,
            BigBoys
        }

        public static Obj_AI_Base GetMobs(float spellRange, MobTypes mobTypes = MobTypes.All, int minMobCount = 1)
        {
            List<Obj_AI_Base> mobs = MinionManager.GetMinions(
                spellRange + 200,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs == null) return null;

            if (mobTypes == MobTypes.BigBoys)
            {
                Obj_AI_Base oMob = (from fMobs in mobs
                                    from fBigBoys in
                                        new[]
                                            {
                                                "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red",
                                                "SRU_Krug", "SRU_Dragon", "SRU_Baron", "Sru_Crab"
                                            }
                                    where fBigBoys == fMobs.SkinName
                                    select fMobs).FirstOrDefault();

                if (oMob != null)
                {
                    if (oMob.IsValidTarget(spellRange))
                    {
                        return oMob;
                    }
                }
            }
            else if (mobs.Count >= minMobCount)
            {
                return mobs[0];
            }

            return null;
        }
    }
    internal static class BuffManager
    {
        public static bool HasSheenBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.Name.ToLower() == "sheen");
        }
        public static bool HasPassive(this Obj_AI_Base obj)
        {
            return obj.PassiveCooldownEndTime - (Game.Time - 15.5) <= 0;
        }

        public static bool HasPassiveShield(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "shenpassiveshield");
        }

    }

    internal class DrawHelper
    {
        public static string MenuTab => "    ";

        public static Font Text = new Font(Drawing.Direct3DDevice,
            new FontDescription
            {
                FaceName = "Tahoma",
                Height = 13,
                OutputPrecision = FontPrecision.Default,
                Quality = FontQuality.ClearTypeNatural
            });

        public static Font TextBold = TextBold = new Font(Drawing.Direct3DDevice,
            new FontDescription
            {
                FaceName = "Tahoma",
                Height = 13,
                Weight = FontWeight.Bold,
                OutputPrecision = FontPrecision.Default,
                Quality = FontQuality.ClearTypeNatural
            });

        public static Font TextWarning =
            TextWarning = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Malgun Gothic",
                    Height = 62,
                    Weight = FontWeight.Bold,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural
                });


        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }
    }

    public static class Champion
    {
        public static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0d;

            if (Shen.Champion.PlayerSpells.E.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);

            if (Common.SummonerManager.IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.IgniteSlot) == SpellState.Ready)
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            return (float)fComboDamage;
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
                case SpellSlot.Q: { return SharpDX.Color.LightSalmon; }

                case SpellSlot.W: { return SharpDX.Color.DarkSeaGreen; }

                case SpellSlot.E: { return SharpDX.Color.Aqua; }

                case SpellSlot.R: { return SharpDX.Color.Yellow; }
            }

            return SharpDX.Color.Wheat;
        }

        public static void DrawRange(this Spell spell, System.Drawing.Color color, bool draw = true, bool checkCoolDown = false)
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