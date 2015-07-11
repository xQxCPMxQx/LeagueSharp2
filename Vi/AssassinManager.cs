using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Vi.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Vi
{
    internal class AssassinManager
    {
        private static Menu menu;
        public static Font Text, TextBold;
        private static readonly string vTab = "    ";

        private static Vector2 DrawPosition
        {
            get
            {
                if (KillableEnemy == null ||
                    !menu.Item("Draw.Sprite").GetValue<bool>())
                    return new Vector2(0f, 0f);

                return new Vector2(KillableEnemy.HPBarPosition.X + KillableEnemy.BoundingRadius / 2f,
                    KillableEnemy.HPBarPosition.Y - 50);
            }
        }

        private static bool DrawSprite
        {
            get { return true; }
        }

        private static Obj_AI_Hero KillableEnemy
        {
            get
            {
                var t = GetTarget(Program.E.Range);

                if (t.IsValidTarget())
                    return t;

                return null;
            }
        }

        public static void Initiliaze()
        {
            new Render.Sprite(Resources.selectedchampion, new Vector2())
            {
                PositionUpdate = () => DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();

            TextBold = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Calibri",
                    Height = 16,
                    Weight = FontWeight.Regular,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Calibri",
                    Height = 16,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            menu = new Menu(Program.ChampionName + " | Assassin Manager", "MenuAssassin");

            menu.AddItem(new MenuItem("AssassinActive", "Active").SetValue(true));
            menu.AddItem(new MenuItem("AssassinSearchRange", vTab + "Search Range")).SetValue(new Slider(1400, 2000));

            menu.AddItem(
                new MenuItem("AssassinSelectOption", vTab + "Set:").SetValue(
                    new StringList(new[] { "Single Select", "Multi Select" })));

            menu.AddItem(new MenuItem("xM1", "Enemies:"));
            foreach (var enemy in HeroManager.Enemies)
            {
                menu.AddItem(
                    new MenuItem("Assassin" + enemy.ChampionName, vTab + enemy.ChampionName).SetValue(
                        TargetSelector.GetPriority(enemy) > 3));
            }
            menu.AddItem(new MenuItem("xM2", "Other Settings:"));

            menu.AddItem(new MenuItem("AssassinSetClick", vTab + "Add/Remove with click").SetValue(true));
            menu.AddItem(
                new MenuItem("AssassinReset", vTab + "Reset List").SetValue(new KeyBind("T".ToCharArray()[0],
                    KeyBindType.Press)));

            menu.AddSubMenu(new Menu("Drawings", "Draw"));

            menu.SubMenu("Draw")
                .AddItem(new MenuItem("DrawSearch", "Search Range").SetValue(new Circle(true, Color.GreenYellow)));
            menu.SubMenu("Draw")
                .AddItem(new MenuItem("DrawActive", "Active Enemy").SetValue(new Circle(true, Color.GreenYellow)));
            menu.SubMenu("Draw")
                .AddItem(new MenuItem("DrawNearest", "Nearest Enemy").SetValue(new Circle(true, Color.DarkSeaGreen)));
            menu.SubMenu("Draw").AddItem(new MenuItem("DrawStatus", "Show status on the screen").SetValue(true));
            menu.SubMenu("Draw").AddItem(new MenuItem("DrawSprite", "Show Selected Enemy").SetValue(true));
            Program.Config.AddSubMenu(menu);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void ClearAssassinList()
        {
            foreach (
                var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                menu.Item("Assassin" + enemy.ChampionName).SetValue(false);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (menu.Item("AssassinReset").GetValue<KeyBind>().Active && args.Msg == 257)
            {
                ClearAssassinList();
                Game.PrintChat(
                    "<font color='#FFFFFF'>Reset Assassin List is Complete! Click on the enemy for Add/Remove.</font>");
            }

            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            if (menu.Item("AssassinSetClick").GetValue<bool>())
            {
                foreach (var objAiHero in from hero in HeroManager.Enemies
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
                            menu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                menu.Item("Assassin" + objAiHero.ChampionName).SetValue(true);
                                break;
                            case 1:
                                var menuStatus = menu.Item("Assassin" + objAiHero.ChampionName).GetValue<bool>();
                                menu.Item("Assassin" + objAiHero.ChampionName).SetValue(!menuStatus);
                                break;
                        }
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!menu.Item("AssassinActive").GetValue<bool>())
                return;

            if (menu.Item("DrawStatus").GetValue<bool>())
            {
                var enemies = ObjectManager.Get<Obj_AI_Hero>().Where(xEnemy => xEnemy.IsEnemy);
                var objAiHeroes = enemies as Obj_AI_Hero[] ?? enemies.ToArray();

                DrawText(TextBold, "Target Mode:", Drawing.Width * 0.89f, Drawing.Height * 0.55f, SharpDX.Color.White);
                var xSelect = menu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex;
                DrawText(
                    Text, xSelect == 0 ? "Single Target" : "Multi Targets", Drawing.Width * 0.94f,
                    Drawing.Height * 0.55f, SharpDX.Color.White);

                DrawText(TextBold, "Selected Target(s)", Drawing.Width * 0.89f, Drawing.Height * 0.58f, SharpDX.Color.White);
                DrawText(TextBold, "__________________", Drawing.Width * 0.89f, Drawing.Height * 0.58f, SharpDX.Color.White);

                for (var i = 0; i < objAiHeroes.Count(); i++)
                {
                    var xValue = menu.Item("Assassin" + objAiHeroes[i].ChampionName).GetValue<bool>();
                    DrawText(
                        xValue ? TextBold : Text, objAiHeroes[i].ChampionName, Drawing.Width * 0.895f,
                        Drawing.Height * 0.58f + (float)(i + 1) * 15,
                        xValue ? SharpDX.Color.GreenYellow : SharpDX.Color.DarkGray);
                }
            }

            var drawSearch = menu.Item("DrawSearch").GetValue<Circle>();
            var drawActive = menu.Item("DrawActive").GetValue<Circle>();
            var drawNearest = menu.Item("DrawNearest").GetValue<Circle>();

            var drawSearchRange = menu.Item("AssassinSearchRange").GetValue<Slider>().Value;
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, drawSearchRange, drawSearch.Color, 1);
            }

            foreach (
                var enemy in
                    HeroManager.Enemies
                        .Where(
                            enemy =>
                                enemy.IsVisible &&
                                menu.Item("Assassin" + enemy.ChampionName) != null &&
                                !enemy.IsDead)
                        .Where(
                            enemy => menu.Item("Assassin" + enemy.ChampionName).GetValue<bool>()))
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

        public static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = Program.E.Range;

            if (!menu.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = menu.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                HeroManager.Enemies.Where(
                    enemy =>
                        !enemy.IsDead && enemy.IsVisible && menu.Item("Assassin" + enemy.ChampionName) != null &&
                        menu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                        Program.Player.Distance(enemy) < assassinRange);

            if (menu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }
    }
}