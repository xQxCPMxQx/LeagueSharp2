#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

#endregion

namespace Leblanc
{
    internal class Program
    {
        public const string ChampionName = "Leblanc";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;

        private static readonly List<Texture> Enemies2 = new List<Texture>();

        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        private static bool leBlancClone;
        private static Texture wButton;
        //private static Texture rButton;
        private static Obj_AI_Hero _selectedTarget;

        private static double soulShackleTimeExperies;

        private static Sprite S;
        private static Font RecF;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        public static TargetSelector vTargetSelector;
        public static string vTargetSelectorStr = "";

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R;

        public static SpellSlot IgniteSlot;

        public static Items.Item Fqc = new Items.Item(3092, 750); // Frost Queen's Claim; 
        public static Items.Item Dfg = new Items.Item(3128, 750);
        public static Items.Item Bft = new Items.Item(3188, 750);

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu TargetSelectorMenu;

        private static readonly string[] LeBlancIsWeakAgainst = {"Galio", "Karma", "Sion", "Annie", "Syndra", "Diana", "Aatrox", "Mordekaiser", "Talon", "Morgana" };
        private static readonly string[] LeBlancIsStrongAgainst = {"Velkoz", "Ahri", "Karthus", "Fizz", "Ziggs", "Katarina", "Orianna", "Nidalee", "Yasuo", "Akali" };

        public static bool LeBlancClone
        {
            get { return leBlancClone; }
            set { leBlancClone = value;}
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName) return;
            //Create the spells
            Q = new Spell(SpellSlot.Q, 720);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 900);
            R = new Spell(SpellSlot.R, 720);

            Q.SetTargetted(0.5f, 1500f);
            W.SetSkillshot(0.5f, 200f, 1200f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 100f, 1750f, true, SkillshotType.SkillshotLine);
            R.SetTargetted(0.5f, 1500f);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            vTargetSelector = new TargetSelector(1000, TargetSelector.TargetingMode.LowHP);
            
            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            TargetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            SimpleTs.AddToMenu(TargetSelectorMenu);

