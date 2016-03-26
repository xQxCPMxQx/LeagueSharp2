using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = SharpDX.Color;

namespace Nocturne.Modes
{
    internal class ModeConfig
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu MenuConfig { get; private set; }
        public static Menu MenuKeys { get; private set; }
        public static Menu MenuHarass { get; private set; }
        public static Menu MenuFarming { get; private set; }
        public static Menu MenuFlee { get; private set; }
        public static Menu MenuMisc { get; private set; }
        // to-do: add ganker mode combo mode + use Q with E Combo
        public static void Initialize()
        {
            MenuConfig = new Menu("Nocturne", "Nocturne", true);

            MenuConfig.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(MenuConfig.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            Common.CommonTargetSelector.Initialize();
            Common.CommonAutoLevel.Initialize();
            Common.CommonAutoBush.Initialize();
            Common.CommonHelper.Initialize();
            
            /*
                MenuCombo = new Menu("Combo", "ExecuteCombo");
                MenuConfig.AddSubMenu(MenuCombo);
            */

            MenuKeys = new Menu("Keys", "Keys").SetFontStyle(FontStyle.Bold, Color.Coral);
            {
                MenuKeys.AddItem(new MenuItem("Key.Combo", "Combo!").SetValue(new KeyBind(MenuConfig.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.GreenYellow);
                MenuKeys.AddItem(new MenuItem("Key.Ganker", "Gank Mode!").SetValue(new KeyBind('G', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, Color.GreenYellow)).SetTag(900);
                MenuKeys.AddItem(new MenuItem("Key.Harass", "Harass").SetValue(new KeyBind(MenuConfig.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.Coral);
                MenuKeys.AddItem(new MenuItem("Key.HarassToggle", "Harass (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle))).SetFontStyle(FontStyle.Regular, Color.Coral).Permashow(true, ObjectManager.Player.ChampionName + " : Toggle Harass", Color.Aqua);
                MenuKeys.AddItem(new MenuItem("Key.Lane", "Lane Farm & Jungle Clear").SetValue(new KeyBind(MenuConfig.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.DarkKhaki);
                MenuKeys.AddItem(new MenuItem("Key.Flee", "Flee").SetValue(new KeyBind('A', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, Color.GreenYellow));
                
                MenuConfig.AddSubMenu(MenuKeys);
            }

            MenuFarming = new Menu("Farm", "Farm");
            {
                Modes.ModeLane.Initialize(MenuFarming);
                Modes.ModeJungle.Initialize(MenuFarming);
                MenuConfig.AddSubMenu(MenuFarming);
            }

            Common.CommonManaManager.Initialize(MenuFarming);
            Common.CommonJungleTimer.Initialize(MenuFarming);

            // Misc
            MenuMisc = new Menu("Misc", "Misc");
            {
                MenuMisc.AddItem(new MenuItem("InterruptSpells", "W: Interrupt Spells").SetValue(true));
                MenuConfig.AddSubMenu(MenuMisc);
            }

            MenuConfig.AddItem(new MenuItem("Game.Mode", "Game Mode:").SetValue(new StringList(new[] { "Auto", "Gank Mode", "Assassin Mode" }, 0)).SetFontStyle(FontStyle.Regular, Color.Coral));
            MenuConfig.AddItem(new MenuItem("Pc.Mode", "How is your own Computer:").SetValue(new StringList(new[] { "New Computer", "Old Computer" }, 0)).SetFontStyle(FontStyle.Regular, Color.Coral));

            new PlayerDrawings().Initialize();
                    
            Evade.EvadeMain.Initialize();

            MenuConfig.AddToMainMenu();

            
            foreach (var i in MenuConfig.Children.Cast<Menu>().SelectMany(GetSubMenu))
            {
                
                i.DisplayName = ":: " + i.DisplayName;

                ///* Center Titles */
                //var currentLength = 0;
                //var maxLenght = 0;

                //foreach (var item in i.Items)
                //{
                //    currentLength = item.DisplayName.Length;
                //    if (currentLength > maxLenght)
                //    {
                //        maxLenght = currentLength;
                //    }
                //}
                //maxLenght += 24;
                //foreach (var item in i.Items.Where(it => it.Tag == 900))
                //{
                //    var pad = "";
                //    for (int j = 0; j < (maxLenght / 2 - item.DisplayName.Length / 2) + (24 / 2); j++)
                //    {
                //        pad += " ";
                //    }

                //    var x = item.DisplayName;
                //    item.DisplayName = pad + x;
                //    item.SetFontStyle(FontStyle.Bold, Color.Aqua);
                //}
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
