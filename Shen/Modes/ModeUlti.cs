using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Shen.Common;
using Color = SharpDX.Color;

namespace Shen.Modes
{
    internal class ModeUlti
    {
        public static LeagueSharp.Common.Menu LocalMenu;
        private static string MenuTab => "    ";
        private static Spell W => Shen.Champion.PlayerSpells.W;
        private static Spell R => Shen.Champion.PlayerSpells.R;
        private static KeyBind ActiveConfirmKey => LocalMenu.Item("SpellR.ConfirmKey").GetValue<KeyBind>();
        private static Vector2 pingLocation;
        private static int lastPingTickCount = 0;
        private static PingCategory pingCategory = PingCategory.Fallback;

        public static Obj_AI_Hero GetHelplessAlly
        {
            get
            {
                IEnumerable<Obj_AI_Hero> vMax =
                    HeroManager.Allies.Where(
                        ally =>
                            !ally.IsDead && !ally.IsMe && !ally.InShop() && !ally.HasBuff("Recall") &&
                            ally.CountEnemiesInRange(ally.UnderAllyTurret() ? 550 : 350 + 350) > 0)
                        .Where(ally =>
                            LocalMenu.Item(ally.ChampionName + ".UseRWarning").GetValue<StringList>().SelectedIndex != 0)
                        .Where(ally =>
                            ally.HealthPercent <=
                            LocalMenu.Item(ally.ChampionName + ".UseRWarning").GetValue<StringList>().SelectedIndex*5)
                        .OrderByDescending(ally =>
                            LocalMenu.Item(ally.ChampionName + ".UseRPriority").GetValue<Slider>().Value);
                return vMax.FirstOrDefault();
            }
        }

        public static void Initialize(LeagueSharp.Common.Menu menuConfig)
        {
            LocalMenu = new LeagueSharp.Common.Menu("R Settings", "TeamMates").SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);

            //var menuImportant = new LeagueSharp.Common.Menu("Important Ally", "Menu.Important");
            
            //string[] strImportantAlly = new string[5];
            //strImportantAlly[0] = "No one are important!";

            //List<Obj_AI_Hero> allyList = HeroManager.Allies.Where(a => !a.IsMe).ToList();

            //for (int i = 0; i < allyList.Count; i++)
            //{
            //    strImportantAlly[i + 1] = allyList[i].CharData.BaseSkinName;
            //}

            //menuImportant.AddItem(new MenuItem("Important.Champion", "Ally Champion:").SetValue(new StringList(strImportantAlly, 0)).SetFontStyle(FontStyle.Regular, Color.GreenYellow));
            //menuImportant.AddItem(new MenuItem("Important.ShowStatus", "Show Important Ally HP Status").SetValue(new StringList(new []{"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, Color.Aqua));
            //menuImportant.AddItem(new MenuItem("Important.ShowPosition", "Show Important Ally Position in Team Fight").SetValue(new Circle(true, System.Drawing.Color.Aqua)).SetFontStyle(FontStyle.Regular, Color.Aqua));

            //LocalMenu.AddSubMenu(menuImportant);

            foreach (var ally in HeroManager.Allies.Where(a => !a.IsMe))
            {
                var menuAlly = new LeagueSharp.Common.Menu(ally.ChampionName, "Ally." + ally.ChampionName).SetFontStyle(FontStyle.Regular, SharpDX.Color.Coral);
                { 
                    menuAlly.AddItem(new MenuItem(ally.ChampionName + ".UseW", "W: Auto Protection").SetValue(new StringList(new[] {"Don't Use", "Use if he need"}, GetProtection(ally.ChampionName) >= 3 ? 1 : 0)).SetFontStyle(FontStyle.Regular, W.MenuColor()));

                    string[] strR = new string[15];
                    strR[0] = "Off";

                    for (var i = 1; i < 15; i++)
                    {
                        strR[i] = "if hp <= % " + (i * 5);
                    }
                    menuAlly.AddItem(new MenuItem(ally.ChampionName + ".UseRWarning", "R: Warn me:").SetValue(new StringList(strR, GetProtection(ally.ChampionName) == 1 ? 2 : (GetProtection(ally.ChampionName) == 2 ? 5 : 8))).SetFontStyle(FontStyle.Regular, R.MenuColor()));
                    menuAlly.AddItem(new MenuItem(ally.ChampionName + ".UseRConfirm", "R: Confirm:").SetValue(new StringList(new[] { "I'll Confirm with Confirmation Key!", "Auto Use Ultimate!" }, GetProtection(ally.ChampionName) >= 3 ? 1 : 0)).SetFontStyle(FontStyle.Regular, R.MenuColor()));
                    menuAlly.AddItem(new MenuItem(ally.ChampionName + ".UseRPriority", "R: Priority:").SetValue(new Slider(GetProtection(ally.ChampionName) + 2, 1, 5)).SetFontStyle(FontStyle.Regular, R.MenuColor()));
                }
                LocalMenu.AddSubMenu(menuAlly);
            }

