#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace GamePing
{
    internal class Program
    {
        public static Menu Config;

        private static void Main(string[] args)
        {
            Config = new Menu("Game Ping", "GamePing", true);
            Config.AddItem(new MenuItem("Show", "Show Game Ping").SetValue(true));
            Config.AddToMainMenu();
 
            Drawing.OnDraw += (arg) =>
            {
                if (Config.Item("Show").IsActive())
                    Drawing.DrawText(Drawing.Width*0.94f, Drawing.Height*0.04f, Color.GreenYellow, "Ping: " + Game.Ping);
            };
        }
    }
}
