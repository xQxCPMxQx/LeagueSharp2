#region
using System;
using System.Collections.Generic;
using System.Dynamic;
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


    internal class Program
    {
        public const string ChampionName = "Olaf";
        private static string space = "         ";

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

        //Items
        private static Items.Item itemHydra;
        private static Items.Item itemBOTRK;
        private static Items.Item itemTiamat;
        private static Items.Item itemBilgewaterCutlass;
        private static Items.Item itemYoumuu;
        private static Items.Item itemRandiunsOmen;

        //Menu
        public static Menu Config;
        private static GameObject _axeObj;
        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName)
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
            itemBOTRK = new Items.Item(3153, 450f);
            itemBilgewaterCutlass = new Items.Item(3144, 450f);
            itemHydra = new Items.Item(3074, 225f);
            itemTiamat = new Items.Item(3077, 225f);
            itemRandiunsOmen = new Items.Item(3143, 490f);
            itemYoumuu = new Items.Item(3142, 225f);

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
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboActive", "Combo!").SetValue(
                            new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            }

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("Spell Settings", "Spell Settings:"));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", space + "Use Q").SetValue(false));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseQ2Harass", space + "Use Q (Short)").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", space + "Use E").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("Mana Settings", "Mana Settings:"));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("Harass.UseQ.MinMana", space + "Q Harass Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("Harass").AddItem(new MenuItem("Toggle Settings", "Toggle Settings:"));
                {
                    Config.SubMenu("Harass")
                        .AddItem(
                            new MenuItem("Harass.UseQ.Toggle", space + "Toggle Q!").SetValue(
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
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQFarm", space + "Use Q").SetValue(false));
                Config.SubMenu("LaneClear")
                    .AddItem(new MenuItem("UseQFarmMinCount", space + "Use Q Min. Minion").SetValue(new Slider(2, 5, 1)));
                Config.SubMenu("LaneClear")
                    .AddItem(new MenuItem("UseQFarmMinMana", space + "Use Q Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClear E Settings", "E Settings "));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("UseEFarm", space + "Use E").SetValue(false));
                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("UseEFarmSet", space + "Use E Just:").SetValue(
                            new StringList(new[] {"Last Hit", "Allways"}, 0)));
                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("UseEFarmMinHealth", space + "Use E Min. Health").SetValue(new Slider(10, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseItems", "Use Items ").SetValue(true));
                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0],
                            KeyBindType.Press)));
            }

            /* [ Jungle Clear ] */
            Config.AddSubMenu(new Menu("Jungle Clear", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarm Q Settings", "Q Settings"));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", space + "Use Q").SetValue(false));
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("UseQJFarmMinMana", space + "Use Q Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarm W Settings", "W Settings"));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", space + "Use W").SetValue(false));
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("UseWJFarmMinMana", space + "Use W Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarm E Settings", "E Settings "));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", space + "Use E").SetValue(false));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseEJFarmSet", space + "Use E Just:").SetValue(
                            new StringList(new[] {"Last Hit", "Allways"}, 1)));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseEJFarmMinHealth", space + "Use E Min. Health").SetValue(new Slider(10, 100, 0)));

                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("JungleFarm Baron Dragon Settings", "Baron / Dragon Settings "));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseJFarmYoumuuForDragon", space + "Use Youmuu's Ghostblade for Dragon").SetValue(
                            false));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("UseJFarmYoumuuForBaron", space + "Use Youmuu's Ghostblade for Baron").SetValue(
                            false));

                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("JungleFarm Blue Red Settings", "Blue / Red Settings "));
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("UseJFarmYoumuuForBlueRed", space + "Use Youmuu's Ghostblade").SetValue(false));

                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("UseQJAutoAxe", "Auto Catch Axe (Only Jungle)").SetValue(false));

                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseItems", "Use Items ").SetValue(true));
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
            var menuMisc = new Menu("Misc", "Misc");
            {
                menuMisc.AddItem(new MenuItem("Misc.AutoE", "Use E Auto (if possible hit to enemy)").SetValue(false));
                menuMisc.AddItem(new MenuItem("Misc.AutoR", "Use R for Crowd Controls").SetValue(false));
                Config.AddSubMenu(menuMisc);
            }
            /* [ Other ] */

            new PotionManager();

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));

            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.SpellDrawing", "Spell Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.QRange", space + "Q range").SetValue(new Circle(true,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.Q2Range", space + "Short Q range").SetValue(new Circle(true,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.ERange", space + "E range").SetValue(new Circle(false,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.AxeDrawing", "Axe Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("Draw.AxePosition", space + "Axe Position").SetValue(new Circle(true,
                        System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("Draw.AxeTime", space + "Axe Time Remaining").SetValue(true));
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
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds - 1);

                Color vTimeColor = time.TotalSeconds > 4 ? Color.White : Color.Red;
                DrawText(vText, display, (int) pos.X - display.Length*3, (int) pos.Y - 65, vTimeColor);
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

            if (t.IsValidTarget() && Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady() &&
                Player.Distance(t.ServerPosition) <= Q.Range)
            {
                PredictionOutput Qpredict = Q.GetPrediction(t);
                var hithere = Qpredict.CastPosition.Extend(ObjectManager.Player.Position, -100);
                if (Player.Distance(t.ServerPosition) >= 350)
                {
                    Q.Cast(hithere);
                }
                else
                    Q.Cast(Qpredict.CastPosition);
            }

            if (t.IsValidTarget() && Config.Item("UseECombo").GetValue<bool>() && E.IsReady() &&
                Player.Distance(t.ServerPosition) <= E.Range)

                E.CastOnUnit(t);

            if (t.IsValidTarget() && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady() &&
                Player.Distance(t.ServerPosition) <= 225f)

                W.Cast();

            if (Config.Item("UseItems").GetValue<bool>())
            {
                itemBOTRK.Cast(t);

                itemBilgewaterCutlass.Cast(t);
                if (Player.Distance(t.ServerPosition) <= itemHydra.Range)
                {
                    itemHydra.Cast();
                }
                if (Player.Distance(t.ServerPosition) <= itemTiamat.Range)
                {
                    itemTiamat.Cast();
                }
                if (Player.Distance(t.ServerPosition) <= 400)
                {
                    itemYoumuu.Cast();
                }
                if (Player.Distance(t.ServerPosition) <= itemRandiunsOmen.Range)
                {
                    itemRandiunsOmen.Cast();
                }
            }

            if (GetComboDamage(t) > t.Health && IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
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
                PredictionOutput Qpredict = Q.GetPrediction(t);
                var hithere = Qpredict.CastPosition.Extend(ObjectManager.Player.Position, -100);
                if (Player.Distance(t.ServerPosition) >= 350)
                {
                    Q.Cast(hithere);
                }
                else
                    Q.Cast(Qpredict.CastPosition);
            }
        }

        private static void CastShortQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() && Q.IsReady() &&
                Player.Mana > Player.Mana/100*Config.Item("Harass.UseQ.MinMana").GetValue<Slider>().Value &&
                Player.Distance(t.ServerPosition) <= Q2.Range)
            {
                PredictionOutput q2Predict = Q2.GetPrediction(t);
                var hithere = q2Predict.CastPosition.Extend(ObjectManager.Player.Position, -140);
                if (q2Predict.Hitchance >= HitChance.High)
                    Q2.Cast(hithere);
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

            if (E.IsReady() && Config.Item("UseEHarass").GetValue<bool>() &&
                Player.Distance(t.ServerPosition) <= E.Range)
                E.CastOnUnit(t);
        }

        private static void LaneClear()
        {
            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

            if (allMinions.Count == 0)
                return;

            if (Config.Item("LaneClearUseItems").GetValue<bool>())
            {
                var vMinions = MinionManager.GetMinions(Player.ServerPosition, itemTiamat.Range);
                {
                    if (vMinions != null && vMinions.Count >= 2)
                    {
                        if (itemTiamat.IsReady())
                            itemTiamat.Cast();
                        if (itemHydra.IsReady())
                            itemHydra.Cast();
                    }
                }
            }

            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                if (Player.Mana < Player.Mana/100*Config.Item("UseQFarmMinMana").GetValue<Slider>().Value)
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

                if (Player.Health < Player.Health/100*Config.Item("UseEFarmMinHealth").GetValue<Slider>().Value)
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
                string[] bigBoys = {"Baron", "Dragon", "Red", "Blue"};

                foreach (
                    var xbigBoys in
                        bigBoys.Where(xbigBoys => _axeObj != null).Where(xbigBoys => mob.Name.Contains(xbigBoys)))
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, _axeObj.Position);
                }
            }

            if (itemYoumuu.IsReady() && Player.Distance(mob) < 400)
            {
                if (mob.Name.Contains("Baron") && Config.Item("UseJFarmYoumuuForBaron").GetValue<bool>())
                    itemYoumuu.Cast();

                if (mob.Name.Contains("Dragon") && Config.Item("UseJFarmYoumuuForDragon").GetValue<bool>())
                    itemYoumuu.Cast();

                if ((mob.Name.Contains("Red") || mob.Name.Contains("Blue")) &&
                    Config.Item("UseJFarmYoumuuForBlueRed").GetValue<bool>())
                    itemYoumuu.Cast();
            }

            if (Config.Item("JungleFarmUseItems").GetValue<bool>())
            {
                if (itemTiamat.IsReady() && Player.Distance(mob) < itemTiamat.Range)
                    itemTiamat.Cast();
                if (itemHydra.IsReady() && Player.Distance(mob) < itemTiamat.Range)
                    itemHydra.Cast();
            }

            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                if (Player.Mana < Player.Mana/100*Config.Item("UseQJFarmMinMana").GetValue<Slider>().Value)
                    return;

                if (Q.IsReady())
                    if (Q.Cast(mob.Position - 20)) ;
            }

            if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady())
            {
                if (Player.Mana < Player.Mana/100*Config.Item("UseWJFarmMinMana").GetValue<Slider>().Value)
                    return;

                if (mobs.Count >= 2 || mob.Health > Player.TotalAttackDamage*2.5)
                    W.Cast();
            }

            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
            {
                if (Player.Health < Player.Health/100*Config.Item("UseEJFarmMinHealth").GetValue<Slider>().Value)
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

            return (float) fComboDamage;
        }

        public static void DrawText(Font vFont, String vText, int vPosX, int vPosY, Color vColor)
        {
            vFont.DrawText(null, vText, vPosX + 2, vPosY + 2, vColor != Color.Black ? Color.Black : Color.White);
            vFont.DrawText(null, vText, vPosX, vPosY, vColor);
        }
    }
}
