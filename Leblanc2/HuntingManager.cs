using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Leblanc
{
    internal class HuntingManager
    {
        private static Obj_AI_Hero _selectedTarget;
       
        public HuntingManager()
        {
            LoadTargetSelectorMenu();
            LoadHuntingMenu();
        }

        private static void LoadTargetSelectorMenu()
        {
            Program.TargetSelectorMenu.AddItem(new MenuItem("TSMode", "Mode"))
                .SetValue(new StringList(new[]
                {
                    "AutoPriority",
                    "Closest",
                    "LessAttack",
                    "LessCast",
                    "LowHP",
                    "MostAD",
                    "MostAP",
                    "NearMouse"
                }, 1));

            Program.TargetSelectorMenu.AddItem(new MenuItem("TSRange", "TS. Range")).SetValue(new Slider(1000, 2000, 100));
        }

        private static void LoadHuntingMenu()
        {
            Program.TargetSelectorMenu.AddItem(new MenuItem("TSMode", "Mode"))
                .SetValue(new StringList(new[] 
                    { "AutoPriority", 
                      "Closest", 
                      "LessAttack", 
                      "LessCast", 
                      "LowHP", 
                      "MostAD", 
                      "MostAP", 
                      "NearMouse" }, 1));

            Program.TargetSelectorMenu.AddItem(new MenuItem("TSRange", "TS. Range")).SetValue(new Slider(1000, 2000, 100));


            Program.TargetSelectorMenu.AddSubMenu(new Menu("Hunting Manager", "HuntinManager"));
            Program.TargetSelectorMenu.SubMenu("HuntinManager").AddItem(new MenuItem("HuntActive", "Hunting Active").SetValue(true));
            Program.TargetSelectorMenu.SubMenu("HuntinManager").AddItem(new MenuItem("HuntRangeColor", "Range Color").SetValue(new Circle(true, Color.GreenYellow)));
            Program.TargetSelectorMenu.SubMenu("HuntinManager").AddItem(new MenuItem("HuntInRangeColor", "Range Enemy Color").SetValue(new Circle(true, Color.GreenYellow)));
            Program.TargetSelectorMenu.SubMenu("HuntinManager").AddItem(new MenuItem("HuntInCloseColor", "Nearest Enemy Color").SetValue(new Circle(true, Color.DarkSeaGreen)));


            Program.TargetSelectorMenu.SubMenu("HuntinManager").AddSubMenu(new Menu("Hunt 1st :", "HuntMode"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.TargetSelectorMenu.SubMenu("HuntinManager")
                    .SubMenu("HuntMode").AddItem(new MenuItem("Hunt" + enemy.ChampionName, enemy.ChampionName).SetValue(false));
            }
            Program.TargetSelectorMenu.SubMenu("HuntinManager")
                .AddItem(new MenuItem("HuntRange", "Hunting Range")).SetValue(new Slider(1000, 2000, 100));

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            TSMode();
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != 0x201)
            {
                return;
            }
            foreach (var objAIHero in from hero in ObjectManager.Get<Obj_AI_Hero>()
                                      where hero.IsValidTarget()
                                      select hero into h
                                      orderby h.Distance(Game.CursorPos) descending
                                      select h into enemy
                                      where enemy.Distance(Game.CursorPos) < 150f
                                      select enemy)
            {
                if (_selectedTarget == null || objAIHero.NetworkId != _selectedTarget.NetworkId && _selectedTarget.IsVisible && !_selectedTarget.IsDead)
                {
                    var menuStatus = Program.TargetSelectorMenu.Item("Hunt" + objAIHero.ChampionName).GetValue<bool>();
                    if (!menuStatus)
                    {
                        Program.TargetSelectorMenu.Item("Hunt" + objAIHero.ChampionName).SetValue(true);
                        Game.PrintChat(string.Format("<font color='#FF8877'>Added to Hunt List: </font> <font color='#70DBDB'>{0}</font>", objAIHero.ChampionName));
                    }
                    else
                    {
                        Program.TargetSelectorMenu.Item("Hunt" + objAIHero.ChampionName).SetValue(false);
                        Game.PrintChat(string.Format("<font color='#FFFFFF'>Removed from Hunt List: </font> <font color='#70DBDB'>{0}</font>", objAIHero.ChampionName));
                    }
                }
                else
                {
                    _selectedTarget = null;
                }
            }
        }

        private static void TSMode()
        {

            float TSRange = Program.TargetSelectorMenu.Item("TSRange").GetValue<Slider>().Value;
            Program.vTargetSelector.SetRange(TSRange);
            var mode = Program.TargetSelectorMenu.Item("TSMode").GetValue<StringList>().SelectedIndex;
            Program.vTargetSelectorStr = "";
            switch (mode)
            {
                case 0:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.AutoPriority);
                    Program.vTargetSelectorStr = "Targetin Mode: Auto Priority";
                    break;
                case 1:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.Closest);
                    Program.vTargetSelectorStr = "Targetin Mode: Closest";
                    break;
                case 2:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.LessAttack);
                    Program.vTargetSelectorStr = "Targetin Mode: Less Attack";
                    break;
                case 3:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.LessCast);
                    Program.vTargetSelectorStr = "Targetin Mode: Less Cast";
                    break;
                case 4:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.LowHP);
                    Program.vTargetSelectorStr = "Targetin Mode: Low HP";
                    break;
                case 5:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.MostAD);
                    Program.vTargetSelectorStr = "Targetin Mode: Most AD";
                    break;
                case 6:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.MostAP);
                    Program.vTargetSelectorStr = "Targetin Mode: Most AP";
                    break;
                case 7:
                    Program.vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.NearMouse);
                    Program.vTargetSelectorStr = "Targetin Mode: Near Mouse";
                    break;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Program.TargetSelectorMenu.Item("HuntActive").GetValue<bool>())
                return;

            /* [Draw Hunting Mode ] */
            var drawRangeColor = Program.TargetSelectorMenu.Item("HuntRangeColor").GetValue<Circle>();
            var drawRangeEnemyColor = Program.TargetSelectorMenu.Item("HuntInRangeColor").GetValue<Circle>();
            var drawNearestEnemyColor = Program.TargetSelectorMenu.Item("HuntInCloseColor").GetValue<Circle>();

            var huntRange = Program.TargetSelectorMenu.Item("HuntRange").GetValue<Slider>().Value;
            if (drawRangeColor.Active)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, huntRange, drawRangeColor.Color);
            }

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                if (enemy.IsVisible && Program.TargetSelectorMenu.Item("Hunt" + enemy.BaseSkinName) != null && !enemy.IsDead &&
                    Program.TargetSelectorMenu.Item("Hunt" + enemy.BaseSkinName).GetValue<bool>())
                {
                    if (ObjectManager.Player.Distance(enemy) < huntRange)
                    {
                        if (drawRangeEnemyColor.Active)
                            Utility.DrawCircle(enemy.Position, 100f, drawRangeEnemyColor.Color);
                    }
                    else if (ObjectManager.Player.Distance(enemy) > huntRange && ObjectManager.Player.Distance(enemy) < huntRange + 400)
                    {
                        if (drawNearestEnemyColor.Active)
                            Utility.DrawCircle(enemy.Position, 100f, drawNearestEnemyColor.Color);
                    }
                }
            }

            /* [Draw Target Selector Mode ] */
            Drawing.DrawLine(Drawing.Width * 0.5f - 61, Drawing.Height * 0.83f - 3, Drawing.Width * 0.5f + 201, Drawing.Height * 0.83f - 3, 27, Color.Black);
            Drawing.DrawLine(Drawing.Width * 0.5f - 60, Drawing.Height * 0.83f - 2, Drawing.Width * 0.5f + 200, Drawing.Height * 0.83f - 2, 25, Color.Wheat);
            Drawing.DrawText(Drawing.Width * 0.5f - 35, Drawing.Height * 0.832f, Color.Black, Program.vTargetSelectorStr);
        }


    }
}