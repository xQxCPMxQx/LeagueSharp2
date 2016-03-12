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
    internal static class ModeFlee
    {
        public static Menu LocalMenu { get; private set; }

        public static void Initialize()
        {
            LocalMenu = new Menu("Flee", "Flee");
            {
                LocalMenu.AddItem(new MenuItem("Flee.UseQ", "Q:").SetValue(new StringList(new[] { "Off", "On" }, 1)).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
                LocalMenu.AddItem(new MenuItem("Flee.Youmuu", "Item Youmuu:").SetValue(new StringList(new[] { "Off", "On" }, 1)).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
                LocalMenu.AddItem(new MenuItem("Flee.DrawMouse", "Draw Mouse Position:").SetValue(new StringList(new[] { "Off", "On" }, 1)).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
            }
            PlayerMenu.MenuConfig.AddSubMenu(LocalMenu);

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {

        }
    }
}
