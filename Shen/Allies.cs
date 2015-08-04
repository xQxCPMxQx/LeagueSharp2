using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

namespace Shen
{
    internal class Allies
    {
        public static Menu LocalMenu;

        private static string MenuTab { get { return "    "; } }

        public Allies()
        {
            Load();
        }

        private static void Load()
        {

            LocalMenu = new Menu("[ Protector Settings ]", "Allies");

            Program.Config.AddSubMenu(LocalMenu);
           LocalMenu.AddItem(new MenuItem("ChampionAllies.ChampionAllies.Title", "Protection Status:"));
            {
                foreach (var ally in HeroManager.Allies.Where(a => !a.IsMe))
                {
                    LocalMenu.AddItem(new MenuItem("Selected.Champ" + ally.ChampionName, MenuTab + ally.ChampionName).SetValue(new StringList(new[] {"Off", "Low Protection", "Medium Protection", "High Protection"}, GetProtection(ally.ChampionName))));
                }
            }
            LocalMenu.AddItem(new MenuItem("Allies.Ulti", "Use Ulti for :").SetValue(new StringList(new[] {"All", "Only High Protection Allies"})));

            LocalMenu.AddItem(new MenuItem("ChampionAllies.Other.Title", "Other Settings:"));
            {
                LocalMenu.AddItem(new MenuItem("ChampionAllies.AutoProtection", MenuTab + "Auto Arrange Protection").SetShared().SetValue(false)).ValueChanged += AutoProtectionItemValueChanged;
            }
            LocalMenu.AddItem(new MenuItem("ChampionAllies.Click", MenuTab + "Mouse LEFT-CLICK: Change Ally's Protection Stauts").SetValue(true));

            LocalMenu.AddItem(new MenuItem("Draw.Title", "Drawings"));
            {
                LocalMenu.AddItem(new MenuItem("Draw.Status", MenuTab + "Show Targeting Status").SetValue(true));
                LocalMenu.AddItem(new MenuItem("Draw.Status.Just", MenuTab + "Just Show This: ").SetValue(new StringList(new[] {"All", "Only High Protection Allies"})));
                LocalMenu.AddItem(new MenuItem("Draw.Notification", MenuTab + "Show Notification Text").SetValue(true));
            }
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            LoadEnemyProtectionData();
        }

        private static void OnUpdate(EventArgs args)
        {

        }

        private static void LoadEnemyProtectionData()
        {
            foreach (var enemy in HeroManager.Allies)
            {
                LocalMenu.Item("Selected.Champ" + enemy.ChampionName)
                        .SetValue(new StringList(new[] { "Off", "Low Protection", "Medium Protection", "High Protection" },
                            GetProtection(enemy.ChampionName)));
            }
        }

        private static void AutoProtectionItemValueChanged(object sender, OnValueChangeEventArgs e)
        {
            if (!e.GetNewValue<bool>())
                return;

            LoadEnemyProtectionData();
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
                return;

            if (Program.Config.Item("ChampionAllies.Click").GetValue<bool>())
            {
                var selectedTarget =
                    HeroManager.Allies.FindAll(
                        hero => hero.Distance(Game.CursorPos, true) < 40000 && !hero.IsMe && !hero.IsDead)
                        .OrderBy(a => a.Distance(Game.CursorPos, true))
                        .FirstOrDefault();
                {
                    if (selectedTarget != null && selectedTarget.IsVisible)
                    {
                        var vSelected =
                            LocalMenu.Item("Selected.Champ" + selectedTarget.ChampionName)
                                .GetValue<StringList>()
                                .SelectedIndex;

                        var i = vSelected == 3 ? 0 : vSelected + 1;

                        LocalMenu.Item("Selected.Champ" + selectedTarget.ChampionName)
                            .SetValue(new StringList(new[] { "Off", "Low Protection", "Medium Protection", "High Protection" }, i));
                    }
                }
            }
        }

