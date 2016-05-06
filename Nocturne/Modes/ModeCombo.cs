using System;
using System.Drawing;

using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Champion;
using Nocturne.Common;
using Color = SharpDX.Color;

namespace Nocturne.Modes
{
    internal static class ModeCombo
    {
        public static Menu MenuLocal { get; private set; }
        private static Spell Q => PlayerSpells.Q;
        private static Spell W => PlayerSpells.W;
        private static Spell E => PlayerSpells.E;
        private static Spell R => PlayerSpells.R;

        public static void Init()
        {
            MenuLocal = new Menu("Combo", "Combo").SetFontStyle(FontStyle.Regular, Color.Aqua);
            MenuLocal.AddItem(new MenuItem("Combo.Q", "Q:").SetValue(true).SetFontStyle(FontStyle.Regular, Q.MenuColor()));
            MenuLocal.AddItem(new MenuItem("Combo.E", "E:").SetValue(true).SetFontStyle(FontStyle.Regular, E.MenuColor()));
            MenuLocal.AddItem(
                new MenuItem("Combo.R", "R:").SetValue(
                    new StringList(new[] {"Off", "On: If I'm in risk", "On: If only can kill with R", "Both"}, 0))
                    .SetFontStyle(FontStyle.Regular, PlayerSpells.R.MenuColor()));

            ModeConfig.MenuConfig.AddSubMenu(MenuLocal);
            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            ExecuteCombo();
            ExecuteComboR();
        }

        private static void ExecuteComboR()
        {
            if (!R.IsReady())
            {
                return;
            }

            var comboRMode = MenuLocal.Item("Combo.R").GetValue<StringList>().SelectedIndex;
            if (comboRMode == 0)
            {
                return;
            }

            var t = CommonTargetSelector.GetTarget(R.Range);
            if (!t.IsValidTarget())
            {
                return;
            }

            var canEffectDamage = 0d;
            canEffectDamage += Q.IsReady() && t.IsValidTarget(Q.Range)? Q.GetDamage(t) : 0;
            canEffectDamage += E.IsReady() && t.IsValidTarget(E.Range) ? E.GetDamage(t) + ObjectManager.Player.TotalAttackDamage * 3 : 0;

            if (comboRMode == 1 || comboRMode == 3)
            {
                if (t.Health > ObjectManager.Player.Health 
                    && ObjectManager.Player.HealthPercent < t.HealthPercent 
                    && ObjectManager.Player.Health < t.TotalAttackDamage * 5 
                    && t.Health < R.GetDamage(t) + ObjectManager.Player.TotalAttackDamage)
                {
                    R.Cast();
                    R.CastOnUnit(t);
                }
            }

            if (comboRMode == 2 || comboRMode == 3)
            {
                if (!t.IsValidTarget(Q.Range) && t.Health < R.GetDamage(t) + ObjectManager.Player.TotalAttackDamage + (Q.IsReady() ? Q.GetDamage(t):0))
                {
                    R.Cast();
                    R.CastOnUnit(t);
                }
            }
        }

        private static void ExecuteCombo()
        {
            var t = CommonTargetSelector.GetTarget(R.Range);

            if (!t.IsValidTarget())
            {
                return;
            }

            if (MenuLocal.Item("Combo.Q").GetValue<bool>() && Q.IsReady() && t.IsValidTarget(Q.Range) &&
                !ObjectManager.Player.HasNocturneParanoia())
            {
                Q.ModeCast(t);
            }

            if (MenuLocal.Item("Combo.Q").GetValue<bool>() && Q.IsReady() && t.IsValidTarget(Q.Range) &&
                t.HasNocturneUnspeakableHorror())
            {
                Q.ModeCast(t);
            }

            if (MenuLocal.Item("Combo.E").GetValue<bool>() && E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.CastOnUnit(t);
            }
        }
    }
}
