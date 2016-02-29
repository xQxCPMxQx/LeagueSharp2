using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KaiHelper.Activator;
using KaiHelper.Misc;
using KaiHelper.Timer;
using KaiHelper.Tracker;
using LeagueSharp;
using LeagueSharp.Common;

namespace KaiHelper
{
    internal class Program
    {
        public static Menu MainMenu;

        private static void Main(string[] args)
        {
            MainMenu = new Menu("KaiHelper", "KaiHelp", true);
            //Menu Tracker = MainMenu.AddSubMenu(new Menu("Tracker", "Tracker"));
            new SkillBar(MainMenu);
            new JungleTimer(MainMenu);
            new GankDetector(MainMenu);
            //new LastPosition(MainMenu);
            new WayPoint(MainMenu);
            new WardDetector(MainMenu);
            new HealthTurret(MainMenu);

            AutoBushRevealer.Initialize(MainMenu);

            //Menu Timer = MainMenu.AddSubMenu(new Menu("Timer", "Timer"));
            //Menu Range = MainMenu.AddSubMenu(new Menu("Range", "Range"));
            new Vision(MainMenu);
            //Menu ActivatorMenu = MainMenu.AddSubMenu(new Menu("Activator", "Activator"));
            new AutoPot(MainMenu);



            foreach (var i in MainMenu.Children.Cast<Menu>().SelectMany(GetChildirens))
            {
                i.DisplayName = ":: " + i.DisplayName;
            }



            MainMenu.AddToMainMenu();
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static IEnumerable<Menu> GetChildirens(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetChildirens))
                yield return childChild;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            bool hasUpdate = Helper.HasNewVersion(Assembly.GetExecutingAssembly().GetName().Name);
            Game.PrintChat("KaiHelper Temp Fixed Version- Loaded!");
        }
    }
}
