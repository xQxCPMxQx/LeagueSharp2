using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace JunglePosition
{
    class Program
    {
        public static Menu Config;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {

            Config = new Menu("xQx | Jungle Position", "JunglePosition", true);

            Config.AddItem(new MenuItem("Show", "Show").SetValue(new Circle(true, Color.GreenYellow)));
            Config.AddItem(new MenuItem("Range", "Circle Range").SetValue(new Slider(30, 100)));
            Config.AddToMainMenu();
            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat(
                "<font color='#70DBDB'>xQx | </font><font color='#FFFFFF'>Jungle Position</font> <font color='#70DBDB'> Loaded!</font>");

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var show = Config.Item("Show").GetValue<Circle>();
            if (show.Active)
            {
                var circleRange = Config.Item("Range").GetValue<Slider>().Value;
                Utility.DrawCircle(new Vector3(7444.86f, 2980.26f, 56.26f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(7232.57f, 4671.71f, 51.95f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(7232.57f, 4671.71f, 55.25f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(3402.31f, 8429.14f, 53.79f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(6859.18f, 11497.25f, 52.69f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(7010.90f, 10021.69f, 57.37f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(9850.36f, 8781.23f, 52.63f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(11128.29f, 6225.54f, 54.85f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(10270.61f, 4974.52f, 54f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(7213.78f, 2103.27f, 54.74f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(4142.55f, 5695.95f, 55.26f), circleRange, show.Color);
                Utility.DrawCircle(new Vector3(6905.46f, 12402.21f, 53.68f), circleRange, show.Color);
            }
        }
    }
}
