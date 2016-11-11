using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Leblanc.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Leblanc.Modes
{
    internal static class ModeFlee
    {
        public static Menu MenuLocal { get; private set; }

        public static void Init(Menu ParentMenu)
        {
            MenuLocal = new Menu("Flee", "Flee");
            {
                MenuLocal.AddItem(new MenuItem("Flee.UseW", "W:").SetValue(true).SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.Q.MenuColor()));
                MenuLocal.AddItem(new MenuItem("Flee.UseR", "R:").SetValue(true).SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.W.MenuColor()));
                MenuLocal.AddItem(new MenuItem("Flee.DrawMouse", "Show Mouse Cursor Position:").SetValue(true));
            }
            ParentMenu.AddSubMenu(MenuLocal);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += delegate(EventArgs args)
            {
                if (!Modes.ModeDraw.MenuLocal.Item("Draw.Enable").GetValue<bool>())
                {
                    return;
                }

                if (!ModeConfig.MenuKeys.Item("Key.Flee").GetValue<KeyBind>().Active)
                {
                    return;
                }

                if (MenuLocal.Item("Flee.DrawMouse").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(Game.CursorPos, 150f, System.Drawing.Color.Red);
                }
            };
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!ModeConfig.MenuKeys.Item("Key.Flee").GetValue<KeyBind>().Active)
            {
                return;
            }

            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (MenuLocal.Item("Flee.UseW").GetValue<bool>())
            {
                Champion.PlayerSpells.CastW(Game.CursorPos);
            }

            if (MenuLocal.Item("Flee.UseR").GetValue<bool>())
            {
                if (Champion.PlayerSpells.W2.IsReady() && !Champion.PlayerSpells.W2.StillJumped())
                {
                    Champion.PlayerSpells.W2.Cast(Game.CursorPos);
                }
                //Champion.PlayerSpells.CastW2(Game.CursorPos);
            }
        }
    }
}
