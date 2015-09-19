#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;

#endregion

namespace Olafisback
{
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
                return this.Equals((Tuple<TA, TB, TC>)obj);
            }

            public bool Equals(Tuple<TA, TB, TC> other)
            {
                return other.item.Equals(item) && other.itemType.Equals(this.itemType)
                       && other.targetingType.Equals(this.targetingType);
            }
        }

        private enum EnumItemType
        {
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
        public static Font vText;
        public static int LastTickTime;
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;

        private static Items.Item itemYoumuu;
        private static Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>> ItemDb;

        //Menu
        public static Menu Config;
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

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            /* [ Items ] */
            itemYoumuu = new Items.Item(3142, 225f);

            ItemDb = new Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>>
            {
                {
                    "Tiamat",
                    new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                        new Items.Item(3077, 250f),
                        EnumItemType.AoE,
                        EnumItemTargettingType.EnemyObjects)
                },
                {
                    "Bilge",
                    new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3144, 450f),
                        EnumItemType.Targeted, EnumItemTargettingType.EnemyHero)
                },
                {
                    "Blade",
                    new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                        new Items.Item(3153, 450f),
                        EnumItemType.Targeted,
                        EnumItemTargettingType.EnemyHero)
                },
                {
                    "Hydra",
                    new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                        new Items.Item(3074, 250f),
                        EnumItemType.AoE,
                        EnumItemTargettingType.EnemyObjects)
                },
                {
                    "Randiun",
                    new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                        new Items.Item(3143, 490f),
                        EnumItemType.AoE,
                        EnumItemTargettingType.EnemyHero)
                }
            };

            /* [ Menus ] */
            Config = new Menu(ChampionName, ChampionName, true);

            /* [ Target Selector ] */
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            /* [ Orbwalker ] */
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            {
                Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("UseItems", "Use Items")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            }

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("Spell Settings", "Spell Settings:"));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", Tab + "Use Q").SetValue(false));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseQ2Harass", Tab + "Use Q (Short)").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", Tab + "Use E").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("Mana Settings", "Mana Settings:"));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("Harass.UseQ.MinMana", Tab + "Q Harass Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("Harass").AddItem(new MenuItem("Toggle Settings", "Toggle Settings:"));
                {
                    Config.SubMenu("Harass")
                        .AddItem(
                            new MenuItem("Harass.UseQ.Toggle", Tab + "Toggle Q!").SetValue(
                                new KeyBind("T".ToCharArray()[0],
                                    KeyBindType.Toggle)));
                }
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass Active!").SetValue(new KeyBind("C".ToCharArray()[0],
                            KeyBindType.Press)));
            }

            /* [ Lane Clear ] */
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            {
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClear Q Settings", "Q Settings"));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQFarm", Tab + "Use Q").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                        {
                            Config.SubMenu("LaneClear").Item("UseQFarmMinCount").Show(eventArgs.GetNewValue<bool>());
                            Config.SubMenu("LaneClear").Item("UseQFarmMinMana").Show(eventArgs.GetNewValue<bool>());
                        };
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQFarmMinCount", Tab + "Use Q Min. Minion").SetValue(new Slider(2, 5, 1)));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQFarmMinMana", Tab + "Use Q Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClear E Settings", "E Settings "));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseEFarm", Tab + "Use E").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                        {
                            Config.SubMenu("LaneClear").Item("UseEFarmSet").Show(eventArgs.GetNewValue<bool>());
                            Config.SubMenu("LaneClear").Item("UseEFarmMinHealth").Show(eventArgs.GetNewValue<bool>());
                        };

                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseEFarmSet", Tab + "Use E Just:").SetValue(new StringList(new[] { "Last Hit", "Always" }, 0)));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseEFarmMinHealth", Tab + "Use E Min. Health").SetValue(new Slider(10, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseItems", "Use Items ").SetValue(true));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press)));
            }

            /* [ Jungle Clear ] */
            Config.AddSubMenu(new Menu("Jungle Clear", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarm Q Settings", "Q Settings"));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", Tab + "Use Q").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                        {
                            Config.SubMenu("JungleFarm").Item("UseQJFarmMinMana").Show(eventArgs.GetNewValue<bool>());
                        };
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarmMinMana", Tab + "Use Q Min. Mana").SetValue(new Slider(30, 100, 0)));
                /*---------------------------*/
                
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarm W Settings", "W Settings")).ValueChanged +=
                    (sender, eventArgs) =>
                        {
                            Config.SubMenu("JungleFarm").Item("UseWJFarm").Show(eventArgs.GetNewValue<bool>());
                            Config.SubMenu("JungleFarm").Item("UseWJFarmMinMana").Show(eventArgs.GetNewValue<bool>());
                        };
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", Tab + "Use W").SetValue(false));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarmMinMana", Tab + "Use W Min. Mana").SetValue(new Slider(30, 100, 0)));
                /*---------------------------*/

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", Tab + "Use E").SetValue(false)).ValueChanged +=
                    (sender, eventArgs) =>
                        {
                            Config.SubMenu("JungleFarm").Item("UseEJFarm").Show(eventArgs.GetNewValue<bool>());
                            Config.SubMenu("JungleFarm").Item("UseEJFarmSet").Show(eventArgs.GetNewValue<bool>());
                            Config.SubMenu("JungleFarm").Item("UseEJFarmMinHealth").Show(eventArgs.GetNewValue<bool>());
                        };;
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarmSet", Tab + "Use E Just:").SetValue(new StringList(new[] { "Last Hit", "Allways" }, 1)));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarmMinHealth", Tab + "Use E Min. Health").SetValue(new Slider(10, 100, 0)));

                /*---------------------------*/
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseItems", "Use Items ").SetValue(true)).ValueChanged +=
                    (sender, eventArgs) =>
                        {
                            Config.SubMenu("JungleFarm").Item("UseJFarmYoumuuForDragon").Show(eventArgs.GetNewValue<bool>());
                            Config.SubMenu("JungleFarm").Item("UseJFarmYoumuuForBlueRed").Show(eventArgs.GetNewValue<bool>());
                        };
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseJFarmYoumuuForDragon", Tab + "Baron/Dragon:").SetValue(new StringList(new []{"Off", "Dragon", "Baron","Both"}, 3)));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseJFarmYoumuuForBlueRed", Tab + "Blue/Red:").SetValue(new StringList(new[] { "Off", "Blue", "Red", "Both" }, 3)));

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJAutoAxe", "Auto Catch Axe (Only in Jungle)").SetValue(false));

                
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "Jungle Farm!").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press)));
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
            var menuMisc = new Menu("Misc", "Misc");
            {
                menuMisc.AddItem(new MenuItem("Misc.AutoE", "Use E Auto (If Enemy Hit)").SetValue(false));
                menuMisc.AddItem(new MenuItem("Misc.AutoR", "Use R Auto on Crowd-Control").SetValue(false));
                Config.AddSubMenu(menuMisc);
            }
            /* [ Other ] */

            new PotionManager();

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));

            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.SpellDrawing", "Spell Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.QRange", Tab + "Q range").SetValue(new Circle(true,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.Q2Range", Tab + "Short Q range").SetValue(new Circle(true,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.ERange", Tab + "E range").SetValue(new Circle(false,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.AxeDrawing", "Axe Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.AxePosition", Tab + "Axe Position").SetValue(new Circle(true,
                        System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("Draw.AxeTime", Tab + "Axe Time Remaining").SetValue(true));
            Config.AddToMainMenu();

            vText = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Times New Roman",
                    Height = 33,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                });

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.PrintChat("<font color='#FFFFFF'>Olaf is Back V2</font> <font color='#70DBDB'> Loaded!</font>");
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

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawAxePosition = Config.Item("Draw.AxePosition").GetValue<Circle>();
            if (drawAxePosition.Active && olafAxe.Object != null)
                Render.Circle.DrawCircle(olafAxe.Object.Position, 150, drawAxePosition.Color, 6);


            if (Config.Item("Draw.AxeTime").GetValue<bool>() && olafAxe.Object != null)
            {
                var time = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time);
                var pos = Drawing.WorldToScreen(olafAxe.AxePos);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);

                Color vTimeColor = time.TotalSeconds > 4 ? Color.White : Color.Red;
                DrawText(vText, display, (int)pos.X - display.Length * 3, (int)pos.Y - 65, vTimeColor);
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
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
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
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady() &&
                Player.Distance(t.ServerPosition) <= Q.Range)
            {
                PredictionOutput qPredictionOutput = Q.GetPrediction(t);
                var castPosition = qPredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -100);
                
                if (Player.Distance(t.ServerPosition) >= 300)
                {
                    Q.Cast(castPosition);
                }
                else
                {
                    Q.Cast(qPredictionOutput.CastPosition);
                }
            }

            if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady() && Player.Distance(t.ServerPosition) <= E.Range)
            {
                E.CastOnUnit(t);
            }

            if (Config.Item("UseWCombo").GetValue<bool>() && W.IsReady() && Player.Distance(t.ServerPosition) <= 225f)
            {
                W.Cast();
            }

            if (Config.Item("UseItems").GetValue<bool>())
            {
                CastItems(t);
            }

            if (GetComboDamage(t) > t.Health && IgniteSlot != SpellSlot.Unknown
                && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                Player.Spellbook.CastSpell(IgniteSlot, t);
            }
        }

        private static void CastQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                Vector3 castPosition;
                PredictionOutput qPredictionOutput = Q.GetPrediction(t);

                if (!t.IsFacing(Player) && t.Path.Count() >= 1) // target is running
                {
                    castPosition = Q.GetPrediction(t).CastPosition
                                   + Vector3.Normalize(t.ServerPosition - Player.Position) * t.MoveSpeed / 2;
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

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() && Q.IsReady()
                && Player.Mana > Player.MaxMana / 100 * Config.Item("Harass.UseQ.MinMana").GetValue<Slider>().Value
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
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
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
            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

            if (allMinions.Count == 0)
                return;

            if (Config.Item("LaneClearUseItems").GetValue<bool>())
            {
                foreach (var item in from item in ItemDb
                                     where
                                         item.Value.ItemType == EnumItemType.AoE
                                         && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                                     let iMinions =
                                         MinionManager.GetMinions(
                                             ObjectManager.Player.ServerPosition,
                                             item.Value.Item.Range)
                                     where
                                         iMinions.Count >= Config.Item("Lane.W.MinObj").GetValue<Slider>().Value
                                         && item.Value.Item.IsReady()
                                     select item)
                {
                    item.Value.Item.Cast();
                }
            }

            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * Config.Item("UseQFarmMinMana").GetValue<Slider>().Value)
                    return;

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

                var qMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                    Player.Distance(lastMinion.Position));

                var locQ = Q.GetLineFarmLocation(qMinions, Q.Width);

                if (qMinions.Count == qMinions.Count(m => Player.Distance(m) < Q.Range) &&
                    locQ.MinionsHit >= vParamQMinionCount &&
                    locQ.Position.IsValid())
                    Q.Cast(lastMinion.Position);
            }

            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady())
            {

                if (Player.Health < Player.MaxHealth / 100 * Config.Item("UseEFarmMinHealth").GetValue<Slider>().Value)
                    return;

                var eMinions = MinionManager.GetMinions(Player.ServerPosition, E.Range);

                var vParamESettings = Config.Item("UseEFarmSet").GetValue<StringList>().SelectedIndex;
                switch (vParamESettings)
                {
                    case 0:
                        {
                            if (eMinions[0].IsValidTarget() &&
                                eMinions[0].Health <= Player.GetSpellDamage(eMinions[0], SpellSlot.E))
                                E.CastOnUnit(eMinions[0]);
                            break;
                        }
                    case 1:
                        {
                            if (eMinions[0].IsValidTarget())
                                E.CastOnUnit(eMinions[0]);
                            break;
                        }
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs == null)
                return;

            var mob = mobs[0];

            if (Config.Item("UseQJAutoAxe").GetValue<bool>() &&
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                string[] bigBoys = { "Baron", "Dragon", "Red", "Blue" };

                foreach (
                    var xbigBoys in
                        bigBoys.Where(xbigBoys => olafAxe != null).Where(xbigBoys => mob.Name.Contains(xbigBoys)))
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, olafAxe.AxePos);
                }
            }

            if (Config.Item("JungleFarmUseItems").GetValue<bool>())
            {
                foreach (var item in from item in ItemDb
                                     where
                                         item.Value.ItemType == EnumItemType.AoE
                                         && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                                     let iMinions =
                                         MinionManager.GetMinions(
                                             ObjectManager.Player.ServerPosition,
                                             item.Value.Item.Range)
                                     where item.Value.Item.IsReady()
                                     select item)
                {
                    item.Value.Item.Cast();
                }

                if (itemYoumuu.IsReady() && Player.Distance(mob) < 400)
                {
                    var youmuuBaron = Config.Item("UseJFarmYoumuuForDragon").GetValue<StringList>().SelectedIndex;
                    var youmuuRed = Config.Item("UseJFarmYoumuuForBlueRed").GetValue<StringList>().SelectedIndex;

                    if (mob.Name.Contains("Dragon") && (youmuuBaron == (int)Mobs.Dragon || youmuuBaron == (int)Mobs.All)) {itemYoumuu.Cast();}

                    if (mob.Name.Contains("Baron") && (youmuuBaron == (int)Mobs.Baron || youmuuBaron == (int)Mobs.All)) itemYoumuu.Cast();

                    if (mob.Name.Contains("Blue") && (youmuuRed == (int)Mobs.Blue || youmuuRed == (int)Mobs.All)) itemYoumuu.Cast();

                    if (mob.Name.Contains("Red") && (youmuuRed == (int)Mobs.Red || youmuuRed == (int)Mobs.All)) itemYoumuu.Cast();
                }
            }

            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * Config.Item("UseQJFarmMinMana").GetValue<Slider>().Value)
                    return;

                if (Q.IsReady()) Q.Cast(mob.Position - 20);
            }

            if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * Config.Item("UseWJFarmMinMana").GetValue<Slider>().Value)
                    return;

                if (mobs.Count >= 2 || mob.Health > Player.TotalAttackDamage * 2.5)
                    W.Cast();
            }

            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
            {
                if (Player.Health < Player.MaxHealth / 100 * Config.Item("UseEJFarmMinHealth").GetValue<Slider>().Value)
                    return;

                var vParamESettings = Config.Item("UseEJFarmSet").GetValue<StringList>().SelectedIndex;
                switch (vParamESettings)
                {
                    case 0:
                        {
                            if (mob.Health <= Player.GetSpellDamage(mob, SpellSlot.E))
                                E.CastOnUnit(mob);
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

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.Q);

            if (E.IsReady())
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.E);

            if (Items.CanUseItem(3146))
                fComboDamage += Player.GetItemDamage(vTarget, Damage.DamageItems.Hexgun);

            if (IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            return (float)fComboDamage;
        }

        public static void DrawText(Font vFont, String vText, int vPosX, int vPosY, Color vColor)
        {
            vFont.DrawText(null, vText, vPosX + 2, vPosY + 2, vColor != Color.Black ? Color.Black : Color.White);
            vFont.DrawText(null, vText, vPosX, vPosY, vColor);
        }
    }
}
