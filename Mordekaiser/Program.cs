using System;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using Mordekaiser.Events;
using Mordekaiser.Logics;

namespace Mordekaiser
{
    class Program
    {
        public const string ChampionName = "Mordekaiser";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;
        public static Orbwalking.Orbwalker Orbwalker;
        public static LeagueSharp.Common.Menu Config;

        public static Menu Menu;
        public static Items Items;
        public static Utils Utils;
        public static Draws Draws;

        public static OnUpdate OnUpdate;
        public static Combo Combo;
        public static Harass Harass;
        public static LaneClear LaneClear;
        public static JungleClear JungleClear;
        public static DamageCalc DamageCalc;
        
        public static SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        private static void Main(string[] args) { CustomEvents.Game.OnGameLoad += Game_OnGameLoad; }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName)
                return;

            Spells.Initiate();
            
            Config = new LeagueSharp.Common.Menu(string.Format("xQx | {0}", ChampionName), ChampionName, true);
            Config.AddSubMenu(new LeagueSharp.Common.Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new LeagueSharp.Common.Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            PlayerSpells.Initialize();

            DamageCalc = new DamageCalc();
            Utils = new Utils();
            Menu = new Menu();
            Items = new Items();

            Draws = new Draws();
            Combo = new Combo();
            Harass = new Harass();
            LaneClear = new LaneClear();
            JungleClear = new JungleClear();
            OnUpdate = new OnUpdate();
            LogicW.Initiate();

            Config.AddItem(
                new MenuItem("GameMode", "Game Mode:").SetValue(new StringList(new[] { "AP", "AD", "Hybrid", "Tanky" }, 0)))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);


            var oMenu = new LeagueSharp.Common.Menu("Other Settings", "OtherSettings");
            {
                oMenu.AddItem(new MenuItem("Other.Items", "Use Items").SetValue(true));
                oMenu.AddItem(new MenuItem("Other.Ignite", "Use Ignite").SetValue(true));
                oMenu.AddItem(new MenuItem("Other.Sheen", "Check Sheen on Combo").SetValue(true));
                oMenu.AddItem(new MenuItem("Other.Health", "Auto R if my Health < %").SetValue(new Slider(15, 0, 100)));
                Config.AddSubMenu(oMenu);
            }

            Config.AddToMainMenu();

            Game.PrintChat("Mordekasier</font> <font color='#ff3232'> How to Train Your Dragon </font> <font color='#FFFFFF'>Loaded!</font>");
        }
    }
}
