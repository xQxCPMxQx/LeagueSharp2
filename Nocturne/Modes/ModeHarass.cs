using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Champion;
using Nocturne.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Nocturne.Modes
{
    internal static class ModeHarass
    {
        public static Menu MenuLocal { get; private set; }

        private static Spell Q => PlayerSpells.Q;
        private static Spell E => PlayerSpells.E;

        public static void Init()
        {
            MenuLocal = new Menu("Harass", "Harass");
            {
                MenuLocal.AddItem(new MenuItem("Harass.UseQ", "Q:").SetValue(new StringList(new[] {"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
                MenuLocal.AddItem(new MenuItem("Harass.ToggleQEP", "Toggle:").SetValue(new StringList(new[] { "Off", "Q", "E", "E + Q Combo", "E + Q + Passive AA Combo" }, 4)).SetFontStyle(FontStyle.Regular, PlayerSpells.W.MenuColor()));
            }
            ModeConfig.MenuConfig.AddSubMenu(MenuLocal);

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                ExecuteHarass();
            }
            
            if (ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                ExecuteToggle();
            }
        }

        private static void ExecuteToggle()
        {
            if (!ModeConfig.MenuKeys.Item("Key.HarassToggle").GetValue<KeyBind>().Active || ObjectManager.Player.ManaPercent < CommonManaManager.HarassMinManaPercent)
            {
                return;
            }

            var modeToggle = MenuLocal.Item("Harass.ToggleQEP").GetValue<StringList>().SelectedIndex;
            if (modeToggle == 0)
            {
                return;
            }

            var t = CommonTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            switch (modeToggle)
            {

                case 1:
                {
                    if (Q.IsReady())
                    {
                        if (t.IsValidTarget(Q.Range))
                        {
                            Q.ModeCast(t);
                        }
                    }
                    break;
                }

                case 2:
                {
                    if (E.IsReady())
                    {
                        if (t.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(t);
                        }
                    }
                    break;
                }

                case 3:
                    {
                        if (Q.IsReady() && E.IsReady())
                        {
                            if (t.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(t);
                                Q.ModeCast(t);
                            }
                        }
                        break;
                    }

                case 4:
                    {
                        if (Q.IsReady() && E.IsReady() && ObjectManager.Player.HasPassive())
                        {
                            if (t.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(t);
                                Q.ModeCast(t);
                            }
                        }
                        break;
                    }
            }
        }

        private static void ExecuteHarass()
        {
            var useQ = MenuLocal.Item("Harass.UseQ").GetValue<StringList>().SelectedIndex;
            if (useQ == 0)
            {
                return;
            }

            if (ObjectManager.Player.ManaPercent < CommonManaManager.HarassMinManaPercent)
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
