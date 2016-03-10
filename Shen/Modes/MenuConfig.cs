using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Shen.Champion;

namespace Shen.Modes
{
    internal static class MenuConfig
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static LeagueSharp.Common.Menu LocalMenu { get; private set; }
        public static void Initialize()
        {
            LocalMenu = new LeagueSharp.Common.Menu("Shen", "Shen", true);

            LocalMenu.AddSubMenu(new LeagueSharp.Common.Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(LocalMenu.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);
            Shen.Champion.PlayerSpells.Initialize();
            ModeSelector.Initialize(LocalMenu);

            Common.AutoBushManager.Initialize();
            Common.AutoLevelManager.Initialize();
            Common.SummonerManager.Initialize();
            Common.ItemManager.Initialize();

            ModeUlti.Initialize(LocalMenu);
            ModeCombo.Initialize(LocalMenu);
            ModeJungle.Initialize(LocalMenu);
            ModeDrawing.Initialize(LocalMenu);
            
            //ModePerma.Initialize(LocalMenu);

            SpiritUnit.Initialize();
            
            LocalMenu.AddToMainMenu();
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

        private static List<Obj_AI_Hero> GetHelplessTeamMate()
        {
            return null;
        }

    }
}
