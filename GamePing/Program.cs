#region
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
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
