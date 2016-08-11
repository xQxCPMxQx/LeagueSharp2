using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Marksman.Utils
{

    public static class CData
    {
        public enum ManaPercent
        {
            Harass,
            Lane,
            Jungle
        }

        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public static float RealAaRange(this Obj_AI_Hero t)
        {
            return t.AttackRange;
        }
        public static bool IsManaLowFor(this Obj_AI_Hero t, ManaPercent manaPercent = ManaPercent.Lane)
        {
            switch (manaPercent)
            {
                case ManaPercent.Lane:
                    return t.ManaPercent < Program.Config.Item("Lane.Mana").GetValue<Slider>().Value;

                case ManaPercent.Jungle:
                    return t.ManaPercent < Program.Config.Item("Jungle.Mana").GetValue<Slider>().Value;

                default:
                    return t.ManaPercent < Program.Config.Item("Lane.Mana").GetValue<Slider>().Value;

            }
        }

        public static SpellSlot IgniteSlot = ObjectManager.Player.GetSpellSlot("summonerdot");
        public static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0f;

            if (IgniteSlot != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot)
                == SpellState.Ready && ObjectManager.Player.Distance(t) < 550)
            {
                fComboDamage += (float)ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);
            }

            if (LeagueSharp.Common.Items.CanUseItem(3144) && ObjectManager.Player.Distance(t) < 550)
            {
                fComboDamage += (float)ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);
            }

            if (LeagueSharp.Common.Items.CanUseItem(3153) && ObjectManager.Player.Distance(t) < 550)
            {
                fComboDamage += (float)ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);
            }

            return fComboDamage;
        }
    }
}
