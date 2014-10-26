using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace MasterYiQx
{
    internal class AssassinManager
    {
        public AssassinManager()
        {
            Load();
        }

        private static void Load()
        {
            Program.TargetSelectorMenu.AddSubMenu(new Menu("Assassin Manager", "AssassinManager"));
            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddItem(new MenuItem("AssassinActive", "Assassin Active").SetValue(true));
            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddItem(new MenuItem("AssassinSetClick", "Use Click Add/Remove").SetValue(true));
            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddItem(new MenuItem("AssassinRangeColor", "Assassin Range Color").SetValue(new Circle(true, Color.GreenYellow)));
            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddItem(new MenuItem("AssassinInRangeColor", "Range Enemy Color").SetValue(new Circle(true, Color.GreenYellow)));
            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddItem(new MenuItem("AssassinInCloseColor", "Nearest Enemy Color").SetValue(new Circle(true, Color.DarkSeaGreen)));
            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddItem(new MenuItem("AssassinReset", "Reset Assassin List").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            Program.TargetSelectorMenu.SubMenu("AssassinManager").AddSubMenu(new Menu("Assassin 1st :", "AssassinMode"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.TargetSelectorMenu.SubMenu("AssassinManager")
                    .SubMenu("AssassinMode").AddItem(new MenuItem("Assassin" + enemy.ChampionName, enemy.ChampionName).SetValue(SimpleTs.GetPriority(enemy) > 3));
            }
            Program.TargetSelectorMenu.SubMenu("AssassinManager")
                .AddItem(new MenuItem("AssassinRange", "Assassin Range")).SetValue(new Slider(1000, 2000));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void OnGameUpdate(EventArgs args)
        {
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {

            if (Program.TargetSelectorMenu.Item("AssassinReset").GetValue<KeyBind>().Active && args.Msg == 257)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
                {
                    Program.TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).SetValue(false);
                }
                Game.PrintChat("<font color='#FFFFFF'>Reset Assassin List is Complete! Click on the enemy for Add/Remove.</font>");
            }

            if (args.Msg != 0x201)
            {
                return;
            }

            if (Program.TargetSelectorMenu.Item("AssassinSetClick").GetValue<bool>())
            {
                foreach (var objAiHero in from hero in ObjectManager.Get<Obj_AI_Hero>()
                                          where hero.IsValidTarget()
                                          select hero
                                              into h
                                              orderby h.Distance(Game.CursorPos) descending
                                              select h
                                                  into enemy
                                                  where enemy.Distance(Game.CursorPos) < 150f
                                                  select enemy)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        var menuStatus = Program.TargetSelectorMenu.Item("Assassin" + objAiHero.ChampionName).GetValue<bool>();
                        Program.TargetSelectorMenu.Item("Assassin" + objAiHero.ChampionName).SetValue(!menuStatus);
                        Game.PrintChat(string.Format("<font color='{0}'>{1}</font> <font color='#09F000'>{2} ({3})</font>",
                                            !menuStatus ? "#FFFFFF" : "#FF8877",
                                            !menuStatus ? "Added to Assassin List:" : "Removed from Assassin List:",
                                            objAiHero.Name, objAiHero.ChampionName));
                    }
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Program.TargetSelectorMenu.Item("AssassinActive").GetValue<bool>())
                return;

            if (ObjectManager.Player.IsDead)
                return;

            var drawRangeColor = Program.TargetSelectorMenu.Item("AssassinRangeColor").GetValue<Circle>();
            var drawRangeEnemyColor = Program.TargetSelectorMenu.Item("AssassinInRangeColor").GetValue<Circle>();
            var drawNearestEnemyColor = Program.TargetSelectorMenu.Item("AssassinInCloseColor").GetValue<Circle>();

            var assassinRange = Program.TargetSelectorMenu.Item("AssassinRange").GetValue<Slider>().Value;
            if (drawRangeColor.Active)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, assassinRange, drawRangeColor.Color);
            }

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>()
                .Where(enemy => enemy.Team != ObjectManager.Player.Team)
                .Where(enemy => enemy.IsVisible && Program.TargetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null && !enemy.IsDead)
                .Where(enemy => Program.TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>()))
            {
                if (ObjectManager.Player.Distance(enemy) < assassinRange)
                {
                    if (drawRangeEnemyColor.Active)
                        Utility.DrawCircle(enemy.Position, 100f, drawRangeEnemyColor.Color);
                }
                else if (ObjectManager.Player.Distance(enemy) > assassinRange && ObjectManager.Player.Distance(enemy) < assassinRange + 400)
                {
                    if (drawNearestEnemyColor.Active)
                        Utility.DrawCircle(enemy.Position, 100f, drawNearestEnemyColor.Color);
                }
            }
        }
    }
}