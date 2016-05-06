using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Champion;

namespace Nocturne.Common
{
    internal class CommonMath
    {

        private static Spell Q => PlayerSpells.Q;
        private static Spell W => PlayerSpells.W;
        private static Spell E => PlayerSpells.E;
        private static Spell R => PlayerSpells.R;

        public static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0d;

            if (ObjectManager.Player.HasPassive())
            {
                fComboDamage += ObjectManager.Player.TotalAttackDamage * 1.2;
            }

            if (Q.IsReady())
            {
                fComboDamage += Q.GetDamage(t);
            }

            if (E.IsReady())
            {
                fComboDamage += E.GetDamage(t) + ObjectManager.Player.TotalAttackDamage * 2;
            }

            if (R.IsReady())
            {
                fComboDamage += R.GetDamage(t);
                fComboDamage += ObjectManager.Player.TotalAttackDamage * 2;
            }

            if (CommonItems.Youmuu.IsReady())
            {
                fComboDamage += ObjectManager.Player.TotalAttackDamage * 4;
            }

            if (Common.CommonSummoner.IgniteSlot != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(Common.CommonSummoner.IgniteSlot) == SpellState.Ready)
            {
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);
            }

            if (LeagueSharp.Common.Items.CanUseItem(3128))
            {
                fComboDamage += ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);
            }

            return (float)fComboDamage;
        }
    }
}
