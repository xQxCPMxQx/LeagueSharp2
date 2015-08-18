using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

using Color = System.Drawing.Color;

namespace JaxQx
{
    internal class AssassinManager
    {
        public static Menu LocalMenu;
        public static Font Text, TextBold, TextWarning;

        public AssassinManager()
        {
            new Utils();
            Load();
        }

        private static string MenuTab
        {
            get { return "    "; }
        }

        private static float SelectorRange
        {
            get { return Program.Config.Item("Enemies.SearchRange").GetValue<Slider>().Value; }
        }

        private static Obj_AI_Hero TsEnemy
        {
            get
            {
                var t = TargetSelector.GetTarget(SelectorRange, TargetSelector.DamageType.Physical);
                if (t == null)
                {
                    return null;
                }
                
                var vMax = HeroManager.Enemies.Where(
                    e =>
                        !e.IsDead && e.IsVisible && e.IsValidTarget(SelectorRange))
                    .Max(
                        h => LocalMenu.Item("Selected" + h.ChampionName).GetValue<StringList>().SelectedIndex);

                if (!double.IsNaN(vMax))
                {
                    var enemy = HeroManager.Enemies.Where(
                        e =>
                            !e.IsDead && e.IsVisible && e.IsValidTarget(SelectorRange) &&
                            LocalMenu.Item("Selected" + e.ChampionName).GetValue<StringList>().SelectedIndex == vMax);

                    return enemy.MinOrDefault(hero => hero.Health);
                }

                return null;
            }
        }

        private static void Load()
        {
            LocalMenu = new Menu("[ Jax Target Selector ]", "Enemies");

            Program.Config.AddSubMenu(LocalMenu);
            LocalMenu.AddItem(
                new MenuItem("Enemies.Mode", "Target Selector:").SetValue(new StringList(new[] { "L# Target Selector", "Jax Target Selector" }, 1)));
            LocalMenu.AddItem(new MenuItem("Enemies.Active", "Active").SetValue(true));
            LocalMenu.AddItem(new MenuItem("Enemies.SearchRange", MenuTab + "Enemy Searching Range"))
                .SetValue(new Slider(1000, 1500));

            LocalMenu.AddItem(new MenuItem("Enemies.Enemies.Title", "Enemies:", false, TextFontStyle.Bold));
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    LocalMenu.AddItem(new MenuItem("Selected" + enemy.ChampionName, MenuTab + enemy.CharData.BaseSkinName).SetValue(new StringList(new[] { "Low Focus", "Medium Focus", "High Focus" }, GetPriority(enemy.ChampionName))));
                }
            }

            LocalMenu.AddItem(new MenuItem("Enemies.Other.Title", "Other Settings:", false, TextFontStyle.Bold));
            {
                LocalMenu.AddItem(
                    new MenuItem("Enemies.AutoPriority Focus", MenuTab + "Auto arrange priorities").SetShared().SetValue(false))
                    .ValueChanged += AutoPriorityItemValueChanged;
            }
            LocalMenu.AddItem(
                new MenuItem("Enemies.Click", MenuTab + "Chance Enemy's Hitchance with Mouse Left-click").SetValue(true));

            LocalMenu.AddItem(new MenuItem("Draw.Title", "Drawings", false, TextFontStyle.Bold));
            {
                LocalMenu.AddItem(
                    new MenuItem("Draw.Search", MenuTab + "Show Search Range").SetValue(new Circle(true,
                        Color.GreenYellow)));
                LocalMenu.AddItem(new MenuItem("Draw.Status", MenuTab + "Show Targeting Status").SetValue(true));
                LocalMenu.AddItem(new MenuItem("Draw.Status.Show", MenuTab + MenuTab + "Show This:").SetValue(new StringList(new[] { "All", "Show Only High Enemies" })));
            }
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            LoadEnemyPriorityData();
        }

        private static void LoadEnemyPriorityData()
        {
            foreach (var enemy in HeroManager.Enemies)
            {
                LocalMenu.Item("Selected" + enemy.ChampionName).SetValue(new StringList(new[] { "Low Focus", "Medium Focus", "High Focus" }, GetPriority(enemy.ChampionName)));
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

            if (!Program.Config.Item("Enemies.Click").GetValue<bool>())
                return;

            var selectedTarget =
                HeroManager.Enemies.FindAll(
                    hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000)
                    .OrderBy(h => h.Distance(Game.CursorPos, true))
                    .FirstOrDefault();
            {
                if (selectedTarget == null || !selectedTarget.IsVisible)
                    return;

                var vSelected =
                    LocalMenu.Item("Selected" + selectedTarget.ChampionName)
                        .GetValue<StringList>()
                        .SelectedIndex;

                var i = vSelected == 2 ? 0 : vSelected + 1;

                LocalMenu.Item("Selected" + selectedTarget.ChampionName)
                    .SetValue(new StringList(new[] { "Low Focus", "Medium Focus", "High Focus" }, i));
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

            return highPriority.Contains(championName) ? 2 : 1;
        }

        public static void DrawLineInWorld(Vector3 start, Vector3 end, int width, Color color)
        {
            Drawing.DrawLine(start.X, start.Y, end.X, end.Y, width, color);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Config.Item("Enemies.Mode").GetValue<StringList>().SelectedIndex == 0)
                return;

            if (Program.Config.Item("Draw.Status").GetValue<bool>())
            {
                foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsVisible && !enemy.IsDead))
                {
                    var vSelected =
                        (LocalMenu.Item("Selected" + enemy.ChampionName).GetValue<StringList>().SelectedIndex);

                    if (LocalMenu.Item("Draw.Status.Show").GetValue<StringList>().SelectedIndex == 1 && vSelected != 2)
                        continue;

                    Utils.DrawText(vSelected == 2 ? Utils.TextBold : Utils.Text,
                        LocalMenu.Item("Selected" + enemy.CharData.BaseSkinName).GetValue<StringList>().SelectedValue,
                        enemy.HPBarPosition.X + enemy.BoundingRadius / 2f -
                        (enemy.CharData.BaseSkinName.Length / 2f),
                        enemy.HPBarPosition.Y - 20,
                        vSelected == 2
                            ? SharpDX.Color.Red
                            : (vSelected == 1 ? SharpDX.Color.Yellow : SharpDX.Color.Gray));
                }
            }
            var drawSearch = Program.Config.Item("Draw.Search").GetValue<Circle>();
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position,
                    SelectorRange, drawSearch.Color, 1);
            }
        }

        public Obj_AI_Hero GetTarget(float vRange = 0,
            TargetSelector.DamageType vDamageType = TargetSelector.DamageType.Physical)
        {
            return Math.Abs(vRange) < 0.00001 ? null : TsEnemy;
        }
    }
}
