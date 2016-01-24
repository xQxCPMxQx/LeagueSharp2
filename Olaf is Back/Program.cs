#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace Olafisback
{
    using System.Drawing;

    using Color = SharpDX.Color;
    using Font = SharpDX.Direct3D9.Font;

    internal class OlafAxe
    {
        public GameObject Object { get; set; }
        public float NetworkId { get; set; }
        public Vector3 AxePos { get; set; }
        public double ExpireTime { get; set; }
    }

    internal enum Mobs
    {
        Blue = 1,
        Red = 2,
        Dragon = 1,
        Baron = 2,
        All = 3
    }

    internal class Program
    {
        private struct Tuple<TA, TB, TC> : IEquatable<Tuple<TA, TB, TC>>
        {
            private readonly TA item;
            private readonly TB itemType;
            private readonly TC targetingType;

            public Tuple(TA pItem, TB pItemType, TC pTargetingType)
            {
                this.item = pItem;
                this.itemType = pItemType;
                this.targetingType = pTargetingType;
            }

            public TA Item
            {
                get { return this.item; }
            }

            public TB ItemType
            {
                get { return this.itemType; }
            }

            public TC TargetingType
            {
                get { return this.targetingType; }
            }

            public override int GetHashCode()
            {
                return this.item.GetHashCode() ^ this.itemType.GetHashCode() ^ this.targetingType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                return this.Equals((Tuple<TA, TB, TC>) obj);
            }

            public bool Equals(Tuple<TA, TB, TC> other)
            {
                return other.item.Equals(item) && other.itemType.Equals(this.itemType)
                       && other.targetingType.Equals(this.targetingType);
            }
        }

        private enum EnumItemType
        {
            OnTarget,
            Targeted,
            AoE
        }

        private enum EnumItemTargettingType
        {
            Ally,
            EnemyHero,
            EnemyObjects
        }

        public static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static string Tab
        {
            get { return "       "; }
        }

        public const string ChampionName = "Olaf";


        private static readonly OlafAxe olafAxe = new OlafAxe();
        public static Font TextAxe, TextLittle;
        public static int LastTickTime;
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        public static AutoLevel AutoLevel;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;


        private static Items.Item itemYoumuu;
        private static Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>> ItemDb;

        //Menu
        public static Menu Config, MenuMisc, MenuCombo;
        //        private static GameObject _axeObj;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.CharData.BaseSkinName != ChampionName)
                return;

            /* [ Spells ] */
            Q = new Spell(SpellSlot.Q, 1000);
            Q2 = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 75f, 1500f, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 75f, 1600f, false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(E);

            /* [ Items ] */
            itemYoumuu = new Items.Item(3142, 225f);

            ItemDb =
                new Dictionary<string, Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>>
                {
                    {
                        "Tiamat",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3077, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                    },
                    {
                        "Bilge",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3144, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Blade",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3153, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Hydra",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3074, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                    },
                    {
                        "Titanic Hydra Cleave",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3748, Orbwalking.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.OnTarget,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Randiun",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3143, 490f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Hextech",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3146, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Entropy",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3184, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Youmuu's Ghostblade",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3142, Orbwalking.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Sword of the Divine",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3131, Orbwalking.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    }
                };

            /* [ Menus ] */
            Config = new Menu(ChampionName, ChampionName, true).SetFontStyle(FontStyle.Regular, Color.GreenYellow);

            /* [ Target Selector ] */
            new AssassinManager().Initialize();
            /* [ Orbwalker ] */
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            /* [ Combo ] */
            MenuCombo = new Menu("Combo", "Combo");
            Config.AddSubMenu(MenuCombo);
            {
                MenuCombo.AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
            }
            Config.AddItem(
                new MenuItem("ComboActive", "Combo!").SetValue(
                    new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)))
                .SetFontStyle(FontStyle.Regular, Color.GreenYellow);

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("Spell Settings", "Spell Settings:"));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", Tab + "Use Q").SetValue(false));
                Config.SubMenu("Harass")
                    .AddItem(new MenuItem("UseQ2Harass", Tab + "Use Q (Short-Range)").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", Tab + "Use E").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("Mana Settings", "Mana Settings:"));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("Harass.UseQ.MinMana", Tab + "Q Harass Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("Harass").AddItem(new MenuItem("Toggle Settings", "Toggle Settings:"));
                {
                    Config.SubMenu("Harass")
                        .AddItem(
                            new MenuItem("Harass.UseQ.Toggle", Tab + "Auto-Use Q").SetValue(
                                new KeyBind("T".ToCharArray()[0],
                                    KeyBindType.Toggle))).Permashow(true, "Olaf | Toggle Q");
                }
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0],
                            KeyBindType.Press)));
            }

            /* [ Lane Clear ] */
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            {
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        Config.SubMenu("LaneClear").Item("UseQFarmMinCount").Show(eventArgs.GetNewValue<bool>());
                        Config.SubMenu("LaneClear").Item("UseQFarmMinMana").Show(eventArgs.GetNewValue<bool>());
                    };
                Config.SubMenu("LaneClear")
                    .AddItem(new MenuItem("UseQFarmMinCount", Tab + "Min. Minion to Use Q").SetValue(new Slider(2, 5, 1)));
                Config.SubMenu("LaneClear")
                    .AddItem(new MenuItem("UseQFarmMinMana", Tab + "Min. Mana to Use Q").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        Config.SubMenu("LaneClear").Item("UseEFarmSet").Show(eventArgs.GetNewValue<bool>());
                        Config.SubMenu("LaneClear").Item("UseEFarmMinHealth").Show(eventArgs.GetNewValue<bool>());
                    };

                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("UseEFarmSet", Tab + "Use E:").SetValue(new StringList(
                            new[] {"Last Hit", "Always"}, 0)));
                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("UseEFarmMinHealth", Tab + "Min. Health to Use E").SetValue(new Slider(10, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseItems", "Use Items ").SetValue(true));
                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("LaneClearActive", "Lane Clear!").SetValue(new KeyBind("V".ToCharArray()[0],
                            KeyBindType.Press)));
            }

            /* [ Jungle Clear ] */
            Config.AddSubMenu(new Menu("Jungle Clear", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        Config.SubMenu("JungleFarm").Item("UseQJFarmMinMana").Show(eventArgs.GetNewValue<bool>());
                    };
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseQJFarmMinMana", Tab + "Min. Mana to Use Q").SetValue(new Slider(30, 100, 0)));
                /*---------------------------*/

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(false)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        Config.SubMenu("JungleFarm").Item("UseWJFarmMinMana").Show(eventArgs.GetNewValue<bool>());
                    };
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("UseWJFarmMinMana", Tab + "Min. Man to Use W").SetValue(new Slider(30, 100, 0)));
                /*---------------------------*/

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(false)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        Config.SubMenu("JungleFarm").Item("UseEJFarmSet").Show(eventArgs.GetNewValue<bool>());
                        Config.SubMenu("JungleFarm").Item("UseEJFarmMinHealth").Show(eventArgs.GetNewValue<bool>());
                    };
                ;
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseEJFarmSet", Tab + "Use E:").SetValue(
                            new StringList(new[] {"Last Hit", "Always"}, 1)));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseEJFarmMinHealth", Tab + "Min. Health to Use E").SetValue(new Slider(10, 100, 0)));

                /*---------------------------*/
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("JungleFarmUseItems", "Use Items ").SetValue(true))
                    .ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        Config.SubMenu("JungleFarm").Item("UseJFarmYoumuuForDragon").Show(eventArgs.GetNewValue<bool>());
                        Config.SubMenu("JungleFarm")
                            .Item("UseJFarmYoumuuForBlueRed")
                            .Show(eventArgs.GetNewValue<bool>());
                    };
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseJFarmYoumuuForDragon", Tab + "Baron/Dragon:").SetValue(
                            new StringList(new[] {"Off", "Dragon", "Baron", "Both"}, 3)));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseJFarmYoumuuForBlueRed", Tab + "Blue/Red:").SetValue(
                            new StringList(new[] {"Off", "Blue", "Red", "Both"}, 3)));

                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("JungleFarmActive", "Jungle Farm!").SetValue(new KeyBind("V".ToCharArray()[0],
                            KeyBindType.Press)));
            }


            /* [ Flee ] */
            var menuFlee = new Menu("Flee", "Flee");
            {
                menuFlee.AddItem(new MenuItem("Flee.UseQ", "Use Q").SetValue(false));
                menuFlee.AddItem(new MenuItem("Flee.UseYou", "Use Youmuu's Ghostblade").SetValue(false));
                menuFlee.AddItem(
                    new MenuItem("Flee.Active", "Flee!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                Config.AddSubMenu(menuFlee);
            }

            /* [ Misc ] */
            MenuMisc = new Menu("Misc", "Misc");
            {
                MenuMisc.AddItem(new MenuItem("Misc.AutoE", "Auto-Use E (If Enemy Hit)").SetValue(false));
                MenuMisc.AddItem(new MenuItem("Misc.AutoR", "Auto-Use R on Crowd-Control").SetValue(false));
                Config.AddSubMenu(MenuMisc);
            }
            Summoners.Initialize();
            //PotionManager.Initialize();
            AutoLevel = new AutoLevel();

            /* [ Other ] */

            new PotionManager();

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));

            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.SpellDrawing", "Spell Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.QRange", Tab + "Q range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.Q2Range", Tab + "Short Q range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.ERange", Tab + "E range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.AxeDrawing", "Axe Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.AxePosition", Tab + "Axe Position").SetValue(
                        new StringList(new[] {"Off", "Circle", "Line", "Both"}, 3)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.AxeTime", Tab + "Axe Time Remaining").SetValue(true));
            Config.AddToMainMenu();

            foreach (var i in Config.Children.Cast<Menu>().SelectMany(GetChildirens))
            {
                i.DisplayName = ":: " + i.DisplayName;
            }


            TextAxe = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 39,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });
            TextLittle = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
            Game.PrintChat(
                "<font color='#ff3232'>Olaf is Back Ver.: 3 </font><font color='#d4d4d4'><font color='#FFFFFF'> Loaded </font>");

        }

        private static IEnumerable<Menu> GetChildirens(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetChildirens))
                yield return childChild;
        }


        private static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.Name == "olaf_axe_totem_team_id_green.troy")
            {
                olafAxe.Object = obj;
                olafAxe.ExpireTime = Game.Time + 8;
                olafAxe.NetworkId = obj.NetworkId;
                olafAxe.AxePos = obj.Position;
                //_axeObj = obj;
                //LastTickTime = Environment.TickCount;
            }
        }

        private static void GameObject_OnDelete(GameObject obj, EventArgs args)
        {
            if (obj.Name == "olaf_axe_totem_team_id_green.troy")
            {
                olafAxe.Object = null;
                //_axeObj = null;
                LastTickTime = 0;
            }
        }

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                foreach (var item in
                    ItemDb.Where(
                        i =>
                            i.Value.ItemType == EnumItemType.OnTarget
                            && i.Value.TargetingType == EnumItemTargettingType.EnemyHero && i.Value.Item.IsReady()))
                {
                    item.Value.Item.Cast();
                }

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && W.IsReady()
                    && args.Target.Health > Player.TotalAttackDamage*2)
                {
                    W.Cast();
                }
            }
        }

        private static void CountAa()
        {
            int result = 0;

            foreach (
                var e in
                    HeroManager.Enemies.Where(e => e.Distance(Player.Position) < Q.Range*3 && !e.IsDead && e.IsVisible))
            {
                var getComboDamage = GetComboDamage(e);
                var str = " ";

                if (e.Health < getComboDamage + Player.TotalAttackDamage*5)
                {
                    result = (int) Math.Ceiling((e.Health - getComboDamage)/Player.TotalAttackDamage) + 1;
                    if (e.Health < getComboDamage)
                    {
                        str = "Combo = Kill";
                    }
                    else
                    {
                        str = (getComboDamage > 0 ? "Combo " : "") + (result > 0 ? result + " x AA Damage = Kill" : "");
                    }
                }

                DrawText(
                    TextLittle,
                    str,
                    (int) e.HPBarPosition.X + 145,
                    (int) e.HPBarPosition.Y + 5,
                    result <= 4 ? Color.GreenYellow : Color.White);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            CountAa();

            var drawAxePosition = Config.Item("Draw.AxePosition").GetValue<StringList>().SelectedIndex;
            if (olafAxe.Object != null)
            {
                var exTime = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time).TotalSeconds;
                var color = exTime > 4 ? System.Drawing.Color.Yellow : System.Drawing.Color.Red;
                switch (drawAxePosition)
                {
                    case 1:
                        Render.Circle.DrawCircle(olafAxe.Object.Position, 150, color, 6);
                        break;
                    case 2:
                    {
                        var line = new Geometry.Polygon.Line(
                            Player.Position,
                            olafAxe.AxePos,
                            Player.Distance(olafAxe.AxePos));
                        line.Draw(color, 1);
                    }
                        break;
                    case 3:
                    {
                        Render.Circle.DrawCircle(olafAxe.Object.Position, 150, color, 6);

                        var line = new Geometry.Polygon.Line(
                            Player.Position,
                            olafAxe.AxePos,
                            Player.Distance(olafAxe.AxePos));
                        line.Draw(color, 1);
                    }
                        break;


                }
            }

            if (Config.Item("Draw.AxeTime").GetValue<bool>() && olafAxe.Object != null)
            {
                var time = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time);
                var pos = Drawing.WorldToScreen(olafAxe.AxePos);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);

                Color vTimeColor = time.TotalSeconds > 4 ? Color.White : Color.Red;
                DrawText(TextAxe, display, (int) pos.X - display.Length*3, (int) pos.Y - 65, vTimeColor);
            }
            /*
                        if (_axeObj != null)
                        {
                            Render.Circle.DrawCircle(_axeObj.Position, 150, System.Drawing.Color.Yellow, 6);
                        }
             */
            //Draw the ranges of the spells.
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item("Draw." + spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color, 1);
                }
            }
            var Q2Range = Config.Item("Draw.Q2Range").GetValue<Circle>();
            if (Q2Range.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q2.Range, Q2Range.Color, 1);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            

            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo || !Player.HasBuff("Recall"))
            {
                if (Config.Item("Harass.UseQ.Toggle").GetValue<KeyBind>().Active)
                {
                    CastQ();
                }
            }

            if (E.IsReady() && Config.Item("Misc.AutoE").GetValue<bool>())
            {
                var t = AssassinManager.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                    E.CastOnUnit(t);
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                JungleFarm();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }

            if (Config.Item("Flee.Active").GetValue<KeyBind>().Active)
                Flee();

            if (R.IsReady() && Config.Item("Misc.AutoR").GetValue<bool>())
            {
                CastR();
            }
        }

        private static void Combo()
        {
            var t = AssassinManager.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                CastQ();
            }

            if (E.IsReady() && Player.Distance(t.ServerPosition) <= E.Range)
            {
                E.CastOnUnit(t);
            }

            if (W.IsReady() && Player.Distance(t.ServerPosition) <= 225f)
            {
                W.Cast();
            }

            CastItems(t);

            if (GetComboDamage(t) > t.Health && Summoners.IgniteSlot != SpellSlot.Unknown
                && Player.Spellbook.CanUseSpell(Summoners.IgniteSlot) == SpellState.Ready)
            {
                Player.Spellbook.CastSpell(Summoners.IgniteSlot, t);
            }
        }

        private static void CastQ()
        {
            if (!Q.IsReady())
                return;

            var t = AssassinManager.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            Vector3 castPosition2;

            if (!t.IsFacing(Player) && ObjectManager.Player.Distance(t.Position) < ObjectManager.Player.Distance(t.Path[1]) && ObjectManager.Player.Distance(t.Position) > Q.Range/3)
            {
                castPosition2 = t.Position + Vector3.Normalize(t.ServerPosition - ObjectManager.Player.Position)*t.MoveSpeed / 2;
                Render.Circle.DrawCircle(castPosition2, 100f, System.Drawing.Color.Black);
            }
            else
            {
                castPosition2 = t.Position + Vector3.Normalize(t.ServerPosition - ObjectManager.Player.Position) * 20;
            }
            if (castPosition2 != Vector3.Zero && ObjectManager.Player.Distance(castPosition2) <= Q.Range)
            {
                Q.Cast(castPosition2);
            }
            return;


            if (t.IsValidTarget())
            {
                Vector3 castPosition;
                PredictionOutput qPredictionOutput = Q.GetPrediction(t);

                if (!t.IsFacing(Player) && t.Path.Count() >= 1 ) // target is running
                {
                    castPosition = Q.GetPrediction(t).CastPosition + Vector3.Normalize(t.ServerPosition - Player.Position)*t.MoveSpeed/2;
                }
                else
                {
                    castPosition = qPredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -100);
                }

                Q.Cast(Player.Distance(t.ServerPosition) >= 350 ? castPosition : qPredictionOutput.CastPosition);
            }
        }

        private static void CastShortQ()
        {
            if (!Q.IsReady())
                return;

            var t = AssassinManager.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() && Q.IsReady()
                && Player.Mana > Player.MaxMana/100*Config.Item("Harass.UseQ.MinMana").GetValue<Slider>().Value
                && Player.Distance(t.ServerPosition) <= Q2.Range)
            {
                PredictionOutput q2PredictionOutput = Q2.GetPrediction(t);
                var castPosition = q2PredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -140);
                if (q2PredictionOutput.Hitchance >= HitChance.High) Q2.Cast(castPosition);
            }
        }

        private static void CastR()
        {
            BuffType[] buffList =
            {
                BuffType.Blind,
                BuffType.Charm,
                BuffType.Fear,
                BuffType.Knockback,
                BuffType.Knockup,
                BuffType.Taunt,
                BuffType.Slow,
                BuffType.Silence,
                BuffType.Disarm,
                BuffType.Snare
            };

            foreach (var b in buffList.Where(b => Player.HasBuffOfType(b)))
            {
                R.Cast();
            }
        }

        private static void Harass()
        {
            var t = AssassinManager.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Config.Item("UseQHarass").GetValue<bool>())
            {
                CastQ();
            }

            if (Config.Item("UseQ2Harass").GetValue<bool>())
            {
                CastShortQ();
            }

            if (E.IsReady() && Config.Item("UseEHarass").GetValue<bool>()
                && Player.Distance(t.ServerPosition) <= E.Range)
            {
                E.CastOnUnit(t);
            }
        }

        private static void LaneClear()
        {
            var allMinions = MinionManager.GetMinions(
                Player.ServerPosition,
                Q.Range,
                MinionTypes.All,
                MinionTeam.Enemy,
                MinionOrderTypes.MaxHealth);

            if (allMinions.Count <= 0) return;

            if (Config.Item("LaneClearUseItems").GetValue<bool>())
            {
                foreach (var item in from item in ItemDb
                    where
                        item.Value.ItemType == EnumItemType.AoE
                        && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                    let iMinions = allMinions
                    where
                        item.Value.Item.IsReady()
                        && iMinions[0].Distance(Player.Position) < item.Value.Item.Range
                    select item)
                {
                    item.Value.Item.Cast();
                }
            }

            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady()
                && Player.HealthPercent > Config.Item("UseQFarmMinMana").GetValue<Slider>().Value)
            {
                var vParamQMinionCount = Config.Item("UseQFarmMinCount").GetValue<Slider>().Value;

                var objAiHero = from x1 in ObjectManager.Get<Obj_AI_Minion>()
                    where x1.IsValidTarget() && x1.IsEnemy
                    select x1
                    into h
                    orderby h.Distance(Player) descending
                    select h
                    into x2
                    where x2.Distance(Player) < Q.Range - 20 && !x2.IsDead
                    select x2;

                var aiMinions = objAiHero as Obj_AI_Minion[] ?? objAiHero.ToArray();

                var lastMinion = aiMinions.First();

                var qMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    Player.Distance(lastMinion.Position));

                if (qMinions.Count > 0)
                {
                    var locQ = Q.GetLineFarmLocation(qMinions, Q.Width);

                    if (qMinions.Count == qMinions.Count(m => Player.Distance(m) < Q.Range)
                        && locQ.MinionsHit >= vParamQMinionCount && locQ.Position.IsValid())
                    {
                        Q.Cast(lastMinion.Position);
                    }
                }
            }

            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady()
                && Player.HealthPercent > Config.Item("UseEFarmMinHealth").GetValue<Slider>().Value)
            {
                var eMinions = MinionManager.GetMinions(Player.ServerPosition, E.Range);
                if (eMinions.Count > 0)
                {
                    var eFarmSet = Config.Item("UseEFarmSet").GetValue<StringList>().SelectedIndex;
                    switch (eFarmSet)
                    {
                        case 0:
                        {
                            if (eMinions[0].Health <= E.GetDamage(eMinions[0]))
                            {
                                E.CastOnUnit(eMinions[0]);
                            }
                            break;
                        }
                        case 1:
                        {
                            E.CastOnUnit(eMinions[0]);
                            break;
                        }
                    }
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
            {
                return;
            }

            var mob = mobs[0];

            if (Config.Item("JungleFarmUseItems").GetValue<bool>())
            {
                foreach (var item in from item in ItemDb
                    where
                        item.Value.ItemType == EnumItemType.AoE
                        && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                    let iMinions = mobs
                    where item.Value.Item.IsReady() && iMinions[0].IsValidTarget(item.Value.Item.Range)
                    select item)
                {
                    item.Value.Item.Cast();
                }

                if (itemYoumuu.IsReady() && Player.Distance(mob) < 400)
                {
                    var youmuuBaron = Config.Item("UseJFarmYoumuuForDragon").GetValue<StringList>().SelectedIndex;
                    var youmuuRed = Config.Item("UseJFarmYoumuuForBlueRed").GetValue<StringList>().SelectedIndex;

                    if (mob.Name.Contains("Dragon") &&
                        (youmuuBaron == (int) Mobs.Dragon || youmuuBaron == (int) Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Baron") && (youmuuBaron == (int) Mobs.Baron || youmuuBaron == (int) Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Blue") && (youmuuRed == (int) Mobs.Blue || youmuuRed == (int) Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Red") && (youmuuRed == (int) Mobs.Red || youmuuRed == (int) Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }
                }
            }

            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                if (Player.Mana < Player.MaxMana/100*Config.Item("UseQJFarmMinMana").GetValue<Slider>().Value) return;

                if (Q.IsReady()) Q.Cast(mob.Position - 20);
            }

            if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady())
            {
                if (Player.Mana < Player.MaxMana/100*Config.Item("UseWJFarmMinMana").GetValue<Slider>().Value) return;

                if (mobs.Count >= 2 || mob.Health > Player.TotalAttackDamage*2.5) W.Cast();
            }

            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
            {
                if (Player.Health < Player.MaxHealth/100*Config.Item("UseEJFarmMinHealth").GetValue<Slider>().Value)
                    return;

                var vParamESettings = Config.Item("UseEJFarmSet").GetValue<StringList>().SelectedIndex;
                switch (vParamESettings)
                {
                    case 0:
                    {
                        if (mob.Health <= Player.GetSpellDamage(mob, SpellSlot.E)) E.CastOnUnit(mob);
                        break;
                    }
                    case 1:
                    {
                        E.CastOnUnit(mob);
                        break;
                    }
                }
            }
        }

        private static void Flee()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (Config.Item("Flee.UseQ").GetValue<bool>())
                if (Q.IsReady())
                {
                    CastQ();
                }
            if (Config.Item("Flee.UseYou").GetValue<bool>())
            {
                if (itemYoumuu.IsReady())
                    itemYoumuu.Cast();
            }
        }

        private static void CastItems(Obj_AI_Hero t)
        {
            foreach (var item in ItemDb)
            {
                if (item.Value.ItemType == EnumItemType.AoE
                    && item.Value.TargetingType == EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()) item.Value.Item.Cast();
                }
                if (item.Value.ItemType == EnumItemType.Targeted
                    && item.Value.TargetingType == EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()) item.Value.Item.Cast(t);
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady()) fComboDamage += Q.GetDamage(t);

            if (E.IsReady()) fComboDamage += E.GetDamage(t);

            if (Items.CanUseItem(3146)) fComboDamage += Player.GetItemDamage(t, Damage.DamageItems.Hexgun);

            if (Summoners.IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(Summoners.IgniteSlot) == SpellState.Ready)
            {
                fComboDamage += Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);
            }

            return (float) fComboDamage;
        }

        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, Color aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor != Color.Black ? Color.Black : Color.White);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);
        }
    }
}