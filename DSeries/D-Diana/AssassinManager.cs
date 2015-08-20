using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace D_Diana
{
    internal class AssassinManager
    {
        public static Font Text, TextBold;
        private static readonly string space = "    ";

        public AssassinManager()
        {
            Load();
        }

        private static void Load()
        {
            TextBold = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 13,
                    Weight = FontWeight.Bold,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 13,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            Program.Config.AddSubMenu(new Menu("Set Priority Targets", "MenuAssassin"));
            Program.Config.SubMenu("MenuAssassin").AddItem(new MenuItem("AssassinActive", "Active").SetValue(true));
            Program.Config.SubMenu("MenuAssassin")
                .AddItem(new MenuItem("AssassinSearchRange", space + "Search Range"))
                .SetValue(new Slider(1400, 2000));

            Program.Config.SubMenu("MenuAssassin")
                .AddItem(
                    new MenuItem("AssassinSelectOption", space + "Set:").SetValue(
                        new StringList(new[] {"Single Select", "Multi Select"})));

            Program.Config.SubMenu("MenuAssassin").AddItem(new MenuItem("xM1", "Enemies:"));
            foreach (
                var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.Config.SubMenu("MenuAssassin")
                    .AddItem(
                        new MenuItem("Assassin" + enemy.ChampionName, space + enemy.ChampionName).SetValue(
                            TargetSelector.GetPriority(enemy) > 3));
            }
            Program.Config.SubMenu("MenuAssassin").AddItem(new MenuItem("xM2", "Other Settings:"));

            Program.Config.SubMenu("MenuAssassin")
                .AddItem(new MenuItem("AssassinSetClick", space + "Add/Remove with click").SetValue(true));
            Program.Config.SubMenu("MenuAssassin")
                .AddItem(
                    new MenuItem("AssassinReset", space + "Reset List").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));

            Program.Config.SubMenu("MenuAssassin").AddSubMenu(new Menu("Drawings", "Draw"));

            Program.Config.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawSearch", "Search Range").SetValue(new Circle(true, Color.GreenYellow)));
            Program.Config.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawActive", "Active Enemy").SetValue(new Circle(true, Color.GreenYellow)));
            Program.Config.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawNearest", "Nearest Enemy").SetValue(new Circle(true, Color.DarkSeaGreen)));
            Program.Config.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawStatus", "Show status on the screen").SetValue(true));

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void ClearAssassinList()
        {
            foreach (
                var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.Config.Item("Assassin" + enemy.ChampionName).SetValue(false);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (Program.Config.Item("AssassinReset").GetValue<KeyBind>().Active && args.Msg == 257)
            {
                ClearAssassinList();
                Game.PrintChat(
                    "<font color='#FFFFFF'>Reset Assassin List is Complete! Click on the enemy for Add/Remove.</font>");
            }

            if (args.Msg != (uint) WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            if (Program.Config.Item("AssassinSetClick").GetValue<bool>())
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
                        var xSelect =
                            Program.Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                Program.Config.Item("Assassin" + objAiHero.ChampionName).SetValue(true);
                                Game.PrintChat(
                                    $"<font color='FFFFFF'>Added to Assassin List</font> <font color='#09F000'>{objAiHero.Name} ({objAiHero.ChampionName})</font>");
                                break;
                            case 1:
                                var menuStatus =
                                    Program.Config.Item("Assassin" + objAiHero.ChampionName)
                                        .GetValue<bool>();
                                Program.Config.Item("Assassin" + objAiHero.ChampionName)
                                    .SetValue(!menuStatus);
                                Game.PrintChat(
                                    $"<font color='{(!menuStatus ? "#FFFFFF" : "#FF8877")}'>{(!menuStatus ? "Added to Assassin List:" : "Removed from Assassin List:")}</font> <font color='#09F000'>{objAiHero.Name} ({objAiHero.ChampionName})</font>");
                                break;
                        }
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Program.Config.Item("AssassinActive").GetValue<bool>())
                return;

            if (Program.Config.Item("DrawStatus").GetValue<bool>())
            {
                var enemies = ObjectManager.Get<Obj_AI_Hero>().Where(xEnemy => xEnemy.IsEnemy);
                var objAiHeroes = enemies as Obj_AI_Hero[] ?? enemies.ToArray();

                DrawText(TextBold, "Target Mode:", Drawing.Width*0.89f, Drawing.Height*0.55f, SharpDX.Color.White);
                var xSelect = Program.Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex;
                DrawText(
                    Text, xSelect == 0 ? "Single Target" : "Multi Targets", Drawing.Width*0.94f,
                    Drawing.Height*0.55f, SharpDX.Color.White);

                DrawText(TextBold, "Priority Targets", Drawing.Width*0.89f, Drawing.Height*0.58f, SharpDX.Color.White);
                DrawText(TextBold, "_____________", Drawing.Width*0.89f, Drawing.Height*0.58f, SharpDX.Color.White);

                for (var i = 0; i < objAiHeroes.Count(); i++)
                {
                    var xValue = Program.Config.Item("Assassin" + objAiHeroes[i].ChampionName).GetValue<bool>();
                    DrawText(
                        xValue ? TextBold : Text, objAiHeroes[i].ChampionName, Drawing.Width*0.895f,
                        Drawing.Height*0.58f + (float) (i + 1)*15,
                        xValue ? SharpDX.Color.GreenYellow : SharpDX.Color.DarkGray);
                }
            }

            var drawSearch = Program.Config.Item("DrawSearch").GetValue<Circle>();
            var drawActive = Program.Config.Item("DrawActive").GetValue<Circle>();
            var drawNearest = Program.Config.Item("DrawNearest").GetValue<Circle>();

            var drawSearchRange = Program.Config.Item("AssassinSearchRange").GetValue<Slider>().Value;
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, drawSearchRange, drawSearch.Color, 1);
            }

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(enemy => enemy.Team != ObjectManager.Player.Team)
                        .Where(
                            enemy =>
                                enemy.IsVisible &&
                                Program.Config.Item("Assassin" + enemy.ChampionName) != null &&
                                !enemy.IsDead)
                        .Where(
                            enemy => Program.Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>()))
            {
                if (ObjectManager.Player.Distance(enemy) < drawSearchRange)
                {
                    if (drawActive.Active)
                        Render.Circle.DrawCircle(enemy.Position, 115f, drawActive.Color, 1);
                }
                else if (ObjectManager.Player.Distance(enemy) > drawSearchRange &&
                         ObjectManager.Player.Distance(enemy) < drawSearchRange + 400)
                {
                    if (drawNearest.Active)
                        Render.Circle.DrawCircle(enemy.Position, 115f, drawNearest.Color, 1);
                }
            }
        }
    }
}