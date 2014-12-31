using System;
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
            Config.AddItem(new MenuItem("Show", "Show").SetValue(true));
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat(
                "<font color='#70DBDB'>xQx | </font><font color='#FFFFFF'>Jungle Position (4.20)</font> <font color='#70DBDB'> Loaded!</font>");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var show = Config.Item("Show").GetValue<bool>();
            if (show)
            {
                var circleRange = 75f;
                if (Game.MapId == (GameMapId) 11)
                {
                    Utility.DrawCircle(new Vector3(7461.018f, 3253.575f, 52.57141f), circleRange, Color.Blue, 1, 15); // blue team :red
                    Utility.DrawCircle(new Vector3(3511.601f, 8745.617f, 52.57141f), circleRange, Color.Blue, 1, 15); // blue team :blue
                    Utility.DrawCircle(new Vector3(7462.053f, 2489.813f, 52.57141f), circleRange, Color.Blue, 1, 15); // blue team :golems
                    Utility.DrawCircle(new Vector3(3144.897f, 7106.449f, 51.89026f), circleRange, Color.Blue, 1, 15); // blue team :wolfs
                    Utility.DrawCircle(new Vector3(7770.341f, 5061.238f, 49.26587f), circleRange, Color.Blue, 1, 15); // blue team :wariaths

                    Utility.DrawCircle(new Vector3(10930.93f, 5405.83f, -68.72192f), circleRange, Color.Yellow, 1, 15); // Dragon

                    Utility.DrawCircle(new Vector3(7326.056f, 11643.01f, 50.21985f), circleRange, Color.Red, 1, 15); // red team :red
                    Utility.DrawCircle(new Vector3(11417.6f, 6216.028f, 51.00244f), circleRange, Color.Red, 1, 15); // red team :blue
                    Utility.DrawCircle(new Vector3(7368.408f, 12488.37f, 56.47668f), circleRange, Color.Red, 1, 15); // red team :golems
                    Utility.DrawCircle(new Vector3(10342.77f, 8896.083f, 51.72742f), circleRange, Color.Red, 1, 15); // red team :wolfs
                    Utility.DrawCircle(new Vector3(7001.741f, 9915.717f, 54.02466f), circleRange, Color.Red, 1, 15); // red team :wariaths                    

                }
                else if (Game.MapId == GameMapId.SummonersRift)
                {
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
}
