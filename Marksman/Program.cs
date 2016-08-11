#region

using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Champions;
using Marksman.Utils;
using SharpDX;
using SharpDX.Direct3D9;
using Activator = Marksman.Utils.Activator;


#endregion

namespace Marksman
{
    using System.Collections.Generic;

    using Color = SharpDX.Color;

    internal class Program
    {
        public static Menu Config;

        public static Menu OrbWalking;

        public static Menu QuickSilverMenu;

        public static Menu MenuActivator;

        public static Champion CClass;

        public static Activator AActivator;

        public static Utils.AutoLevel AutoLevel;

        public static AutoPink AutoPink;

        public static AutoBushRevealer AutoBushRevealer;


        //public static Utils.EarlyEvade EarlyEvade;

        public static double ActivatorTime;

        private static float AsmLoadingTime = 0;

        public static Spell Smite;

        public static SpellSlot SmiteSlot = SpellSlot.Unknown;

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };

        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };

        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };

        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("Marksman", "Marksman", true).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
            CClass = new Champion();

            var BaseType = CClass.GetType();

            /* Update this with Activator.CreateInstance or Invoke
               http://stackoverflow.com/questions/801070/dynamically-invoking-any-function-by-passing-function-name-as-string 
               For now stays cancer.
             */
            var championName = ObjectManager.Player.ChampionName.ToLowerInvariant();

            switch (championName)
            {
                case "ashe":
                    CClass = new Ashe();
                    break;
                case "caitlyn":
                    CClass = new Caitlyn();
                    break;
                case "corki":
                    CClass = new Corki();
                    break;
                case "draven":
                    CClass = new Draven();
                    break;
                case "ezreal":
                    CClass = new Ezreal();
                    break;
                case "graves":
                    CClass = new Graves();
                    break;
                case "gnar":
                    CClass = new Gnar();
                    break;
                case "jinx":
                    CClass = new Jinx();
                    break;
                case "kalista":
                    CClass = new Kalista();
                    break;
                case "kindred":
                    CClass = new Kindred();
                    break;
                case "kogmaw":
                    CClass = new Kogmaw();
                    break;
                case "lucian":
                    CClass = new Lucian();
                    break;
                case "missfortune":
                    CClass = new MissFortune();
                    break;
                case "quinn":
                    CClass = new Quinn();
                    break;
                case "sivir":
                    CClass = new Sivir();
                    break;
                case "teemo":
                    CClass = new Teemo();
                    break;
                case "tristana":
                    CClass = new Tristana();
                    break;
                case "twitch":
                    CClass = new Twitch();
                    break;
                case "urgot":
                    CClass = new Urgot();
                    break;
                case "vayne":
                    CClass = new Vayne();
                    break;
                case "varus":
                    CClass = new Varus();
                    break;
            }
            Config.DisplayName = "Marksman | " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(championName);

            CClass.Id = ObjectManager.Player.CharData.BaseSkinName;
            CClass.Config = Config;

            OrbWalking = Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            CClass.Orbwalker = new Orbwalking.Orbwalker(OrbWalking);

            OrbWalking.AddItem(new MenuItem("Orb.AutoWindUp", "Marksman - Auto Windup").SetValue(false)).ValueChanged +=
                (sender, argsEvent) => { if (argsEvent.GetNewValue<bool>()) CheckAutoWindUp(); };
            AActivator = new Activator();

            MenuActivator = new Menu("Activator", "Activator").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
            {
                AutoLevel = new Utils.AutoLevel();
                AutoPink = new Utils.AutoPink();
                AutoPink.Initialize();
                IncomingDangerous.Initialize();
                //ExecutedTime.Initialize();
                AutoBushRevealer = new AutoBushRevealer();
                //EarlyEvade = new Utils.EarlyEvade();
                //MenuActivator.AddSubMenu(EarlyEvade.MenuLocal);

                /* Menu Items */
                var items = MenuActivator.AddSubMenu(new Menu("Items", "Items"));
                items.AddItem(new MenuItem("BOTRK", "BOTRK").SetValue(true));
                items.AddItem(new MenuItem("GHOSTBLADE", "Ghostblade").SetValue(true));
                items.AddItem(new MenuItem("SWORD", "Sword of the Divine").SetValue(true));
                items.AddItem(new MenuItem("MURAMANA", "Muramana").SetValue(true));
                QuickSilverMenu = new Menu("QSS", "QuickSilverSash");
                items.AddSubMenu(QuickSilverMenu);
                QuickSilverMenu.AddItem(new MenuItem("AnyStun", "Any Stun").SetValue(true));
                QuickSilverMenu.AddItem(new MenuItem("AnySlow", "Any Slow").SetValue(true));
                QuickSilverMenu.AddItem(new MenuItem("AnySnare", "Any Snare").SetValue(true));
                QuickSilverMenu.AddItem(new MenuItem("AnyTaunt", "Any Taunt").SetValue(true));
                foreach (var t in AActivator.BuffList)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                    {
                        if (t.ChampionName == enemy.ChampionName)
                            QuickSilverMenu.AddItem(new MenuItem(t.BuffName, t.DisplayName).SetValue(t.DefaultValue));
                    }
                }
                items.AddItem(
                    new MenuItem("UseItemsMode", "Use items on").SetValue(
                        new StringList(new[] { "No", "Mixed mode", "Combo mode", "Both" }, 2)));

                new PotionManager(MenuActivator);

                /* Menu Summoners */
                var summoners = MenuActivator.AddSubMenu(new Menu("Summoners", "Summoners"));
                {
                    var summonersHeal = summoners.AddSubMenu(new Menu("Heal", "Heal"));
                    {
                        summonersHeal.AddItem(new MenuItem("SUMHEALENABLE", "Enable").SetValue(true));
                        summonersHeal.AddItem(
                            new MenuItem("SUMHEALSLIDER", "Min. Heal Per.").SetValue(new Slider(20, 99, 1)));
                    }

                    var summonersBarrier = summoners.AddSubMenu(new Menu("Barrier", "Barrier"));
                    {
                        summonersBarrier.AddItem(new MenuItem("SUMBARRIERENABLE", "Enable").SetValue(true));
                        summonersBarrier.AddItem(
                            new MenuItem("SUMBARRIERSLIDER", "Min. Heal Per.").SetValue(new Slider(20, 99, 1)));
                    }

                    var summonersIgnite = summoners.AddSubMenu(new Menu("Ignite", "Ignite"));
                    {
                        summonersIgnite.AddItem(new MenuItem("SUMIGNITEENABLE", "Enable").SetValue(true));
                    }
                }
            }
            Config.AddSubMenu(MenuActivator);
            //var Extras = Config.AddSubMenu(new Menu("Extras", "Extras"));
            //new PotionManager(Extras);

            // If Champion is supported draw the extra menus
            if (BaseType != CClass.GetType())
            {
                SetSmiteSlot();

                var combo = new Menu("Combo", "Combo").SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
                if (CClass.ComboMenu(combo))
                {
                    if (SmiteSlot != SpellSlot.Unknown)
                        combo.AddItem(new MenuItem("ComboSmite", "Use Smite").SetValue(true));

                    Config.AddSubMenu(combo);
                }

                var harass = new Menu("Harass", "Harass");
                if (CClass.HarassMenu(harass))
                {
                    harass.AddItem(new MenuItem("HarassMana", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));
                    Config.AddSubMenu(harass);
                }

                var laneclear = new Menu("Lane Mode", "LaneClear");
                if (CClass.LaneClearMenu(laneclear))
                {
                    laneclear.AddItem(new MenuItem("Lane.Enabled", ":: Enable Lane Farm!").SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, "Marsman | Enable Lane Farm", SharpDX.Color.Aqua);

                    var minManaMenu = new Menu("Min. Mana Settings", "Lane.MinMana.Title");
                    {
                        minManaMenu.AddItem(new MenuItem("LaneMana.Alone", "If I'm Alone %:").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue);
                        minManaMenu.AddItem(new MenuItem("LaneMana.Enemy", "If Enemy Close %:").SetValue(new Slider(60, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed);
                        laneclear.AddSubMenu(minManaMenu);
                    }
                    Config.AddSubMenu(laneclear);
                }

                var jungleClear = new Menu("Jungle Mode", "JungleClear");
                if (CClass.JungleClearMenu(jungleClear))
                {
                    var minManaMenu = new Menu("Min. Mana Settings", "Jungle.MinMana.Title");
                    {
                        minManaMenu.AddItem(new MenuItem("Jungle.Mana.Ally", "Ally Mobs %:").SetValue(new Slider(50, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue);
                        minManaMenu.AddItem(new MenuItem("Jungle.Mana.Enemy", "Enemy Mobs %:").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed);
                        minManaMenu.AddItem(new MenuItem("Jungle.Mana.BigBoys", "Baron/Dragon %:").SetValue(new Slider(70, 100, 0))).SetFontStyle(FontStyle.Regular, Color.HotPink);
                        jungleClear.AddSubMenu(minManaMenu);
                    }
                    jungleClear.AddItem(new MenuItem("Jungle.Items", ":: Use Items:").SetValue(new StringList(new[] { "Off", "Use for Baron", "Use for Baron", "Both" }, 3)));
                    jungleClear.AddItem(new MenuItem("Jungle.Enabled", ":: Enable Jungle Farm!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, "Marsman | Enable Jungle Farm", SharpDX.Color.Aqua);
                    Config.AddSubMenu(jungleClear);
                }

                /*----------------------------------------------------------------------------------------------------------*/
                Obj_AI_Base ally = (from aAllies in HeroManager.Allies
                                    from aSupportedChampions in
                                        new[]
                                            {
                                                "janna", "tahm", "leona", "lulu", "lux", "nami", "shen", "sona", "braum", "bard"
                                            }
                                    where aSupportedChampions == aAllies.ChampionName.ToLower()
                                    select aAllies).FirstOrDefault();

                if (ally != null)
                {
                    var menuAllies = new Menu("Ally Combo", "Ally.Combo").SetFontStyle(FontStyle.Regular, SharpDX.Color.Crimson);
                    {
                        Obj_AI_Hero Leona = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "leona");
                        if (Leona != null)
                        {
                            var menuLeona = new Menu("Leona", "Leona");
                            menuLeona.AddItem(new MenuItem("Leona.ComboBuff", "Force Focus Marked Enemy for Bonus Damage").SetValue(true));
                            menuAllies.AddSubMenu(menuLeona);
                        }

                        Obj_AI_Hero Lux = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "lux");
                        if (Lux != null)
                        {
                            var menuLux = new Menu("Lux", "Lux");
                            menuLux.AddItem(new MenuItem("Lux.ComboBuff", "Force Focus Marked Enemy for Bonus Damage").SetValue(true));
                            menuAllies.AddSubMenu(menuLux);
                        }

                        Obj_AI_Hero Shen = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "shen");
                        if (Shen != null)
                        {
                            var menuShen = new Menu("Shen", "Shen");
                            menuShen.AddItem(new MenuItem("Shen.ComboBuff", "Force Focus Q Marked Enemy Objects for Heal").SetValue(true));
                            menuShen.AddItem(new MenuItem("Shen.ComboBuff", "Minimum Heal:").SetValue(new Slider(80)));
                            menuAllies.AddSubMenu(menuShen);
                        }

                        Obj_AI_Hero Tahm = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Tahm");
                        if (Tahm != null)
                        {
                            var menuTahm = new Menu("Tahm", "Tahm");
                            menuTahm.AddItem(new MenuItem("Tahm.ComboBuff", "Force Focus Marked Enemy for Stun").SetValue(true));
                            menuAllies.AddSubMenu(menuTahm);
                        }

                        Obj_AI_Hero Sona = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Sona");
                        if (Sona != null)
                        {
                            var menuSona = new Menu("Sona", "Sona");
                            menuSona.AddItem(new MenuItem("Sona.ComboBuff", "Force Focus to Marked Enemy").SetValue(true));
                            menuAllies.AddSubMenu(menuSona);
                        }

                        Obj_AI_Hero Lulu = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Lulu");
                        if (Lulu != null)
                        {
                            var menuLulu = new Menu("Lulu", "Lulu");
                            menuLulu.AddItem(new MenuItem("Lulu.ComboBuff", "Force Focus to Enemy If I have E buff").SetValue(true));
                            menuAllies.AddSubMenu(menuLulu);
                        }

                        Obj_AI_Hero Nami = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "nami");
                        if (Nami != null)
                        {
                            var menuNami = new Menu("Nami", "Nami");
                            menuNami.AddItem(new MenuItem("Nami.ComboBuff", "Force Focus to Enemy If I have E Buff").SetValue(true));
                            menuAllies.AddSubMenu(menuNami);
                        }
                    }
                    Config.AddSubMenu(menuAllies);
                }
                /*----------------------------------------------------------------------------------------------------------*/

                var misc = new Menu("Misc", "Misc").SetFontStyle(FontStyle.Regular, SharpDX.Color.DarkOrange);
                if (CClass.MiscMenu(misc))
                {
                    misc.AddItem(new MenuItem("Misc.SaveManaForUltimate", "Save Mana for Ultimate").SetValue(false));                    
                    Config.AddSubMenu(misc);
                }
                /*
                                var extras = new Menu("Extras", "Extras");
                                if (CClass.ExtrasMenu(extras))
                                {
                                    Config.AddSubMenu(extras);
                                }
                 */

                var marksmanDrawings = new Menu("Drawings", "MDrawings");
                Config.AddSubMenu(marksmanDrawings);

                var drawing = new Menu(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(championName), "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aquamarine);
                if (CClass.DrawingMenu(drawing))
                {
                    marksmanDrawings.AddSubMenu(drawing);
                }

                var GlobalDrawings = new Menu("Global", "GDrawings");
                {
                    marksmanDrawings.AddItem(new MenuItem("Draw.TurnOff", "Drawings").SetValue(new StringList(new[] { "Disable", "Enable", "Disable on Combo Mode", "Disable on Lane/Jungle Mode", "Both" }, 1)));
                    var menuCompare = new Menu("Compare me with", "Menu.Compare");
                    {
                        string[] strCompare = new string[HeroManager.Enemies.Count + 1];
                        strCompare[0] = "Off";
                        var i = 1;
                        foreach (var e in HeroManager.Enemies)
                        {
                            strCompare[i] = e.ChampionName;
                            i += 1;
                        }
                        menuCompare.AddItem(new MenuItem("Marksman.Compare.Set", "Set").SetValue(new StringList(new[] { "Off", "Auto Compare at Startup" }, 1)));
                        menuCompare.AddItem(new MenuItem("Marksman.Compare", "Compare me with").SetValue(new StringList(strCompare, 0)));
                        GlobalDrawings.AddSubMenu(menuCompare);
                    }

                    GlobalDrawings.AddItem(new MenuItem("Draw.KillableEnemy", "Killable Enemy Text").SetValue(false));
                    GlobalDrawings.AddItem(new MenuItem("Draw.MinionLastHit", "Minion Last Hit").SetValue(new StringList(new[] { "Off", "On", "Just Out of AA Range Minions" }, 2)));



                    //GlobalDrawings.AddItem(new MenuItem("Draw.JunglePosition", "Jungle Farm Position").SetValue(new StringList(new[] { "Off", "If I'm Close to Mobs", "If Jungle Clear Active" }, 2)));
                    GlobalDrawings.AddItem(new MenuItem("Draw.DrawMinion", "Draw Minions Sprite").SetValue(false));
                    GlobalDrawings.AddItem(new MenuItem("Draw.DrawTarget", "Draw Target Sprite").SetValue(true));
                    marksmanDrawings.AddSubMenu(GlobalDrawings);

                }
            }

            if (Config.Item("Marksman.Compare.Set").GetValue<StringList>().SelectedIndex == 1 && ObjectManager.Player.Level < 6)
            {
                LoadDefaultCompareChampion();
            }

            CClass.MainMenu(Config);

            if (championName == "sivir")
            {
                Evade.Evade.Initiliaze();
                Evade.Config.Menu.DisplayName = "E";
                Config.AddSubMenu(Evade.Config.Menu);
            }

            //Evade.Evade.Initiliaze();
            //Config.AddSubMenu(Evade.Config.Menu);

            Config.AddToMainMenu();

            foreach (var i in Config.Children.Cast<Menu>().SelectMany(GetChildirens))
            {
                i.DisplayName = ":: " + i.DisplayName;
            }

            Sprite.Load();
            CheckAutoWindUp();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnUpdate += eventArgs =>
            {

                if (CClass.FleeActive)
                {
                    ExecuteFlee();
                }

                if (CClass.LaneClearActive)
                {
                    ExecuteLaneClear();
                }

                if (CClass.JungleClearActive)
                {
                    ExecuteJungleClear();
                }

                PermaActive();
            };

            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;

            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
            Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            AsmLoadingTime = Game.Time;
        }

        private static IEnumerable<Menu> GetChildirens(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetChildirens))
                yield return childChild;
        }

        private static void CheckAutoWindUp()
        {
            var additional = 0;

            if (Game.Ping >= 100)
            {
                additional = Game.Ping / 100 * 10;
            }
            else if (Game.Ping > 40 && Game.Ping < 100)
            {
                additional = Game.Ping / 100 * 20;
            }
            else if (Game.Ping <= 40)
            {
                additional = +20;
            }
            var windUp = Game.Ping + additional;
            if (windUp < 40)
            {
                windUp = 40;
            }
            OrbWalking.Item("ExtraWindup").SetValue(windUp < 200 ? new Slider(windUp, 200, 0) : new Slider(200, 200, 0));
        }

        private static void LoadDefaultCompareChampion()
        {
            var enemyChampions = new[]
                                     {
                                         "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jinx", "Kalista",
                                         "Kindred", "KogMaw", "Lucian", "MissFortune", "Quinn", "Sivir", "Tristana",
                                         "Twitch", "Urgot", "Varus", "Vayne"
                                     };

            List<Obj_AI_Hero> mobs = HeroManager.Enemies;

            Obj_AI_Hero compChampion =
                (from fMobs in mobs from fBigBoys in enemyChampions where fBigBoys == fMobs.ChampionName select fMobs)
                    .FirstOrDefault();

            if (compChampion != null)
            {
                var selectedIndex = 0;
                string[] strQ = new string[HeroManager.Enemies.Count + 1];
                strQ[0] = "Off";
                var i = 1;
                foreach (var e in HeroManager.Enemies)
                {
                    strQ[i] = e.ChampionName;
                    if (e.ChampionName == compChampion.ChampionName)
                    {
                        selectedIndex = i;
                    }
                    i += 1;
                }
                Config.Item("Marksman.Compare").SetValue(new StringList(strQ, selectedIndex));
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            var turnOffDrawings = Config.Item("Draw.TurnOff").GetValue<StringList>().SelectedIndex;

            if (turnOffDrawings == 0)
            {
                return;
            }

            if ((turnOffDrawings == 2 || turnOffDrawings == 4) && CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            if ((turnOffDrawings == 3 || turnOffDrawings == 4) && (CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear))
            {
                return;
            }

            if (Config.Item("Draw.KillableEnemy").GetValue<bool>())
            {
                var t = KillableEnemyAa;
                if (t.Key != null && t.Key.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 1000) && t.Value > 0)
                {
                    Utils.Utils.DrawText(Utils.Utils.Text, string.Format("{0}: {1} x AA Damage = Kill", t.Key.ChampionName, t.Value), (int)t.Key.HPBarPosition.X + 145, (int)t.Key.HPBarPosition.Y + 5, SharpDX.Color.White);
                }
            }


            var myChampionKilled = ObjectManager.Player.ChampionsKilled;
            var myAssists = ObjectManager.Player.Assists;
            var myDeaths = ObjectManager.Player.Deaths;
            var myMinionsKilled = ObjectManager.Player.MinionsKilled;

            if (Config.Item("Marksman.Compare.Set").GetValue<StringList>().SelectedIndex == 1 && ObjectManager.Player.Level < 6)
            {
                if (Config.Item("Marksman.Compare").GetValue<StringList>().SelectedIndex != 0)
                {
                    Obj_AI_Hero compChampion = null;
                    foreach (
                        Obj_AI_Hero e in
                            HeroManager.Enemies.Where(
                                e =>
                                    e.ChampionName ==
                                    Config.Item("Marksman.Compare").GetValue<StringList>().SelectedValue))
                    {
                        compChampion = e;
                    }

                    var compChampionKilled = compChampion.ChampionsKilled;
                    var compAssists = compChampion.Assists;
                    var compDeaths = compChampion.Deaths;
                    var compMinionsKilled = compChampion.MinionsKilled;
                    var xText = "You: " + myChampionKilled + " / " + myDeaths + " / " + myAssists + " | " +
                                myMinionsKilled +
                                "      vs      " +
                                compChampion.ChampionName + " : " + compChampionKilled + " / " + compDeaths + " | " +
                                compAssists + " | " + compMinionsKilled;

                    DrawBox(new Vector2(Drawing.Width * 0.400f, Drawing.Height * 0.132f), 350, 26,
                        System.Drawing.Color.FromArgb(100, 255, 200, 37), 1, System.Drawing.Color.Black);
                    Utils.Utils.DrawText(Utils.Utils.Text, xText, Drawing.Width * 0.422f, Drawing.Height * 0.140f,
                        SharpDX.Color.Wheat);

                    if (Game.Time - AsmLoadingTime < 15)
                    {
                        var timer = string.Format("0:{0:D2}", (int)15 - (int)(Game.Time - AsmLoadingTime));
                        var notText =
                            "You can turn on/off this option. Go to 'Marksman -> Global Drawings -> Compare With Me'";
                        Utils.Utils.DrawText(Utils.Utils.Text, notText, Drawing.Width * 0.291f, Drawing.Height * 0.166f,
                            SharpDX.Color.Black);
                        Utils.Utils.DrawText(Utils.Utils.Text, notText, Drawing.Width * 0.290f, Drawing.Height * 0.165f,
                            SharpDX.Color.White);
                        Utils.Utils.DrawText(Utils.Utils.Text, "This message will self destruct in " + timer,
                            Drawing.Width * 0.400f, Drawing.Height * 0.195f, SharpDX.Color.Aqua);
                    }
                }
            }

            /*            var toD = CClass.Config.Item("Draw.ToD").GetValue<bool>();
                        if (toD)
                        {
                            var enemyCount =
                                CClass.Config.Item("Draw.ToDMinEnemy").GetValue<Slider>().Value;
                            var controlRange =
                                CClass.Config.Item("Draw.ToDControlRange").GetValue<Slider>().Value;

                            var xEnemies = HeroManager.Enemies.Count(enemies => enemies.IsValidTarget(controlRange));
                            if (xEnemies >= enemyCount)
                                return;

                            var toDRangeColor =
                                CClass.Config.Item("Draw.ToDControlRangeColor").GetValue<Circle>();
                            if (toDRangeColor.Active)
                                Render.Circle.DrawCircle(ObjectManager.Player.Position, controlRange, toDRangeColor.Color);

                        }
                        */
            /*
            var t = TargetSelector.SelectedTarget;
            if (!t.IsValidTarget())
            {
                t = TargetSelector.GetTarget(1100, TargetSelector.DamageType.Physical);
                TargetSelector.SetTarget(t);
            }

            if (t.IsValidTarget() && ObjectManager.Player.Distance(t) < 1110)
            {
                Render.Circle.DrawCircle(t.Position, 150, Color.Yellow);
            }
            */
            //Utils.Jungle.DrawJunglePosition(Config.Item("Draw.JunglePosition").GetValue<StringList>().SelectedIndex);


            var drawMinionLastHit = Config.Item("Draw.MinionLastHit").GetValue<StringList>().SelectedIndex;
            if (drawMinionLastHit != 0)
            {
                var mx = ObjectManager.Get<Obj_AI_Minion>().Where(m => !m.IsDead && m.IsEnemy).Where(m => m.Health <= ObjectManager.Player.TotalAttackDamage);

                if (drawMinionLastHit == 1)
                {
                    mx = mx.Where(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65));
                }
                else
                {
                    mx = mx.Where(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65 + 300) && m.Distance(ObjectManager.Player.Position) > Orbwalking.GetRealAutoAttackRange(null) + 65);
                }

                foreach (var minion in mx)
                {
                    Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius, System.Drawing.Color.GreenYellow, 1);
                }
            }

            if (CClass != null)
            {
                CClass.Drawing_OnDraw(args);
            }
        }

        private void MySupport()
        {

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //Obj_AI_Hero shen = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "shen");
            //if (shen != null)
            //{

            //}
            //var shenVorpalStar = HeroManager.Enemies.Find(e => e.Buffs.Any(b => b.Name.ToLower() == "shenvorpalstar" && e.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65)));
            //if (shenVorpalStar != null)
            //{
            //    CClass.Orbwalker.ForceTarget(shenVorpalStar);
            //}

            //Obj_AI_Hero Tahm = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Tahm");
            //if (Tahm != null)
            //{

            //}

            //var enemy = HeroManager.Enemies.Find(e => e.Buffs.Any(b => b.Name.ToLower() == "Tahmmark" && e.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65)));
            //if (enemy != null)
            //{
            //    CClass.Orbwalker.ForceTarget(enemy);
            //}

            /*-------------------------------------------------------------*/
            
            if (Items.HasItem(3139) || Items.HasItem(3140))
            {
                CheckChampionBuff();
            }
            
            //Update the combo and harass values.
            CClass.ComboActive = CClass.Config.Item("Orbwalk").GetValue<KeyBind>().Active;
            
            var vHarassManaPer = Config.Item("HarassMana").GetValue<Slider>().Value;
            CClass.HarassActive = CClass.Config.Item("Farm").GetValue<KeyBind>().Active &&
                                  ObjectManager.Player.ManaPercent >= vHarassManaPer;

            CClass.ToggleActive = ObjectManager.Player.ManaPercent >= vHarassManaPer && CClass.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo;

            var vLaneClearManaPer = HeroManager.Enemies.Find(e => e.IsValidTarget(2000) && !e.IsZombie) == null
                ? Config.Item("LaneMana.Alone").GetValue<Slider>().Value
                : Config.Item("LaneMana.Enemy").GetValue<Slider>().Value;

            CClass.LaneClearActive = CClass.Config.Item("LaneClear").GetValue<KeyBind>().Active &&
                                     ObjectManager.Player.ManaPercent >= vLaneClearManaPer && Config.Item("Lane.Enabled").GetValue<KeyBind>().Active;

            CClass.JungleClearActive = false;
            if (CClass.Config.Item("LaneClear").GetValue<KeyBind>().Active && Config.Item("Jungle.Enabled").GetValue<KeyBind>().Active)
            {
                List<Obj_AI_Base> mobs = MinionManager.GetMinions(ObjectManager.Player.Position, 1000, MinionTypes.All, MinionTeam.Neutral);

                if (mobs.Count > 0)
                {
                    var minMana = Config.Item("Jungle.Mana.Enemy").GetValue<Slider>().Value;

                    if (mobs[0].SkinName.ToLower().Contains("baron") || mobs[0].SkinName.ToLower().Contains("dragon") || mobs[0].Team() == Jungle.GameObjectTeam.Neutral)
                    {
                        minMana = Config.Item("Jungle.Mana.BigBoys").GetValue<Slider>().Value;
                    }

                    else if (mobs[0].Team() == (Jungle.GameObjectTeam)ObjectManager.Player.Team)
                    {
                        minMana = Config.Item("Jungle.Mana.Ally").GetValue<Slider>().Value;
                    }

                    else if (mobs[0].Team() != (Jungle.GameObjectTeam)ObjectManager.Player.Team)
                    {
                        minMana = Config.Item("Jungle.Mana.Enemy").GetValue<Slider>().Value;
                    }

                    if (ObjectManager.Player.ManaPercent >= minMana)
                    {
                        CClass.JungleClearActive = true;
                    }
                }
            }
            //CClass.JungleClearActive = CClass.Config.Item("LaneClear").GetValue<KeyBind>().Active && ObjectManager.Player.ManaPercent >= Config.Item("Jungle.Mana").GetValue<Slider>().Value;

            CClass.Game_OnGameUpdate(args);

            UseSummoners();
            var useItemModes = Config.Item("UseItemsMode").GetValue<StringList>().SelectedIndex;

            //Items
            if (
                !((CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                   (useItemModes == 2 || useItemModes == 3))
                  ||
                  (CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed &&
                   (useItemModes == 1 || useItemModes == 3))))
                return;

            var botrk = Config.Item("BOTRK").GetValue<bool>();
            var ghostblade = Config.Item("GHOSTBLADE").GetValue<bool>();
            var sword = Config.Item("SWORD").GetValue<bool>();
            var muramana = Config.Item("MURAMANA").GetValue<bool>();
            var target = CClass.Orbwalker.GetTarget() as Obj_AI_Base;

            var smiteReady = (SmiteSlot != SpellSlot.Unknown &&
                              ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready);

            if (smiteReady && CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Smiteontarget(target as Obj_AI_Hero);

            if (botrk)
            {
                if (target != null && target.Type == ObjectManager.Player.Type &&
                    target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 550)
                {
                    var hasCutGlass = Items.HasItem(3144);
                    var hasBotrk = Items.HasItem(3153);

                    if (hasBotrk || hasCutGlass)
                    {
                        var itemId = hasCutGlass ? 3144 : 3153;
                        var damage = ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Botrk);
                        if (hasCutGlass || ObjectManager.Player.Health + damage < ObjectManager.Player.MaxHealth)
                            Items.UseItem(itemId, target);
                    }
                }
            }

            if (ghostblade && target != null && target.Type == ObjectManager.Player.Type &&
                !ObjectManager.Player.HasBuff("ItemSoTD", true) /*if Sword of the divine is not active */
                && Orbwalking.InAutoAttackRange(target))
                Items.UseItem(3142);

            if (sword && target != null && target.Type == ObjectManager.Player.Type &&
                !ObjectManager.Player.HasBuff("spectralfury", true) /*if ghostblade is not active*/
                && Orbwalking.InAutoAttackRange(target))
                Items.UseItem(3131);

            if (muramana && Items.HasItem(3042))
            {
                if (target != null && CClass.ComboActive &&
                    target.Position.Distance(ObjectManager.Player.Position) < 1200)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana", true))
                    {
                        Items.UseItem(3042);
                    }
                }
                else
                {
                    if (ObjectManager.Player.HasBuff("Muramana", true))
                    {
                        Items.UseItem(3042);
                    }
                }
            }
        }

        public static void UseSummoners()
        {
            if (ObjectManager.Player.IsDead)
                return;

            const int xDangerousRange = 1100;

            if (Config.Item("SUMHEALENABLE").GetValue<bool>())
            {
                var xSlot = ObjectManager.Player.GetSpellSlot("summonerheal");
                var xCanUse = ObjectManager.Player.Health <=
                              ObjectManager.Player.MaxHealth / 100 * Config.Item("SUMHEALSLIDER").GetValue<Slider>().Value;

                if (xCanUse && !ObjectManager.Player.InShop() &&
                    (xSlot != SpellSlot.Unknown || ObjectManager.Player.Spellbook.CanUseSpell(xSlot) == SpellState.Ready)
                    && ObjectManager.Player.CountEnemiesInRange(xDangerousRange) > 0)
                {
                    ObjectManager.Player.Spellbook.CastSpell(xSlot);
                }
            }

            if (Config.Item("SUMBARRIERENABLE").GetValue<bool>())
            {
                var xSlot = ObjectManager.Player.GetSpellSlot("summonerbarrier");
                var xCanUse = ObjectManager.Player.Health <=
                              ObjectManager.Player.MaxHealth / 100 *
                              Config.Item("SUMBARRIERSLIDER").GetValue<Slider>().Value;

                if (xCanUse && !ObjectManager.Player.InShop() &&
                    (xSlot != SpellSlot.Unknown || ObjectManager.Player.Spellbook.CanUseSpell(xSlot) == SpellState.Ready)
                    && ObjectManager.Player.CountEnemiesInRange(xDangerousRange) > 0)
                {
                    ObjectManager.Player.Spellbook.CastSpell(xSlot);
                }
            }

            if (Config.Item("SUMIGNITEENABLE").GetValue<bool>())
            {
                var xSlot = ObjectManager.Player.GetSpellSlot("summonerdot");
                var t = CClass.Orbwalker.GetTarget() as Obj_AI_Hero;

                if (t != null && xSlot != SpellSlot.Unknown &&
                    ObjectManager.Player.Spellbook.CanUseSpell(xSlot) == SpellState.Ready)
                {
                    if (ObjectManager.Player.Distance(t) < 650 &&
                        ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) >=
                        t.Health)
                    {
                        ObjectManager.Player.Spellbook.CastSpell(xSlot, t);
                    }
                }
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            CClass.Orbwalking_AfterAttack(unit, target);
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            CClass.Orbwalking_BeforeAttack(args);
        }
        private static void ExecuteFlee()
        {
            CClass.ExecuteFlee();
        }

        private static void ExecuteJungleClear()
        {
            CClass.ExecuteJungleClear();
        }
        private static void ExecuteLaneClear()
        {
            CClass.ExecuteLaneClear();
        }
        private static void PermaActive()
        {
            CClass.PermaActive();
        }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            CClass.Obj_AI_Base_OnProcessSpellCast(sender, args);
        }
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (Config.Item("Misc.SaveManaForUltimate").GetValue<bool>() &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level > 0 &&
                Math.Abs(ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Cooldown) < 0.00001 &&
                args.Slot != SpellSlot.R)
            {
                var lastMana = ObjectManager.Player.Mana - ObjectManager.Player.Spellbook.GetSpell(args.Slot).ManaCost;
                if (lastMana < ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).ManaCost)
                {
                    args.Process = false;
                }
            }
            
            CClass.Spellbook_OnCastSpell(sender, args);
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            CClass.OnCreateObject(sender, args);
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            CClass.OnDeleteObject(sender, args);
        }

        private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            CClass.Obj_AI_Base_OnBuffAdd(sender, args);
        }

        private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            CClass.Obj_AI_Base_OnBuffRemove(sender, args);
        }

        private static void CheckChampionBuff()
        {
            var canUse3139 = Items.HasItem(3139) && Items.CanUseItem(3139);
            var canUse3140 = Items.HasItem(3140) && Items.CanUseItem(3140);

            foreach (var t1 in ObjectManager.Player.Buffs)
            {
                foreach (var t in QuickSilverMenu.Items)
                {
                    if (QuickSilverMenu.Item(t.Name).GetValue<bool>())
                    {
                        if (t1.Name.ToLower().Contains(t.Name.ToLower()))
                        {
                            var t2 = t1;
                            foreach (var bx in AActivator.BuffList.Where(bx => bx.BuffName == t2.Name))
                            {
                                if (bx.Delay > 0)
                                {
                                    if (ActivatorTime + bx.Delay < Game.Time)
                                        ActivatorTime = Game.Time;

                                    if (ActivatorTime + bx.Delay <= Game.Time)
                                    {
                                        if (canUse3139)
                                            Items.UseItem(3139);
                                        else if (canUse3140)
                                            Items.UseItem(3140);
                                        ActivatorTime = Game.Time;
                                    }
                                }
                                else
                                {
                                    if (canUse3139)
                                        Items.UseItem(3139);
                                    else if (canUse3140)
                                        Items.UseItem(3140);
                                }
                            }
                        }
                    }

                    if (QuickSilverMenu.Item("AnySlow").GetValue<bool>() &&
                        ObjectManager.Player.HasBuffOfType(BuffType.Slow))
                    {
                        if (canUse3139)
                            Items.UseItem(3139);
                        else if (canUse3140)
                            Items.UseItem(3140);
                    }
                    if (QuickSilverMenu.Item("AnySnare").GetValue<bool>() &&
                        ObjectManager.Player.HasBuffOfType(BuffType.Snare))
                    {
                        if (canUse3139)
                            Items.UseItem(3139);
                        else if (canUse3140)
                            Items.UseItem(3140);
                    }
                    if (QuickSilverMenu.Item("AnyStun").GetValue<bool>() &&
                        ObjectManager.Player.HasBuffOfType(BuffType.Stun))
                    {
                        if (canUse3139)
                            Items.UseItem(3139);
                        else if (canUse3140)
                            Items.UseItem(3140);
                    }
                    if (QuickSilverMenu.Item("AnyTaunt").GetValue<bool>() &&
                        ObjectManager.Player.HasBuffOfType(BuffType.Taunt))
                    {
                        if (canUse3139)
                            Items.UseItem(3139);
                        else if (canUse3140)
                            Items.UseItem(3140);
                    }
                }
            }
        }

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Items.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Items.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
                Smite = new Spell(SmiteSlot, 700);
            }
        }

        private static void Smiteontarget(Obj_AI_Hero t)
        {
            var useSmite = Config.Item("ComboSmite").GetValue<bool>();
            var itemCheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
            if (itemCheck && useSmite &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(ObjectManager.Player.Position) < Smite.Range)
            {
                ObjectManager.Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }
        public static void DrawBox(Vector2 position, int width, int height, System.Drawing.Color color, int borderwidth, System.Drawing.Color borderColor)
        {
            Drawing.DrawLine(position.X, position.Y, position.X + width, position.Y, height, color);

            if (borderwidth > 0)
            {
                Drawing.DrawLine(position.X, position.Y, position.X + width, position.Y, borderwidth, borderColor);
                Drawing.DrawLine(position.X, position.Y + height, position.X + width, position.Y + height, borderwidth, borderColor);
                Drawing.DrawLine(position.X, position.Y + 1, position.X, position.Y + height, borderwidth, borderColor);
                Drawing.DrawLine(position.X + width, position.Y + 1, position.X + width, position.Y + height, borderwidth, borderColor);
            }
        }
        private static KeyValuePair<Obj_AI_Hero, int> KillableEnemyAa
        {
            get
            {
                var x = 0;
                var t = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null) + 1400,
                    TargetSelector.DamageType.Physical);
                {
                    if (t.IsValidTarget())
                    {
                        if (t.Health
                            < ObjectManager.Player.TotalAttackDamage
                            * (1 / ObjectManager.Player.AttackCastDelay > 1400 ? 8 : 4))
                        {
                            x = (int)Math.Ceiling(t.Health / ObjectManager.Player.TotalAttackDamage);
                        }
                        return new KeyValuePair<Obj_AI_Hero, int>(t, x);
                    }
                }
                return new KeyValuePair<Obj_AI_Hero, int>(t, x);
            }
        }
    }
}
