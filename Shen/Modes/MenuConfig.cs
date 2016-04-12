using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Shen.Champion;
using Color = SharpDX.Color;

namespace Shen.Modes
{
    internal static class MenuConfig
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static LeagueSharp.Common.Menu LocalMenu { get; private set; }
        public static void Initialize()
        {

            LocalMenu = new Menu(":: Shen is Back", "Shen", true).SetFontStyle(FontStyle.Regular, Color.GreenYellow);

            var MenuTools = new Menu("Tools", "Tools");
            LocalMenu.AddSubMenu(MenuTools);

            MenuTools.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(MenuTools.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            Shen.Champion.PlayerSpells.Initialize();
            ModeSelector.Initialize(MenuTools);

            Common.AutoBushManager.Initialize(MenuTools);
            Common.AutoLevelManager.Initialize(MenuTools);
            Common.SummonerManager.Initialize();
            Common.ItemManager.Initialize();
            Common.CommonSkins.Initialize(MenuTools);

            ModeUlti.Initialize(LocalMenu);
            ModeCombo.Initialize(LocalMenu);
            ModeJungle.Initialize(LocalMenu);
            ModeDrawing.Initialize(LocalMenu);
            
            ModePerma.Initialize(LocalMenu);

            SpiritUnit.Initialize();
            
            LocalMenu.AddToMainMenu();

            foreach (var i in LocalMenu.Children.Cast<Menu>().SelectMany(GetSubMenu))
            {

                i.DisplayName = ":: " + i.DisplayName;
            }


            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (Shen.Champion.PlayerSpells.R.IsReady() && Shen.Modes.ModeUlti.LocalMenu.Item("SpellR.ConfirmKey").GetValue<KeyBind>().Active)
            {
                var t = ModeUlti.GetHelplessAlly;
                if (t != null && Shen.Champion.PlayerSpells.R.IsReady())
                {
                    Shen.Champion.PlayerSpells.R.CastOnUnit(t);
                }
            }
        }

        private static IEnumerable<Menu> GetSubMenu(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetSubMenu))
                yield return childChild;
        }

        private static List<Obj_AI_Hero> GetHelplessTeamMate()
        {
            return null;
        }

    }
}
