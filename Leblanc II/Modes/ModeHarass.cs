using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Leblanc.Champion;
using Color = SharpDX.Color;
using Leblanc.Common;

namespace Leblanc.Modes
{

    internal class ModeHarass
    {
        public static Menu MenuLocal { get; private set; }
        public static Menu MenuToggle { get; private set; }
        private static Spell Q => Champion.PlayerSpells.Q;
        private static Spell W => Champion.PlayerSpells.W;
        private static Spell E => Champion.PlayerSpells.E;

        private static bool AutoReturnW => MenuLocal.Item("Harass.UseW.Return").GetValue<bool>();

        private static int ToggleActive => MenuToggle.Item("Toggle.Active").GetValue<StringList>().SelectedIndex;

        
        private static Obj_AI_Hero Target => TargetSelector.GetTarget(Q.Range * 2, TargetSelector.DamageType.Magical);
      
        public static void Init()
        {

            MenuLocal = new LeagueSharp.Common.Menu("Harass", "Harass").SetFontStyle(FontStyle.Regular, Color.AliceBlue);
            {
                MenuLocal.AddItem(new MenuItem("Harass.UseQ", "Q:").SetValue(true).SetFontStyle(FontStyle.Regular, Q.MenuColor()));
                MenuLocal.AddItem(new MenuItem("Harass.UseW", "W:").SetValue(new StringList(new []{"Off", "On", "On: After Q"}, 2)).SetFontStyle(FontStyle.Regular, W.MenuColor()));
                MenuLocal.AddItem(new MenuItem("Harass.UseW.Return", "W: Auto Return:").SetValue(true).SetFontStyle(FontStyle.Regular, W.MenuColor()));
                MenuLocal.AddItem(new MenuItem("Harass.UseE", "E:").SetValue(false).SetFontStyle(FontStyle.Regular, E.MenuColor()));
                Modes.ModeConfig.MenuConfig.AddSubMenu(MenuLocal);
            }

            MenuToggle = new Menu("Toggle Harass", "Toggle").SetFontStyle(FontStyle.Regular, Color.AliceBlue);
            {
                MenuToggle.AddItem(new MenuItem("Toggle.Active", "Active:").SetValue(new StringList(new[] { "Just with Laneclear Mode", "Just with Lasthit Mode", "Both" }, 2)).SetFontStyle(FontStyle.Regular, Q.MenuColor()));
                MenuToggle.AddItem(new MenuItem("Toggle.UseQ", "Q:").SetValue(false).SetFontStyle(FontStyle.Regular, Q.MenuColor()));
                MenuToggle.AddItem(new MenuItem("Toggle.UseW", "W:").SetValue(false).SetFontStyle(FontStyle.Regular, W.MenuColor()));
                MenuToggle.AddItem(new MenuItem("Toggle.UseE", "E:").SetValue(false).SetFontStyle(FontStyle.Regular, E.MenuColor()));
                Modes.ModeConfig.MenuConfig.AddSubMenu(MenuToggle);
            }


            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                ExecuteHarass();
            }

            if (Modes.ModeConfig.MenuKeys.Item("Key.Harass1").GetValue<KeyBind>().Active)
            {
                ExecuteToggle();
            }
        }

        private static void ExecuteHarass()
        {
            if (MenuLocal.Item("Harass.UseQ").GetValue<bool>() && Q.CanCast(Target))
            {
                PlayerSpells.CastQ(Target);
            }

            var harassUseW = MenuLocal.Item("Harass.UseW").GetValue<StringList>().SelectedIndex;

            if (harassUseW != 0 && W.CanCast(Target))
            {
                switch (harassUseW)
                {
                    case 1:
                    {
                        PlayerSpells.CastW(Target);
                        break;
                    }
                    case 2:
                    {
                        if (Target.HasMarkedWithQ())
                            PlayerSpells.CastW(Target);
                        break;
                    }
                }

            }

            if (W.StillJumped() && AutoReturnW)
            {
                W.Cast();
            }

            if (MenuLocal.Item("Harass.UseE").GetValue<bool>() && E.CanCast(Target))
            {
                PlayerSpells.CastE(Target);
            }
        }

        private static void ExecuteToggle()
        {
            if (ToggleActive == 0 && Modes.ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
            {
                return;
            }
            
            if (ToggleActive == 1 && Modes.ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                return;
            }

            if (ToggleActive == 2 && !(Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit))
            {
                return;
            }

            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                return;
            }

            if (MenuToggle.Item("Toggle.UseQ").GetValue<bool>() && Q.CanCast(Target))
            {
                PlayerSpells.CastQ(Target);
            }

            if (MenuToggle.Item("Toggle.UseW").GetValue<bool>() && W.CanCast(Target))
            {
                PlayerSpells.CastW(Target);
            }

            if (W.StillJumped() && AutoReturnW)
            {
                W.Cast();
            }

            if (MenuToggle.Item("Toggle.UseE").GetValue<bool>() && E.CanCast(Target))
            {
                PlayerSpells.CastE(Target);
            }
        }
    }
}
