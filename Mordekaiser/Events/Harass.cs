using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace Mordekaiser.Events
{
    internal class Harass
    {
        public Harass()
        {
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Utils.Player.Self.IsDead)
            {
                return;
            }

            if (Menu.MenuKeys.Item("Keys.Harass").GetValue<KeyBind>().Active ||
                (Menu.MenuE.Item("UseE.Toggle").GetValue<KeyBind>().Active && !Utils.Player.Self.IsRecalling()))
            {
                ExecuteE();
            }
        }

        private static void ExecuteE()
        {
            if (Utils.Player.Self.HealthPercent <= Menu.MenuE.Item("UseE.Harass.MinHeal").GetValue<Slider>().Value)
            {
                return;
            }

            if (!Menu.MenuE.Item("UseE.Harass").GetValue<bool>())
                return;

            if (!Spells.E.IsReady())
            {
                return;
            }

            var t = TargetSelector.GetTarget(Spells.E.Range, TargetSelector.DamageType.Magical);

            if (!t.IsValidTarget())
            {
                return;
            }

            Spells.E.Cast(t);
        }
    }
}