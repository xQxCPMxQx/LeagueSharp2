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

        private static Spell Q => PlayerSpells.Q;
        private static Spell E => PlayerSpells.E;

        public static void Initialize()
        {
            LocalMenu = new Menu("Harass", "Harass");
            {
                LocalMenu.AddItem(new MenuItem("Harass.UseQ", "Q:").SetValue(new StringList(new[] {"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
                LocalMenu.AddItem(new MenuItem("Harass.ToggleQEP", "Toggle:").SetValue(new StringList(new[] { "Off", "Q", "E", "E + Q Combo", "E + Q + Passive AA Combo" }, 4)).SetFontStyle(FontStyle.Regular, PlayerSpells.W.MenuColor()));
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
            if (!PlayerMenu.MenuKeys.Item("Key.HarassToggle").GetValue<KeyBind>().Active || ObjectManager.Player.ManaPercent < ManaManager.HarassMinManaPercent)
            {
                return;
            }

            var modeToggle = LocalMenu.Item("Harass.ToggleQEP").GetValue<StringList>().SelectedIndex;
            switch (modeToggle)
            {
                case 1:
                {
                    if (Q.IsReady())
                    {
                        var t1 = AssassinManager.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                        if (t1.IsValidTarget(Q.Range))
                        {
                            Q.ModeCast(t1);
                        }
                    }
                    break;
                }

                case 2:
                {
                    if (E.IsReady())
                    {
                        var t1 = AssassinManager.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (t1.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(t1);
                        }
                    }
                    break;
                }

                case 3:
                    {
                        if (Q.IsReady() && E.IsReady())
                        {
                            var t1 = AssassinManager.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (t1.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(t1);

                                if (t1.HasNocturneUnspeakableHorror())
                                {
                                    Q.ModeCast(t1);
                                }
                            }
                        }
                        break;
                    }

                case 4:
                    {
                        if (Q.IsReady() && E.IsReady() && ObjectManager.Player.HasPassive())
                        {
                            var t1 = AssassinManager.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (t1.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(t1);

                                if (t1.HasNocturneUnspeakableHorror())
                                {
                                    Q.ModeCast(t1);
                                }
                            }
                        }
                        break;
                    }
            }
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
