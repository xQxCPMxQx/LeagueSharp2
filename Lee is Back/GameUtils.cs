using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace LeeSin
{
    public class Captions
    {
        public static Obj_AI_Hero Player => ObjectManager.Player;
        public static string MenuTab => "    ";
        public static string ChampionName => "Leblanc";
        public static string MinManaPercent => "Min. Mana Per. %";
        public static string PermaShowTag => ObjectManager.Player.ChampionName + " | ";
    }

    public static class GameUtils
    {

    }

    public static class JungleUtils
    {

        public enum MobTypes
        {
            All,
            BigBoys
        }

        public static bool In<T>(T source, params T[] list)
        {
            return list.Equals(source);
        }

        public static Obj_AI_Base GetMobs(float spellRange, MobTypes mobTypes = MobTypes.All, int minMobCount = 1)
        {
            List<Obj_AI_Base> mobs = MinionManager.GetMinions(spellRange + 200, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

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
    public static class MenuUtils
    {
        public static string[] GetMinionCountString(int maxMinionCount = 3, string objectName = "Minion", bool addJustBigBoys = false)
        {
            var arrayCount = maxMinionCount + (addJustBigBoys ? 2 : 1);
            string[] str = new string[arrayCount];
            {
                str[0] = "Off";
                if (addJustBigBoys)
                {
                    str[1] = "Just for Big Boys";
                }

                for (int i = 1; i < arrayCount - (addJustBigBoys ? 1 : 0); i++)
                {
                    str[(int)i + (addJustBigBoys ? 1 : 0)] = objectName + " Count >= " + i;
                }
                return str;
            }
        }
        public static MenuItem GetMinionCountStringList(string menuItemName, string menuItemDescription, int maxMinionCount = 3, string description = "Minion Count >=", bool addJustBigBoys = false)
        {
            var arrayCount = maxMinionCount + (addJustBigBoys ? 2 : 1);
            string[] str = new string[arrayCount];
            {
                str[0] = "Off";
                if (addJustBigBoys)
                {
                    str[1] = "Just for Big Boys";
                }

                for (int i = 1; i < arrayCount - (addJustBigBoys ? 1 : 0); i++)
                {
                    str[(int)i + (addJustBigBoys ? 1 : 0)] = description + " " + i;
                }

                var aMenu = new MenuItem(menuItemName, menuItemDescription).SetValue(new StringList(str));
                return aMenu;
            }
        }

        public static StringList GetMinionCountStringList(int maxMinionCount = 3, string description = "Minion Count >=", bool addJustBigBoys = false)
        {
            var arrayCount = maxMinionCount + (addJustBigBoys ? 2 : 1);
            string[] str = new string[arrayCount];
            {
                str[0] = "Off";
                if (addJustBigBoys)
                {
                    str[1] = "Just for Big Boys";
                }

                for (int i = 1; i < arrayCount - (addJustBigBoys ? 1 : 0); i++)
                {
                    str[(int)i + (addJustBigBoys ? 1 : 0)] = description + " " + i;
                }
                return new StringList(str);
            }
        }
    }
    public static class SpellUtilities
    {
        public static SharpDX.Color MenuColor(this Spell spell)
        {
            switch (spell.Slot)
            {
                case SpellSlot.Q:
                    {
                        return SharpDX.Color.Aqua;
                    }

                case SpellSlot.W:
                    {
                        return SharpDX.Color.DarkSeaGreen;
                    }

                case SpellSlot.E:
                    {
                        return SharpDX.Color.OrangeRed;
                    }

                case SpellSlot.R:
                    {
                        return SharpDX.Color.Yellow;
                    }
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
                    spell.IsReady() ? color : System.Drawing.Color.Gray, spell.IsReady() ? 5 : 1);
            }
            else
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, color, 1);
            }
        }
    }
}