        private static int GetProtection(string championName)
        {
            string[] lowProtection =
            {
                "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                "Soraka", "Tahm", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
            };

            string[] mediumProtection =
            {
                "Aatrox", "Akali", "Darius", "Diana", "Ekko", "Elise", "Evelynn", "Fiddlesticks", "Fiora", "Fizz",
                "Galio", "Gangplank", "Gragas", "Heimerdinger", "Irelia", "Jax", "Jayce", "Kassadin", "Kayle", "Kha'Zix",
                "Lee Sin", "Lissandra", "Maokai", "Mordekaiser", "Morgana", "Nocturne", "Nidalee", "Pantheon", "Poppy",
                "RekSai", "Rengar", "Riven", "Rumble", "Ryze", "Shaco", "Swain", "Trundle", "Tryndamere", "Udyr",
                "Urgot", "Vladimir", "Vi", "XinZhao", "Yasuo", "Zilean"
            };

            string[] highProtection =
            {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                "Zed", "Ziggs"
            };

            if (lowProtection.Contains(championName))
            {
                return 1;
            }
            if (mediumProtection.Contains(championName))
            {
                return 2;
            }
            if (highProtection.Contains(championName))
            {
                return 3;
            }
            return 1;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (LocalMenu.Item("Draw.Status").GetValue<bool>())
            {
                foreach (var a in HeroManager.Allies.Where(a => a.IsVisible && !a.IsDead && !a.IsMe))
                {
                    var vSelected =
                        (LocalMenu.Item("Selected.Champ" + a.ChampionName).GetValue<StringList>().SelectedIndex);

                    //if ((LocalMenu.Item("Draw.Status.Show").GetValue<StringList>().SelectedIndex == 1 && vSelected != 3) || vSelected == 0)
                    //    continue;
                    
                    if (vSelected != 0)
                    {
                        Utils.DrawText(vSelected == 3 ? Utils.TextBold : Utils.Text,
                            LocalMenu.Item("Selected.Champ" + a.CharData.BaseSkinName)
                                .GetValue<StringList>()
                                .SelectedValue,
                            a.HPBarPosition.X + a.BoundingRadius/2f -
                            (a.CharData.BaseSkinName.Length/2f),
                            a.HPBarPosition.Y - 20,
                            vSelected == 3
                                ? SharpDX.Color.Red
                                : (vSelected == 1 &&
                                   LocalMenu.Item("Draw.Status.Just").GetValue<StringList>().SelectedIndex == 1
                                    ? SharpDX.Color.Yellow
                                    : SharpDX.Color.Gray));
                    }
                }
            }
        }

        public static Obj_AI_Hero GetAlly
        {
            get
            {
                var vMax = HeroManager.Allies.Where(
                    a =>
                        !a.IsDead && !a.IsMe && a.CountEnemiesInRange(UnderAllyTurret(a) ? 300 : 300 + 300) > 0 && a.Health < 400)
                    .Max(
                        a => LocalMenu.Item("Selected.Champ" + a.ChampionName).GetValue<StringList>().SelectedIndex);

                if (!Double.IsNaN(vMax))
                {
                    var ally = HeroManager.Allies.Where(
                        a => !a.IsDead && !a.IsMe &&
                             LocalMenu.Item("Selected.Champ" + a.ChampionName).GetValue<StringList>().SelectedIndex <= vMax);

                    return ally.MinOrDefault(hero => hero.Health);
                }

                return null;
            }
        }
        public static bool UnderAllyTurret(Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Where<Obj_AI_Turret>(turret =>
            {
                if (turret == null || !turret.IsValid || turret.Health <= 0f)
                {
                    return false;
                }
                if (!turret.IsEnemy)
                {
                    return true;
                }
                return false;
            })
                .Any<Obj_AI_Turret>(
                    turret =>
                        Vector2.Distance(unit.Position.To2D(), turret.Position.To2D()) < 900f && turret.IsAlly);
        }
    }
}