            Config.AddSubMenu(TargetSelectorMenu);
            
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo menu:
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseSmartW", "Smart W Active").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgniteCombo", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseDFGCombo", "Use Deathfire Grasp").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseBFTCombo", "Blackfire Torch").SetValue(true));

            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboDoubleStun", "Double Stun!").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));

            Config.SubMenu("Combo").AddSubMenu(new Menu("Don't Use Combo on", "DontCombo"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("DontCombo")
                    .AddItem(new MenuItem("DontCombo" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }

            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0],
                        KeyBindType.Press)));


            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWQHarass", "Use W+Q").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            //Config.SubMenu("Harass").AddItem(new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q+W", "Q+W+E", "W+Q+E" })));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(
                        new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));

            //Farming menu:
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ")
                .SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "Lane Clear!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //JungleFarm menu:
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ")
                .SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Misc
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            //Drawings menu:
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(true,  Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WObjectPosition", "W Object Position").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WObjectTimeTick", "Show W Tick").SetValue(true));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WQRange", "W+Q Range").SetValue(new Circle(false, Color.GreenYellow)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("EActiveRange", "E Active Range").SetValue(new Circle(false, Color.GreenYellow)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));

            new PotionManager();
            new AssassinManager();
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            //Game.OnWndProc += Game_OnWndProc;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;

            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            Drawing.OnDraw += Drawing_OnDraw;
            
            //Init();
            
            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != 0x201)
            {
                return;
            }
            foreach (var objAIHero in from hero in ObjectManager.Get<Obj_AI_Hero>()
                                      where hero.IsValidTarget() 
                                      select hero into h
                                      orderby h.Distance(Game.CursorPos, false) descending
                                      select h into enemy
                                      where enemy.Distance(Game.CursorPos, false) < 150f
                                      select enemy)
            {
                if (_selectedTarget == null || objAIHero.NetworkId != _selectedTarget.NetworkId && _selectedTarget.IsVisible && !_selectedTarget.IsDead)
                {
                    _selectedTarget = objAIHero;
                    vTargetSelectorStr = objAIHero.ChampionName;
                    Game.PrintChat(string.Format("<font color='#FFFFFF'>New Target: </font> <font color='#70DBDB'>{0}</font>", objAIHero.ChampionName));
                }
                else
                {
                    _selectedTarget = null;
                    vTargetSelectorStr = "";
                }
            }
             
             
        }
        private static int FindCounterStatusForTarget(string enemyBaseSkinName)
        {
            if (LeBlancIsWeakAgainst.Contains(enemyBaseSkinName))
                return 1;

            if (LeBlancIsStrongAgainst.Contains(enemyBaseSkinName))
                return 2;
            
            return 0;
        }

        private static Obj_AI_Hero EnemyHaveSoulShackle
        {
            get
            {
                foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => Player.Distance(hero) <= 1100) where hero.IsEnemy 
                                     from buff in hero.Buffs where buff.Name.Contains("LeblancSoulShackle") 
                                     select hero)
                {
                    soulShackleTimeExperies = Game.Time + 2;
                    return hero;
                }
                soulShackleTimeExperies = 0;
                return null;
            }
        }
        private static bool DrawEnemySoulShackle
        {
            get
            { return (from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => Player.Distance(hero) <= 1100) where hero.IsEnemy 
                      from buff in hero.Buffs 
                      select (buff.Name.Contains("LeblancSoulShackle"))).FirstOrDefault(); }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;

            var isValidTarget = unit.IsValidTarget(E.Range) && spell.DangerLevel == InterruptableDangerLevel.High;

            if (E.IsReady() && isValidTarget)
            {
                E.Cast(unit);
                return;
            }  

            if (R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM" && isValidTarget)
            {
                R.Cast(unit);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            leBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (sender.Name.Contains("displacement_blink_indicator"))
            {
                ExistingSlide.Add(
                    new Slide
                    {
                        Object = sender,
                        NetworkId = sender.NetworkId,
                        Position = sender.Position,
                        ExpireTime = Game.Time + 4
                    });
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("displacement_blink_indicator")) return;
            
            for (var i = 0; i < ExistingSlide.Count; i++)
            {
                if (ExistingSlide[i].NetworkId == sender.NetworkId)
                {
                    ExistingSlide.RemoveAt(i);
                    return;
                }
            }
        }

        public static bool LeBlancStillJumped
        {
            get
            { return !W.IsReady() || Player.Spellbook.GetSpell(SpellSlot.W).Name == "leblancslidereturn";}
        }

        private static void UseSpellR(Obj_AI_Hero vTarget)
        {
            var rMode = Player.Spellbook.GetSpell(SpellSlot.R).Name;

            if (rMode != "LeblancChaosOrbM" || !R.IsReady()) return;

            R.CastOnUnit(vTarget);

            switch (rMode)
            {
                case "LeblancChaosOrbM":
                    {
                        R.Range = Q.Range;
                        R.SetTargetted(0.5f, float.MaxValue);
                        R.CastOnUnit(vTarget);
                        break;
                    }
                case "LeblancSlideM":
                    {
                        R.Range = W.Range;
                        R.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
                        R.Cast(vTarget);
                        break;
                    }
                case "LeblancSoulShackleM":
                    {
                        R.Range = E.Range;
                        R.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);
                        R.Cast(vTarget);
                        break;
                    }
            }
        }

        private static void UserSummoners(Obj_AI_Hero target)
        {
            var useDfg = Config.Item("UseDFGCombo").GetValue<bool>();
            var useBft = Config.Item("UseBFTCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgniteCombo").GetValue<bool>();

            if (Dfg.IsReady() && useDfg)
            {
                Dfg.Cast(target);
            }
            if (Bft.IsReady() && useBft)
            {
                Bft.Cast(target);
            }
            if (Fqc.IsReady())
            {
                Fqc.Cast(target.ServerPosition);
            }
          
            if (useIgnite && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.Distance(target) < 650 && GetComboDamage() >= target.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                }
            }

        }

        private static void Combo(Obj_AI_Hero vTarget)
        {
            if (vTarget == null)
                vTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            var useDfg = Config.Item("UseDFGCombo").GetValue<bool>();
            var useBft = Config.Item("UseBFTCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgniteCombo").GetValue<bool>();

            if (Q.IsReady() && R.IsReady() && Player.Distance(vTarget) < Q.Range)
            {
                useR = (Config.Item("DontCombo" + vTarget.BaseSkinName) != null &&
                        Config.Item("DontCombo" + vTarget.BaseSkinName).GetValue<bool>() == false) && useR;
                {
                    if (useR)
                    {
                        Q.CastOnUnit(vTarget);        
                        if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                            R.CastOnUnit(vTarget);
                    }
                }
            }
            else
            {
                if (useW && W.IsReady() && !LeBlancStillJumped && Player.Distance(vTarget) < W.Range)
                {
                    W.Cast(vTarget);
                }

                if (useE && E.IsReady() && Player.Distance(vTarget) < E.Range)
                {
                    E.Cast(vTarget);
                }

                if (useQ && Q.IsReady() && Player.Distance(vTarget) < Q.Range)
                {
                    Q.CastOnUnit(vTarget);
                }

                if (useR && R.IsReady() && Player.Distance(vTarget) < Q.Range &&
                    Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.Cast(vTarget);
                }

            }

            if (Dfg.IsReady() && useDfg && Player.Distance(vTarget) < Q.Range)
            {
                Dfg.Cast(vTarget);
            }
            if (Bft.IsReady() && useBft && Player.Distance(vTarget) < Q.Range)
            {
                Bft.Cast(vTarget);
            }
            if (Fqc.IsReady() && (useDfg || useBft) && Player.Distance(vTarget) < Q.Range)
            {
                if (Fqc.IsReady())
                {
                    Fqc.Cast(vTarget.ServerPosition);
                }
            }

            if (vTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (GetComboDamage() >= vTarget.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, vTarget);
                }
            }
        }

        private static void Harass()
        {
            var existsMana = Player.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
            if (Player.Mana <= existsMana) return;


            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var wqTarget = SimpleTs.GetTarget(W.Range + Q.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useWQ = Config.Item("UseWQHarass").GetValue<bool>();

            if (ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.Cooldown) // Combo: E-W-Q-R
            {

            }

            if (useWQ && wqTarget != null)
            {
                if (Q.IsReady() && W.IsReady() && !LeBlancStillJumped)
                {
                    W.Cast(wqTarget.ServerPosition);
                    Q.CastOnUnit(wqTarget);
                }
            }

            if (useQ && qTarget != null && Q.IsReady()) 
            {
                Q.CastOnUnit(qTarget);
            }

            if (useW && wTarget != null && W.IsReady() && !LeBlancStillJumped)
            {
                W.Cast(wTarget);
            }
            if (useE && eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }

        private static float GetComboDamage()
        {
            var fComboDamage = 0d;

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (Q.IsReady() && qTarget != null)
                fComboDamage += Player.GetSpellDamage(qTarget, SpellSlot.Q);

            if (W.IsReady() && wTarget != null)
                fComboDamage += Player.GetSpellDamage(wTarget, SpellSlot.W);

            if (E.IsReady() && eTarget != null)
                fComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);

            var rMode = Player.Spellbook.GetSpell(SpellSlot.R).Name;
            switch (rMode)
            {
                case "LeblancChaosOrbM": // R->Q
                    {
                        if (R.IsReady() && qTarget != null)
                            fComboDamage += Player.GetSpellDamage(qTarget, SpellSlot.R);
                        break;
                    }
                case "LeblancSlideM": // R->W
                    {
                        if (R.IsReady() && wTarget != null)
                            fComboDamage += Player.GetSpellDamage(wTarget, SpellSlot.R);
                        break;
                    }
                case "LeblancSoulShackleM": // R->E
                    {
                        if (R.IsReady() && eTarget != null)
                            fComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);
                        break;
                    }
            }

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(wTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.Dfg);

            if (Items.CanUseItem(3188))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.Dfg); 

            if (Items.CanUseItem(3092))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.FrostQueenClaim);

            return (float)fComboDamage;
        }

        private static void SmartW()
        {
            if (!Config.Item("UseSmartW").GetValue<bool>())
                return;

            var vTarget = EnemyHaveSoulShackle;
                foreach (var existingSlide in ExistingSlide)
                {
                    var slide = existingSlide;

                    var onSlidePositionEnemyCount = (from enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                enemy => enemy.Team != Player.Team && enemy.Distance(slide.Position) < 350f)
                        select enemy).Count();

                    var onPlayerPositionEnemyCount = (from enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                enemy => enemy.Team != Player.Team && Player.Distance(enemy) < Q.Range)
                        select enemy).Count();


                    if (Config.Item("ComboDoubleStun").GetValue<KeyBind>().Active && E.IsReady() && R.IsReady())
                    {
                        var onPlayerPositionEnemyCount2 = (from enemy in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(
                                    enemy => enemy.Team != Player.Team && Player.Distance(enemy) < E.Range)
                        select enemy).Count();

                        if (onPlayerPositionEnemyCount2 == 2)
                        {
                            

                        }
                    }
                    if (onPlayerPositionEnemyCount > onSlidePositionEnemyCount)
                    {
                        if (LeBlancStillJumped)
                        { 
                            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                            if (qTarget == null)
                                return;
                            if ((Player.Health < qTarget.Health || Player.Level < qTarget.Level) &&
                                vTarget.Health > GetComboDamage())
                                W.Cast();
                            else
                            {
                                if (Q.IsReady())
                                    Q.CastOnUnit(qTarget);
                                if (R.IsReady())
                                    R.CastOnUnit(qTarget);
                                if (E.IsReady())
                                    E.Cast(qTarget);
                                W.Cast();
                            }
                        }
                         
                    }
                    Utility.DrawCircle(slide.Position, 400f, Color.Red);
                }
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useIgnite)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (useE && eTarget != null && E.IsReady())
                E.Cast(eTarget);

            if (useW && wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
            }

            if (useQ && qTarget != null && Q.IsReady())
                Q.Cast(qTarget);


            if (qTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.Distance(qTarget) < 650 && GetComboDamage() > qTarget.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, qTarget);
                }
            }

            if (!useR || !R.IsReady()) return;
            
            var rMode = Player.Spellbook.GetSpell(SpellSlot.R).Name;
            switch (rMode)
            {
                case "LeblancChaosOrbM":
                {
                    R = Q;
                    if (qTarget != null)
                        R.Cast(qTarget);
                    break;
                }
                case "LeblancSlideM":
                {
                    R = W;
                    if (wTarget != null)
                        R.CastIfWillHit(wTarget);
                    break;
                }
                case "LeblancSoulShackleM":
                {
                    R = E;
                    if (eTarget != null)
                        R.CastIfWillHit(eTarget);
                    break;
                }
            }
        }

        private static void LaneClear()
        {
            if (!Orbwalking.CanMove(40)) return;

            var existsMana = Player.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
            if (Player.Mana <= existsMana) return;

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (Obj_AI_Base vMinion in 
                    from vMinion in minionsQ let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                        where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                            select vMinion)
                {
                    
                    Q.CastOnUnit(vMinion);
                }
            }

            var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 20);
            if (!useW || !W.IsReady()) return;
            var minionsW = W.GetCircularFarmLocation(rangedMinionsW, W.Width * 0.75f);
            if (minionsW.MinionsHit < 3 || !W.InRange(minionsW.Position.To3D())) return;
            W.Cast(minionsW.Position);

        }

        private static void JungleFarm()
        {
            var existsMana = Player.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
            if (Player.Mana <= existsMana) return;

            var useQ = Config.Item("UseQJFarm").GetValue<bool>();
            var useW = Config.Item("UseWJFarm").GetValue<bool>();
            var useE = Config.Item("UseEJFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (useQ && Q.IsReady())
                Q.CastOnUnit(mob);

            if (useW && W.IsReady())
                W.Cast(mob);

            if (useE && E.IsReady())
                E.CastOnUnit(mob);
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            
            //Mode();
            //if (Config.Item("UseSmartW").GetValue<KeyBind>().Active)
            //    SmartW();

            Orbwalker.SetAttacks(true);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                var assassinRange = TargetSelectorMenu.Item("AssassinRange").GetValue<Slider>().Value;
                Obj_AI_Hero vTarget = null;
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>()
                    .Where(enemy => enemy.Team != Player.Team
                        && !enemy.IsDead && enemy.IsVisible
                        && TargetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null
                        && TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>())
                        .OrderBy(enemy => enemy.Distance(Game.CursorPos))
                        )
                {

                    vTarget = Player.Distance(enemy) < assassinRange ? enemy : null;

                }
                Combo(vTarget);
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                    Config.Item("HarassActiveT").GetValue<KeyBind>().Active)
                {
                    Harass();
                }

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            
            if (_selectedTarget != null)
            {
                Utility.DrawCircle(_selectedTarget.Position, 100f, Color.GreenYellow, 7);
            }

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            var wObjectPosition = Config.Item("WObjectPosition").GetValue<Circle>();
            var wObjectTimeTick = Config.Item("WObjectTimeTick").GetValue<bool>();

            var eActiveRange = Config.Item("EActiveRange").GetValue<Circle>();

            var wqRange = Config.Item("WQRange").GetValue<Circle>();

            if (wqRange.Active && Q.IsReady() && W.IsReady())
            {
                Utility.DrawCircle(Player.Position, W.Range + Q.Range, eActiveRange.Color);
            }
            
            if (eActiveRange.Active && EnemyHaveSoulShackle != null)
            {
                Utility.DrawCircle(Player.Position, 1100f, eActiveRange.Color);
            }

            foreach (var existingSlide in ExistingSlide)
            {
                if (wObjectPosition.Active)
                    Utility.DrawCircle(existingSlide.Position, 110f, wObjectPosition.Color);

                if (!wObjectTimeTick) continue;
                if (!(existingSlide.ExpireTime > Game.Time)) continue;

                var time = TimeSpan.FromSeconds(existingSlide.ExpireTime - Game.Time);

                var pos = Drawing.WorldToScreen(existingSlide.Position);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 65, Color.GreenYellow, display);
            }
        }

        public static void Init()
        {
            try
            {
                S = new Sprite(Drawing.Direct3DDevice);
                RecF = new Font(Drawing.Direct3DDevice, new System.Drawing.Font("Tahoma", 9));
            }
            catch (Exception)
            {
                return;
            }
            SpriteHelper.LoadTexture("W", ref wButton, SpriteHelper.TextureType.Default);
            Enemies2.Add(wButton);
            
            Game.PrintChat("Init Done!");
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            try
            {
               foreach (var existingSlide in ExistingSlide)
                {
                    if (S == null || S.IsDisposed)
                    {
                        return;
                    }
                    const float percentScale = 2;
                    S.Begin();
                    var slide = existingSlide;
                    
                   foreach (var mPos in from enemy in Enemies2
                                         select Drawing.WorldToScreen(slide.Position) into serverPos
                                         let playerServerPos = Drawing.WorldToScreen(Player.Position)
                                         select new Size((int)(serverPos[0] - 62 * 0.3f), (int)(serverPos[1] - 62 * 0.3f)))
                    {
                        DirectXDrawer.DrawSprite(S, wButton,
                            mPos.ScaleSize(percentScale, new Vector2(mPos.Width, mPos.Height)),
                            new[] { 0.3f * percentScale, 0.3f * percentScale });
                    }
                    S.End();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (ex.GetType() == typeof(SharpDXException))
                {
                    Game.PrintChat("An error occured. Please re-load LeBlanc app.");
                }
            }
        }
        static void Drawing_OnPostReset(EventArgs args)
        {
            S.OnResetDevice();
            RecF.OnResetDevice();
        }

        static void Drawing_OnPreReset(EventArgs args)
        {
            S.OnLostDevice();
            RecF.OnLostDevice();
        }
        private static void Mode()
        {

            float TSRange = Config.Item("Range").GetValue<Slider>().Value;
            vTargetSelector.SetRange(TSRange);
            var mode = Config.Item("Mode").GetValue<StringList>().SelectedIndex;
            vTargetSelectorStr = "";
            switch (mode)
            {
                case 0:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.AutoPriority);
                    vTargetSelectorStr = "Targetin Mode: Auto Priority";
                    break;
                case 1:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.Closest);
                    vTargetSelectorStr = "Targetin Mode: Closest";
                    break;
                case 2:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.LessAttack);
                    vTargetSelectorStr = "Targetin Mode: Less Attack";
                    break;
                case 3:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.LessCast);
                    vTargetSelectorStr = "Targetin Mode: Less Cast";
                    break;
                case 4:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.LowHP);
                    vTargetSelectorStr = "Targetin Mode: Low HP";
                    break;
                case 5:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.MostAD);
                    vTargetSelectorStr = "Targetin Mode: Most AD";
                    break;
                case 6:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.MostAP);
                    vTargetSelectorStr = "Targetin Mode: Most AP";
                    break;
                case 7:
                    vTargetSelector.SetTargetingMode(TargetSelector.TargetingMode.NearMouse);
                    vTargetSelectorStr = "Targetin Mode: Near Mouse";
                    break;
            }
        }
    }
}