            string[] strHpBarStatus = new[] {"Off", "Priority >= 1", "Priority >= 2", "Priority >= 3", "Priority >= 4", "Priority = 5"};
            
            LocalMenu.AddItem(new MenuItem("SpellR.DrawHPBarStatus", "Show HP Bar Status").SetValue(new StringList(strHpBarStatus, 3)).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
            LocalMenu.AddItem(new MenuItem("SpellR.WarnNotificationText", "Warn me with notification text").SetValue(new StringList(strHpBarStatus, 3)).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
            LocalMenu.AddItem(new MenuItem("SpellR.WarnPingAlly", "Warn me with local ping").SetValue(new StringList(new []{"Off", "Danger", "Fallback"}, 2)));

            string[] strAutoUltimate = new string[15];
            strAutoUltimate[0] = "Off";

            for (var i = 1; i < 15; i++)
            {
                strAutoUltimate[i] = "If my hp >= % " + (i * 5);
            }
            LocalMenu.AddItem(new MenuItem("SpellR.AutoUltimate", "Auto Ulti Condition:").SetValue(new StringList(strAutoUltimate , 8)).SetFontStyle(FontStyle.Regular, SharpDX.Color.IndianRed));
            LocalMenu.AddItem(new MenuItem("SpellR.ConfirmKey", "Ulti Confirm Key!").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(FontStyle.Bold, SharpDX.Color.GreenYellow));

            menuConfig.AddSubMenu(LocalMenu);
            
            
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnDraw += DrawingOnOnDrawUlti;
            Game.OnUpdate += GameOnOnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        private static void WProtection()
        {
            foreach (var ally in HeroManager.Allies.Where(ally => ally.IsMe && ally.IsDead && LocalMenu.Item(ally.ChampionName + ".UseW").GetValue<StringList>().SelectedIndex == 1))
            {
                
            }
        }
        private static void DrawingOnOnDrawUlti(EventArgs args)
        {
            if (!R.IsReady())
            {
                return;
            }

            var ally = GetHelplessAlly;
            if (ally != null)
            {
                var allyConfirmUltimate = LocalMenu.Item(ally.ChampionName + ".UseRConfirm").GetValue<StringList>().SelectedIndex;
                //if (allyConfirmUltimate == 1 && R.IsReady() && ObjectManager.Player.HealthPercent >= LocalMenu.Item("SpellR.AutoUltimate").GetValue<StringList>().SelectedIndex * 5)
                if (LocalMenu.Item("SpellR.WarnNotificationText").GetValue<StringList>().SelectedIndex != 0)
                {
                    if (allyConfirmUltimate == 1 && R.IsReady())
                    {
                        if (Modes.MenuConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                        {
                            DrawWarningMessage(args, "AUTO ULTIMATE: " + ally.ChampionName, Color.GreenYellow);
                            R.CastOnUnit(ally);
                        }
                        else
                        {
                            var warningText = "Press " + char.ConvertFromUtf32((int)ActiveConfirmKey.Key) + " for Ulti: " + ally.ChampionName;
                            DrawWarningMessage(args, warningText, Color.Red);
                        }
                    }
                    else 
                    {
                        var warningText = "Press " + char.ConvertFromUtf32((int) ActiveConfirmKey.Key) + " for Ulti: " + ally.ChampionName;
                        DrawWarningMessage(args, warningText, Color.Red);
                    }
                }

                if (LocalMenu.Item("SpellR.WarnPingAlly").GetValue<StringList>().SelectedIndex != 0)
                {
                    switch (LocalMenu.Item("SpellR.WarnPingAlly").GetValue<StringList>().SelectedIndex)
                    {
                        case 1:
                            {
                                pingCategory = PingCategory.Danger;
                                break;
                            }
                        case 2:
                            {
                                pingCategory = PingCategory.Fallback;
                                break;
                            }
                    }

                    Ping(ally.Position.To2D());
                }
            }
        }

        private static void DrawWarningMessage(EventArgs args, string message = "", SharpDX.Color color = default (Color))
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            var xColor = color;
            DrawHelper.DrawText(DrawHelper.TextWarning, message, Drawing.Width*0.301f, Drawing.Height*0.422f,
                SharpDX.Color.Black);
            DrawHelper.DrawText(DrawHelper.TextWarning, message, Drawing.Width*0.30f, Drawing.Height*0.42f,
                xColor);
        }

        public static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe && sender.IsEnemy && sender is Obj_AI_Hero && args.Target.IsAlly && args.Target is Obj_AI_Hero && !args.Target.IsMe)
            {
                var ally = args.Target as Obj_AI_Hero;
                if (!ally.IsDead)
                {
                    if (W.IsReady() &&
                        LocalMenu.Item(ally.ChampionName + ".UseW").GetValue<StringList>().SelectedIndex == 1 &&
                        ally.NetworkId == args.Target.NetworkId &&
                        ally.Position.Distance(Shen.Champion.SpiritUnit.SwordUnit.Position) < 350)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
        }

       private static void Drawing_OnDraw(EventArgs args)
        {
            if (LocalMenu.Item("SpellR.DrawHPBarStatus").GetValue<StringList>().SelectedIndex != 0)
            {
                var allies =
                    HeroManager.Allies.Where(
                        a =>
                            !a.IsMe &&
                            LocalMenu.Item("SpellR.DrawHPBarStatus").GetValue<StringList>().SelectedIndex <=
                            LocalMenu.Item(a.ChampionName + ".UseRPriority").GetValue<Slider>().Value);
                var objAiHeroes = allies as Obj_AI_Hero[] ?? allies.ToArray();

                for (var i = 0; i < objAiHeroes.Count(); i++)
                {
                    var x = 0.792f;
                    var y = 0.795f;
                    var width = 160;

                    var allyConfirmUltimate = LocalMenu.Item(objAiHeroes[i].ChampionName + ".UseRConfirm").GetValue<StringList>().SelectedIndex;

                    Drawing.DrawLine(Drawing.Width * x + 0, Drawing.Height * 0.479f + (float)(i + 1) * 18, Drawing.Width * y + width, Drawing.Height * 0.479f + (float)(i + 1) * 18, 16, System.Drawing.Color.DarkSlateGray);
                    Drawing.DrawLine(Drawing.Width * x + 1, Drawing.Height * 0.480f + (float)(i + 1) * 18, Drawing.Width * y + width - 1, Drawing.Height * 0.480f + (float)(i + 1) * 18, 14, System.Drawing.Color.Gray);

                    var hPercent = (int)Math.Ceiling((objAiHeroes[i].Health * width / objAiHeroes[i].MaxHealth));
                    if (hPercent > 0)
                    {
                        Drawing.DrawLine(Drawing.Width * x + 1, Drawing.Height * 0.480f + (float)(i + 1) * 18,
                            Drawing.Width * y + hPercent - 1, Drawing.Height * 0.480f + (float)(i + 1) * 18,
                            14, hPercent < 50 && hPercent > 30 ? System.Drawing.Color.Yellow : hPercent <= 30 ? System.Drawing.Color.Red : System.Drawing.Color.DarkOliveGreen);
                    }

                    DrawHelper.DrawText(DrawHelper.Text, objAiHeroes[i].ChampionName + " [R: " + (allyConfirmUltimate == 0 ? "Key confirm" :  "Auto Ulti") + "]", Drawing.Width * y, Drawing.Height * 0.48f + (float)(i + 1) * 18, SharpDX.Color.Black);
                    //Utils.DrawText(Utils.Text, objAiHeroes[i].ChampionName + ": " + hPercent + "%", Drawing.Width * y, Drawing.Height * 0.48f + (float)(i + 1) * 20, SharpDX.Color.Black);
                }
            }
        }
        public static int GetProtection(string championName)
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
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven", "Ezreal",
                "Graves", "Jhin", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                "Zed", "Ziggs"
            };

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

        private static void Ping(Vector2 position)
        {
            if (Utils.TickCount - lastPingTickCount < 30 * 1000)
            {
                return;
            }

            lastPingTickCount = Utils.TickCount;
            pingLocation = position;
            SimplePing();

            Utility.DelayAction.Add(200, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
            Utility.DelayAction.Add(600, SimplePing);
            Utility.DelayAction.Add(800, SimplePing);
        }

        private static void SimplePing()
        {
            Game.ShowPing(pingCategory, pingLocation, true);
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
