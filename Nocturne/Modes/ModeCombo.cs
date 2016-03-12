using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
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
        public static void Initialize()
        {
            MenuLocal = new Menu("Combo", "Combo").SetFontStyle(FontStyle.Regular, Color.Aqua);
            MenuLocal.AddItem(new MenuItem("Combo.Q", "Q:").SetValue(true).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
            MenuLocal.AddItem(new MenuItem("Combo.E", "E:").SetValue(true).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));

            PlayerMenu.MenuConfig.AddSubMenu(MenuLocal);
            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (PlayerMenu.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            ExecuteCombo();
        }

        private static void ExecuteCombo()
        {
            var t = AssassinManager.GetTarget(R.Range);

            if (!t.IsValidTarget())
            {
                return;
            }

            if (MenuLocal.Item("Combo.Q").GetValue<bool>() && Q.IsReady() && t.IsValidTarget(Q.Range) &&
                !ObjectManager.Player.HasNocturneParanoia())
            {
                Q.Cast(t);
            }

            if (MenuLocal.Item("Combo.E").GetValue<bool>() && E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.CastOnUnit(t);
            }
        }


    }
}
