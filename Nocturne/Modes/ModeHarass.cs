using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Nocturne.Modes
{
    internal static class ModeHarass
    {
        public static Menu LocalMenu { get; private set; }

        public static void Initialize()
        {
            LocalMenu = new Menu("Harass", "Harass");
            {
                LocalMenu.AddItem(new MenuItem("Harass.UseQ", "Q:").SetValue(new StringList(new[] {"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
            }
            PlayerMenu.MenuConfig.AddSubMenu(LocalMenu);

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (PlayerMenu.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed)
            {
                return;
            }
            
            Execute();
            ExecuteToggle();
        }

        private static void ExecuteToggle()
        {
            if (!PlayerMenu.MenuKeys.Item("Key.HarassToggle").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (ObjectManager.Player.ManaPercent < ManaManager.HarassMinManaPercent)
            {
                return;
            }


            var t = TargetSelector.GetTarget(PlayerSpells.Q.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
            {
                return;
            }

            PlayerSpells.Q.ModeCast(t);
        }

        private static void Execute()
        {
            var useQ = LocalMenu.Item("Harass.UseQ").GetValue<StringList>().SelectedIndex;
            if (useQ == 0)
            {
                return;
            }

            if (ObjectManager.Player.ManaPercent < ManaManager.HarassMinManaPercent)
            {
                return;
            }
                var t = TargetSelector.GetTarget(PlayerSpells.Q.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
            {
                return;
            }

            if (PlayerSpells.Q.IsReady() && ObjectManager.Player.Distance(t.ServerPosition) <= PlayerSpells.Q.Range)
            {
                PlayerSpells.Q.ModeCast(t);
            }
        }
    }
}
