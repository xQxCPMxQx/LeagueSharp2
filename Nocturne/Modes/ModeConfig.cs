using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Common;
using Color = SharpDX.Color;

namespace Nocturne.Modes
{
    internal class ModeConfig
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu MenuConfig { get; private set; }
        public static Menu MenuKeys { get; private set; }
        public static Menu MenuHarass { get; private set; }
        public static Menu MenuFarm { get; private set; }
        public static Menu MenuFlee { get; private set; }
        public static Menu MenuMisc { get; private set; }
        public static Menu MenuTools { get; private set; }
        // to-do: add ganker mode combo mode + use Q with E Combo
        public static void Init()
        {
            MenuConfig = new Menu(":: Nocturne is Back", "Nocturne", true).SetFontStyle(FontStyle.Regular, Color.GreenYellow);

            MenuTools = new Menu("Tools", "Tools");
            MenuConfig.AddSubMenu(MenuTools);

            MenuTools.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(MenuTools.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);


            Common.CommonTargetSelector.Init(MenuTools);
            Common.CommonAutoLevel.Init(MenuTools);
            Common.CommonAutoBush.Init(MenuTools);
            Common.CommonSkins.Init(MenuTools);
            Common.CommonHelper.Init();

            Modes.ModeSettings.Init(MenuConfig);

            /*
                MenuCombo = new Menu("Combo", "ExecuteCombo");
                MenuConfig.AddSubMenu(MenuCombo);
            */

            MenuKeys = new Menu("Keys", "Keys").SetFontStyle(FontStyle.Bold, Color.Coral);
            {
                MenuKeys.AddItem(new MenuItem("Key.Combo", "Combo!").SetValue(new KeyBind(MenuConfig.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.GreenYellow);
//                MenuKeys.AddItem(new MenuItem("Key.Ganker", "Gank Mode!").SetValue(new KeyBind('G', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, Color.GreenYellow)).SetTag(900);
                MenuKeys.AddItem(new MenuItem("Key.Harass", "Harass").SetValue(new KeyBind(MenuConfig.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.Coral);
                MenuKeys.AddItem(new MenuItem("Key.HarassToggle", "Harass (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle))).SetFontStyle(FontStyle.Regular, Color.Coral).Permashow(true, ObjectManager.Player.ChampionName + " : Toggle Harass", Color.Aqua);
                MenuKeys.AddItem(new MenuItem("Key.Lane", "Lane Farm & Jungle Clear").SetValue(new KeyBind(MenuConfig.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.DarkKhaki);
                MenuKeys.AddItem(new MenuItem("Key.Flee", "Flee").SetValue(new KeyBind('A', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, Color.GreenYellow));
                
                MenuConfig.AddSubMenu(MenuKeys);
            }

            Modes.ModeCombo.Init();
            Evade.EvadeMain.Init();

            MenuFarm = new Menu("Farm", "Farm");
            {
                MenuFarm.AddItem(new MenuItem("Farm.Enable", ":: Lane / Jungle Clear Active!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, ObjectManager.Player.ChampionName + " | " + "Lane/Jungle Farm", Colors.ColorPermaShow);
                Modes.ModeLane.Init(MenuFarm);
                Modes.ModeJungle.Init(MenuFarm);

                
                //MenuFarm.AddItem(new MenuItem("Farm.MinMana.Enable", "Min. Mana Control Active!").SetValue(new KeyBind("M".ToCharArray()[0], KeyBindType.Toggle, true)).SetFontStyle(FontStyle.Regular, Color.Aqua)).Permashow(true, ObjectManager.Player.ChampionName + " | " + "Min. Mana Control Active", Colors.ColorPermaShow);

                MenuConfig.AddSubMenu(MenuFarm);
            }

            Modes.ModeFlee.Init(MenuConfig);

            Common.CommonManaManager.Init(MenuFarm);
            Common.CommonJungleTimer.Init(MenuFarm);

            //MenuConfig.AddItem(new MenuItem("Game.Mode", "Game Mode:").SetValue(new StringList(new[] { "Auto", "Ganker Mode", "Assassin Mode" }, 0)).SetFontStyle(FontStyle.Regular, Color.Coral));
            //MenuConfig.AddItem(new MenuItem("Pc.Mode", "How is your own Computer:").SetValue(new StringList(new[] { "New Computer", "Old Computer" }, 0)).SetFontStyle(FontStyle.Regular, Color.Coral));

            ModeDraw.Init(MenuConfig);

            MenuConfig.AddToMainMenu();
            
            foreach (var i in MenuConfig.Children.Cast<Menu>().SelectMany(GetSubMenu))
            {
                i.DisplayName = ":: " + i.DisplayName;
            }

        }

        private static IEnumerable<Menu> GetSubMenu(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetSubMenu))
                yield return childChild;
        }

    }
}
