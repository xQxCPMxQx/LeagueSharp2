using LeagueSharp;
using LeagueSharp.Common;

namespace Mordekaiser
{
    internal class DamageCalc
    {
        public static float QDamage
        {
            get
            {
                var playerAd = Utils.Player.Self.TotalAttackDamage;
                var playerAp = Utils.Player.Self.TotalMagicalDamage;
                var bonusAd = playerAd * new[] { 0.25, 0.263, 0.275, 0.288, 0.3 }[Spells.Q.Level - 1];
                var bonusAp = playerAp * 0.2f;
                var totalBonusDamage = bonusAd + bonusAp + new[] { 4, 8, 12, 16, 20 }[Spells.Q.Level - 1];
                var multiplierPerLevel = new[] { 2, 2.25, 2.50, 2.75, 3 };
                var y = (float)(playerAd * multiplierPerLevel[Spells.Q.Level - 1] * 2);
                var totalQDamage = playerAd + y + totalBonusDamage;

                return (float)totalQDamage;
            }
        }

        public static float WDamage 
        {
            get
            {
                return new[] { 140, 180, 220, 260, 300 }[Spells.W.Level - 1] + new[] { 50, 70, 90, 110, 130 }[Spells.W.Level - 1];
            }
        }
        public static float EDamage
        {
            get
            {
                return
                    (float)
                    (new[] { 35, 65, 95, 125, 155 }[Spells.E.Level - 1] + Utils.Player.Self.TotalAttackDamage * 0.6
                     + Utils.Player.Self.TotalMagicalDamage * 0.6);

            }
        }

        public float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (Spells.Q.IsReady() && Menu.MenuDrawings.Item("Draw.Calc.Q").GetValue<bool>())
            {
                fComboDamage += QDamage * (100 / (100 + t.Armor));
            }

            if (Spells.W.IsReady() && Menu.MenuDrawings.Item("Draw.Calc.W").GetValue<bool>())
            {
                fComboDamage += WDamage * (100 / (100 + t.SpellBlock));
            }

            if (Spells.E.IsReady() && Menu.MenuDrawings.Item("Draw.Calc.E").GetValue<bool>())
            {
                fComboDamage += WDamage * (100 / (100 + t.Armor));
            }

            if (Spells.R.IsReady() && Menu.MenuDrawings.Item("Draw.Calc.R").GetValue<bool>())
            {
                fComboDamage += (float)ObjectManager.Player.GetSpellDamage(t, SpellSlot.R)* (100 / (100 + t.SpellBlock));
            }

            if (PlayerSpells.IgniteSlot != SpellSlot.Unknown && Menu.MenuDrawings.Item("Draw.Calc.I").GetValue<bool>()
                && Utils.Player.Self.Spellbook.CanUseSpell(PlayerSpells.IgniteSlot) == SpellState.Ready)
            {
                fComboDamage += (float)Utils.Player.Self.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);
            }

            return fComboDamage;
        }
    }
}
