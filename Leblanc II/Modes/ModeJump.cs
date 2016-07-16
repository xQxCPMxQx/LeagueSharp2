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
    internal class ModeJump
    {
        public static Menu MenuLocal { get; private set; }
        private static Spell W => Champion.PlayerSpells.W;
        private static Spell W2 => Champion.PlayerSpells.W2;
        public static void Init(Menu ParentMenu)
        {

            MenuLocal = new LeagueSharp.Common.Menu("W Auto Return Back", "MenuReturnW").SetFontStyle(FontStyle.Regular, Color.AliceBlue);
            {
                MenuLocal.AddItem(new MenuItem("W.Return.Lasthist", "Last hit:").SetValue(true));
                MenuLocal.AddItem(new MenuItem("W.Return.Freeze", "Freeze:").SetValue(true));
                MenuLocal.AddItem(new MenuItem("W.Return.Laneclear", "Lane clear:").SetValue(true));
                MenuLocal.AddItem(new MenuItem("W.Return.Harass", "Harass:").SetValue(true));
                MenuLocal.AddItem(new MenuItem("W.Return.Combo", "Combo:").SetValue(false));

                ParentMenu.AddSubMenu(MenuLocal);

                Game.OnUpdate += GameOnOnUpdate;
            }
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit &&
                MenuLocal.Item("W.Return.Lasthist").GetValue<bool>())
            {
                if (Champion.PlayerSpells.WStillJumped)
                {
                    W.Cast();
                }

                if (Champion.PlayerSpells.W2StillJumped)
                {
                    W2.Cast();
                }
            }

            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Freeze &&
                MenuLocal.Item("W.Return.Freeze").GetValue<bool>())
            {
                if (Champion.PlayerSpells.WStillJumped)
                {
                    W.Cast();
                }

                if (Champion.PlayerSpells.W2StillJumped)
                {
                    W2.Cast();
                }
            }

            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear &&
                MenuLocal.Item("W.Return.Laneclear").GetValue<bool>())
            {
                if (Champion.PlayerSpells.WStillJumped)
                {
                    W.Cast();
                }

                if (Champion.PlayerSpells.W2StillJumped)
                {
                    W2.Cast();
                }
            }


            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed &&
                MenuLocal.Item("W.Return.Harass").GetValue<bool>())
            {
                if (Champion.PlayerSpells.WStillJumped)
                {
                    W.Cast();
                }

                if (Champion.PlayerSpells.W2StillJumped)
                {
                    W2.Cast();
                }
            }

            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                MenuLocal.Item("W.Return.Combo").GetValue<bool>())
            {
                if (Champion.PlayerSpells.WStillJumped)
                {
                    Game.PrintChat("W 1");
                    W.Cast();
                }

                if (Champion.PlayerSpells.W2StillJumped)
                {
                    Game.PrintChat("W 2");
                    W2.Cast();
                }
            }
        }
    }
}
