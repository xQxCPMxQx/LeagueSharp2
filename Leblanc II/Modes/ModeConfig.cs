using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leblanc.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Color = SharpDX.Color;

namespace Leblanc.Modes
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
        public static void Init()
        {
            MenuConfig = new Menu(":: Leblanc II ::", "Leblanc", true).SetFontStyle(FontStyle.Regular, Color.GreenYellow);

            MenuTools = new Menu("Tools", "Tools").SetFontStyle(FontStyle.Regular, Color.Aqua);
            MenuConfig.AddSubMenu(MenuTools);

            MenuTools.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(MenuTools.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            Modes.ModeSettings.Init(MenuConfig);
            {
                Common.CommonGeometry.Init();
                Common.CommonTargetSelector.Init(MenuTools);
                Common.CommonAutoLevel.Init(MenuTools);
                Common.CommonAutoBush.Init(MenuTools);
                Common.CommonSkins.Init(MenuTools);
            }

            var x = new CommonBuffManager();
            Common.CommonHelper.Init();

            MenuKeys = new Menu("Keys", "Keys").SetFontStyle(FontStyle.Bold, Color.Coral);
            {
                //MenuKeys.AddItem(new MenuItem("Key.Combo", "Combo!").SetValue(new KeyBind(MenuConfig.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.GreenYellow);
                MenuKeys.AddItem(new MenuItem("Key.ComboAA", "Combo [Without AA]").SetValue(new KeyBind('Z', KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.Aqua);
                MenuKeys.AddItem(new MenuItem("Key.ChangeCombo", "Change Combo!").SetValue(new KeyBind('J', KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.IndianRed);
                MenuKeys.AddItem(new MenuItem("Key.Harass1", "Harass Toggle!").SetValue(new KeyBind('T', KeyBindType.Toggle)).SetFontStyle(FontStyle.Regular, Color.GreenYellow)).Permashow(true, ObjectManager.Player.ChampionName + " | Harass Toggle", Color.AliceBlue);
                
                //.ValueChanged += (sender, args) =>
                //        {
                //            MenuKeys.Item("Key.Harass1").Permashow(args.GetNewValue<KeyBind>().Active, ObjectManager.Player.ChampionName + " | Harass Toggle", Color.AliceBlue);
                //            //if (args.GetNewValue<KeyBind>().Active)
                //            //{
                //            //    MenuKeys.Item("Key.Harass1").Permashow(args.GetNewValue<KeyBind>().Active, ObjectManager.Player.ChampionName + " | Harass Toggle",Color.AliceBlue);
                //            //}
                //            //else
                //            //{
                //            //    MenuKeys.Item("Key.Harass1").Permashow(false);
                //            //}
                //        };

                MenuKeys.AddItem(new MenuItem("Key.DoubleChain", "Double Chain!").SetValue(new KeyBind('G', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, Color.IndianRed));
                //MenuKeys.AddItem(new MenuItem("Key.Farm", "Farm").SetValue(new KeyBind(MenuConfig.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Color.DarkKhaki);
                MenuKeys.AddItem(new MenuItem("Key.Flee", "Flee").SetValue(new KeyBind('A', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, Color.GreenYellow));
                
                MenuConfig.AddSubMenu(MenuKeys);
            }

            Modes.ModeCombo.Init();
            Modes.ModeChain.Init();
            Modes.ModeHarass.Init();

            MenuFarm = new Menu("Farm", "Farm");
            {
                Modes.ModeLane.Init(MenuFarm);
                Modes.ModeJungle.Init(MenuFarm);

                MenuFarm.AddItem(new MenuItem("Farm.Enable", ":: Lane / Jungle Clear Active!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, ObjectManager.Player.ChampionName + " | " + "Lane/Jungle Farm", Colors.ColorPermaShow);
                MenuFarm.AddItem(new MenuItem("Farm.MinMana.Enable", "Min. Mana Control!").SetValue(new KeyBind("M".ToCharArray()[0], KeyBindType.Toggle, true)).SetFontStyle(FontStyle.Regular, Color.Aqua)).Permashow(true, ObjectManager.Player.ChampionName + " | " + "Min. Mana Control!", Colors.ColorPermaShow);

                MenuConfig.AddSubMenu(MenuFarm);
            }

            Modes.ModeFlee.Init(MenuConfig);

            new ModeDraw().Init();
            Champion.PlayerObjects.Init();
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
