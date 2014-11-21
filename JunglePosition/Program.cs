using System;
using System.Linq;
using LeagueSharp;
using System.IO;
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
            Config.AddToMainMenu();
            
            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat(
                "<font color='#70DBDB'>xQx | </font><font color='#FFFFFF'>Jungle Position (4.20)</font> <font color='#70DBDB'> Loaded!</font>");

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var show = Config.Item("Show").GetValue<Circle>();
            if (show.Active)
            {
                var circleRange = 75f;
                Utility.DrawCircle(new Vector3(7461.018f, 3253.575f, 52.57141f), circleRange, Color.Blue, 1, 15); // blue team :red
                Utility.DrawCircle(new Vector3(3511.601f, 8745.617f, 52.57141f), circleRange, Color.Blue, 1, 15); // blue team :blue
                Utility.DrawCircle(new Vector3(7462.053f, 2489.813f, 52.57141f), circleRange, Color.Blue, 1, 15); // blue team :golems
                Utility.DrawCircle(new Vector3(3144.897f, 7106.449f, 51.89026f), circleRange, Color.Blue, 1, 15); // blue team :wolfs
                Utility.DrawCircle(new Vector3(7770.341f, 5061.238f, 49.26587f), circleRange, Color.Blue, 1, 15); // blue team :wariaths

                Utility.DrawCircle(new Vector3(10930.93f, 5405.83f, -68.72192f), circleRange, show.Color, 1, 15); // Dragon

                Utility.DrawCircle(new Vector3(7326.056f, 11643.01f, 50.21985f), circleRange, Color.Red, 1, 15); // red team :red
                Utility.DrawCircle(new Vector3(11417.6f, 6216.028f, 51.00244f), circleRange, Color.Red, 1, 15); // red team :blue
                Utility.DrawCircle(new Vector3(7368.408f, 12488.37f, 56.47668f), circleRange, Color.Red, 1, 15); // red team :golems
                Utility.DrawCircle(new Vector3(10342.77f, 8896.083f, 51.72742f), circleRange, Color.Red, 1, 15); // red team :wolfs
                Utility.DrawCircle(new Vector3(7001.741f, 9915.717f, 54.02466f), circleRange, Color.Red, 1, 15); // red team :wariaths
            }
        }
    }
}
