using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

namespace XinZhao
{
    internal class AssassinManager
    {
        public static Menu LocalMenu;
        private static Font text, textBold;

        private static string MenuTab { get { return "    "; } }

        private static float SelectorRange
        {
            get { return Program.Config.Item("TS.SearchRange").GetValue<Slider>().Value; }
        }

        public enum TargetingMode
        {
            LowHp,
            MostAd,
            MostAp,
            Closest,
            NearMouse,
            LessAttack,
            LessCast
        }

        public AssassinManager()
        {
            Load();
        }

        private static void Load()
        {
            text = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 13,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            textBold = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 13,
                    Weight = FontWeight.Bold,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });


            LocalMenu = new Menu("XinZhao | Target Selector", "TSxQx");

            Program.Config.AddSubMenu(LocalMenu);
            LocalMenu.AddItem(new MenuItem("TS.Mode", "Selector Mode:").SetValue(new StringList(new[] { "L# Target Selector", "XinZhao Target Selector" }, 1)));
            LocalMenu.AddItem(new MenuItem("TS.Active", "Active").SetValue(true));
            LocalMenu.AddItem(new MenuItem("TS.SearchRange", MenuTab + "Enemy Searching Range")).SetValue(new Slider(950, 1500));

            LocalMenu.AddItem(new MenuItem("TS.Enemies.Title", "Enemies:"));
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    LocalMenu.AddItem(
                        new MenuItem("Selected" + enemy.ChampionName, MenuTab + enemy.CharData.BaseSkinName)
                            .SetValue(new StringList(new[] { "Low Target", "Medium Target", "High Target" },
                                GetPriority(enemy.ChampionName))));
                }
            }

            LocalMenu.AddItem(new MenuItem("TS.Other.Title", "Other Settings:"));
            {
                LocalMenu.AddItem(new MenuItem("TS.AutoPriority", MenuTab + "Auto arrange priorities").SetShared().SetValue(false))
                    .ValueChanged += AutoPriorityItemValueChanged;
            }
            LocalMenu.AddItem(new MenuItem("TargetingMode", MenuTab + "Target Mode").SetShared().SetValue(new StringList(Enum.GetNames(typeof(TargetingMode)))));
            LocalMenu.AddItem(new MenuItem("TS.Click", MenuTab + "Chance Enemy's Hitchance with Mouse Left-click").SetValue(true));

            LocalMenu.AddItem(new MenuItem("Draw.Title", "Drawings"));
            {
                LocalMenu.AddItem(new MenuItem("Draw.Search", MenuTab + "Show Search Range").SetValue(new Circle(true, Color.GreenYellow)));
                LocalMenu.AddItem(new MenuItem("Draw.Status", MenuTab + "Show Targeting Status").SetValue(true));
                LocalMenu.AddItem(new MenuItem("Draw.Status.Show", MenuTab + MenuTab + "Show This:").SetValue(new StringList(new[] { "All", "Just High Target Enemies" })));
            }
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            LoadEnemyPriorityData();
        }

        private static void OnUpdate(EventArgs args)
        {
            return;
            foreach (
                var enemy in
                    HeroManager.Enemies.Where(enemy => !enemy.IsDead)
                        .OrderByDescending(
                            h => LocalMenu.Item("Selected" + h.ChampionName).GetValue<StringList>().SelectedIndex)
                        .ThenBy(h => h.Health))
            {
                var vSelected = (LocalMenu.Item("Selected" + enemy.ChampionName).GetValue<StringList>().SelectedIndex);
                var i = vSelected == 0 ? 1 : vSelected + 1;
                Console.WriteLine(enemy.ChampionName + ": " + i);
            }
            Console.WriteLine(@"--------------------------------------------------");

        }

        public static void DrawText(Font vFont, String vText, float vPosX, float vPosY, SharpDX.ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }

        private static void LoadEnemyPriorityData()
        {
            foreach (var enemy in HeroManager.Enemies)
            {
                LocalMenu.Item("Selected" + enemy.ChampionName)
                        .SetValue(new StringList(new[] { "Low Target", "Medium Target", "High Target" },
                            GetPriority(enemy.ChampionName)));
            }
        }

        private static void AutoPriorityItemValueChanged(object sender, OnValueChangeEventArgs e)
        {
            if (!e.GetNewValue<bool>())
                return;

            LoadEnemyPriorityData();
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
                return;

            if (Program.Config.Item("TS.Click").GetValue<bool>())
            {
                var selectedTarget =
                    HeroManager.Enemies.FindAll(
                        hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000)
                        .OrderBy(h => h.Distance(Game.CursorPos, true))
                        .FirstOrDefault();
                {
                    if (selectedTarget != null && selectedTarget.IsVisible)
                    {
                        var vSelected =
                            LocalMenu.Item("Selected" + selectedTarget.ChampionName)
                                .GetValue<StringList>()
                                .SelectedIndex;

                        var i = vSelected == 2 ? 0 : vSelected + 1;

                        LocalMenu.Item("Selected" + selectedTarget.ChampionName)
                            .SetValue(new StringList(new[] { "Low Target", "Medium Target", "High Target" }, i));
                    }
                }
            }
        }

        private static int GetPriority(string championName)
        {
            string[] lowPriority =
            {
                "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                "Soraka", "Tahm", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
            };

            string[] mediumPriority =
            {
                "Aatrox", "Akali", "Darius", "Diana", "Ekko", "Elise", "Evelynn", "Fiddlesticks", "Fiora", "Fizz",
                "Galio", "Gangplank", "Gragas", "Heimerdinger", "Irelia", "Jax", "Jayce", "Kassadin", "Kayle", "Kha'Zix",
                "Lee Sin", "Lissandra", "Maokai", "Mordekaiser", "Morgana", "Nocturne", "Nidalee", "Pantheon", "Poppy",
                "RekSai", "Rengar", "Riven", "Rumble", "Ryze", "Shaco", "Swain", "Trundle", "Tryndamere", "Udyr",
                "Urgot", "Vladimir", "Vi", "XinZhao", "Yasuo", "Zilean"
            };

            string[] highPriority =
            {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                "Zed", "Ziggs"
            };

            if (lowPriority.Contains(championName))
            {
                return 0;
            }
            if (mediumPriority.Contains(championName))
            {
                return 1;
            }
            if (highPriority.Contains(championName))
            {
                return 2;
            }
            return 1;
        }

        public static void DrawLineInWorld(Vector3 start, Vector3 end, int width, Color color)
        {
            Drawing.DrawLine(start.X, start.Y, end.X, end.Y, width, color);
            //Drawing.DrawLine(from.X, from.Y, to.X, to.Y, width, color);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Config.Item("TS.Mode").GetValue<StringList>().SelectedIndex == 0)
                return;

            if (Program.Config.Item("Draw.Status").GetValue<bool>())
            {
                foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsVisible && !enemy.IsDead))
                {
                    var vSelected =
                        (LocalMenu.Item("Selected" + enemy.ChampionName).GetValue<StringList>().SelectedIndex);


                    /*
                for (int j = 1; j < vSelected; j++)
                {
                    if (j%5 == 0)
                    {
                        Drawing.DrawLine(enemy.HPBarPosition.X + (j*5), enemy.HPBarPosition.Y - 30,
                            enemy.HPBarPosition.X + (j*5) + 20, enemy.HPBarPosition.Y - 30, 3,
                            j == 5 ? Color.Chartreuse : j == 10 ? Color.Yellow : Color.Red);

                        Drawing.DrawLine(
                            enemy.HPBarPosition.X + 145, (float) (enemy.HPBarPosition.Y + (j*1.3) + 15),
                            enemy.HPBarPosition.X + 160, (float) (enemy.HPBarPosition.Y + (j*1.3) + 15), 3,
                            j == 5 ? Color.Chartreuse : j == 10 ? Color.Yellow : Color.Red);
                    }
                }
                */
                    if (LocalMenu.Item("Draw.Status.Show").GetValue<StringList>().SelectedIndex == 1 && vSelected != 2)
                    {
                        return;
                    }

                    DrawText(vSelected == 2 ? textBold : text,
                        LocalMenu.Item("Selected" + enemy.CharData.BaseSkinName).GetValue<StringList>().SelectedValue,
                        enemy.HPBarPosition.X + enemy.BoundingRadius / 2f -
                        (enemy.CharData.BaseSkinName.Length / 2f),
                        enemy.HPBarPosition.Y - 20,
                        vSelected == 2
                            ? SharpDX.Color.Red
                            : (vSelected == 1 ? SharpDX.Color.Yellow : SharpDX.Color.Chartreuse));

                    /*
                Drawing.DrawText(
                    enemy.HPBarPosition.X + enemy.BoundingRadius/2f - (enemy.CharData.BaseSkinName.Length/2f),
                    enemy.HPBarPosition.Y - 20,
                    GetPriority(enemy.ChampionName) == 2
                        ? System.Drawing.Color.Red
                        : (GetPriority(enemy.ChampionName) == 1 ? System.Drawing.Color.Yellow : System.Drawing.Color.Chartreuse),
                    LocalMenu.Item("Selected" + enemy.CharData.BaseSkinName).GetValue<StringList>().SelectedValue);
                 */
                }
            }
            var drawSearch = Program.Config.Item("Draw.Search").GetValue<Circle>();
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position,
                    SelectorRange, drawSearch.Color, 1);
            }
        }
        public Obj_AI_Hero GetTarget(float vRange = 0, TargetSelector.DamageType vDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vRange) < 0.00001)
                return null;

            switch (Program.Config.Item("TS.Mode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return TargetSelector.GetTarget(vRange, vDamageType);

                case 1:
                    return TsEnemy;
            }

            return null;
        }

        private static Obj_AI_Hero TsEnemy
        {
            get
            {
                var vMax = HeroManager.Enemies.Where(
                    e =>
                        !e.IsDead && e.IsVisible && e.IsValidTarget(SelectorRange))
                    .Max(
                        h => LocalMenu.Item("Selected" + h.ChampionName).GetValue<StringList>().SelectedIndex);

                if (!Double.IsNaN(vMax))
                {
                    var enemy = HeroManager.Enemies.Where(
                        e =>
                            !e.IsDead && e.IsVisible && e.IsValidTarget(SelectorRange) &&
                            LocalMenu.Item("Selected" + e.ChampionName).GetValue<StringList>().SelectedIndex == vMax);

                    TargetingMode targettinMode;
                    var menuItem = LocalMenu.Item("TargetingMode").GetValue<StringList>();
                    Enum.TryParse(menuItem.SList[menuItem.SelectedIndex], out targettinMode);

                    switch (targettinMode)
                    {
                        case TargetingMode.LowHp:
                            return enemy.MinOrDefault(hero => hero.Health);

                        case TargetingMode.MostAd:
                            return enemy.MaxOrDefault(hero => hero.BaseAttackDamage + hero.FlatPhysicalDamageMod);

                        case TargetingMode.MostAp:
                            return enemy.MaxOrDefault(hero => hero.BaseAbilityDamage + hero.FlatMagicDamageMod);

                        case TargetingMode.Closest:
                            return
                                enemy.MinOrDefault(
                                    hero => (ObjectManager.Player.ServerPosition).Distance(hero.ServerPosition, true));

                        case TargetingMode.NearMouse:
                            return enemy.Find(hero => hero.Distance(Game.CursorPos, true) < 22500); // 150 * 150

                        case TargetingMode.LessAttack:
                            return
                                enemy.MaxOrDefault(
                                    hero =>
                                        ObjectManager.Player.CalcDamage(hero, Damage.DamageType.Physical, 100) /
                                        (1 + hero.Health) *
                                        GetPriority(hero));

                        case TargetingMode.LessCast:
                            return
                                enemy.MaxOrDefault(
                                    hero =>
                                        ObjectManager.Player.CalcDamage(hero, Damage.DamageType.Magical, 100) /
                                        (1 + hero.Health) *
                                        GetPriority(hero));
                    }
                }

                /*
                var enemy = HeroManager.Enemies.Where(
                    e =>
                        !e.IsDead && e.IsVisible && e.IsValidTarget(SelectorRange))
                    .OrderByDescending(
                        h => LocalMenu.Item("Selected" + h.ChampionName).GetValue<StringList>().SelectedIndex).Max();                
                */
                return null;
            }
        }

        private static double GetPriority(Obj_AI_Hero hero)
        {
            throw new NotImplementedException();
        }

    }
}