#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

#endregion

namespace Leblanc
{
    internal class Program
    {
        public const string ChampionName = "Leblanc";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;

        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        private static bool leBlancClone;

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R;

        public static SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");
        public static Items.Item Fqc = new Items.Item(3092, 750);
        public static Items.Item Dfg = Utility.Map.GetMap().Type == Utility.Map.MapType.TwistedTreeline ? 
                                        new Items.Item(3188, 750) : 
                                        new Items.Item(3128, 750);

        public static Dictionary<HitChance, string> EHitChangeList = new Dictionary<HitChance, string>();
        public static HitChance DefaultEHitChance;

        public static Dictionary<string, string> ComboList = new Dictionary<string, string>();
        public static String DefaultCombo;

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu TargetSelectorMenu;
        public static Menu MenuPlayOptions;

        private static readonly string[] LeBlancIsWeakAgainst =
        {
            "Galio", "Karma", "Sion", "Annie", "Syndra", "Diana",
            "Aatrox", "Mordekaiser", "Talon", "Morgana"
        };

        private static readonly string[] LeBlancIsStrongAgainst =
        {
            "Velkoz", "Ahri", "Karthus", "Fizz", "Ziggs",
            "Katarina", "Orianna", "Nidalee", "Yasuo", "Akali"
        };

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

            ComboList.Add("Auto", "Auto");
            ComboList.Add("W-R", "W-R");
            ComboList.Add("Q-R", "Q-R");

            EHitChangeList.Add(HitChance.Low, "Low");
            EHitChangeList.Add(HitChance.Medium, "Medium");
            EHitChangeList.Add(HitChance.High, "High");
            EHitChangeList.Add(HitChance.VeryHigh, "Very High");
            EHitChangeList.Add(HitChance.Immobile, "Immobile");

            try
            {
                Q = new Spell(SpellSlot.Q, 720);
                W = new Spell(SpellSlot.W, 600);
                E = new Spell(SpellSlot.E, 900);
                R = new Spell(SpellSlot.R, 720);
                
                Q.SetTargetted(0.5f, 1500f);
                W.SetSkillshot(0.5f, 200f, 1200f, false, SkillshotType.SkillshotCircle);
                E.SetSkillshot(0.25f, 100f, 1750f, true, SkillshotType.SkillshotLine);
                
                SpellList.Add(Q);
                SpellList.Add(W);
                SpellList.Add(E);
                SpellList.Add(R);
            }
            catch (Exception)
            {
                Game.PrintChat("There is a problem about Loading Spell Informations");
                return;
            }

            try
            { 
                Config = new Menu(ChampionName, ChampionName, true);
                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            }
            catch (Exception)
            {
                Game.PrintChat("There is a problem about Creating Config Menu");
                return;
            }

            try
            {
                TargetSelectorMenu = new Menu("Target Selector", "TargetSelector");
                TargetSelector.AddToMenu(TargetSelectorMenu);
                Config.AddSubMenu(TargetSelectorMenu);
            }
            catch (Exception)
            {
                Game.PrintChat("There is a problem about Creating TargetSelectorMenu");
                return;
            }

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            try
            {
                //Combo menu:
                Config.AddSubMenu(new Menu("Combo", "Combo"));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
                //Config.SubMenu("Combo").AddItem(new MenuItem("ComboSmartW", "Use Smart W").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));
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

                /* [ Combo Option ] */
                var menuComboOption = new Menu("Combo Option", "ComboOption", false);
                foreach (KeyValuePair<string, string> combo in ComboList)
                {
                    var langItem = menuComboOption.AddItem(new MenuItem(combo.Key, combo.Key).SetValue(false));

                    KeyValuePair<string, string> combo1 = combo;
                    langItem.ValueChanged += (sender, argsEvent) =>
                    {
                        if (argsEvent.GetNewValue<bool>())
                        {
                            menuComboOption.Items.ForEach(
                                x =>
                                {
                                    if (x.GetValue<bool>() && x.Name != combo1.Key)
                                        x.SetValue(false);
                                });
                            DefaultCombo = combo1.Key;
                            Game.PrintChat(string.Format("LeBlanc Default Combo: <font color='#FFF9C200'>{0}</font>", combo1.Key));
                        }
                    };
                }

                Config.SubMenu("Combo").AddSubMenu(menuComboOption);

                foreach (var menuItem in from menuItem in menuComboOption.Items
                                         where menuItem.GetValue<bool>()
                                         from combo in ComboList
                                         where menuItem.Name == combo.Key
                                         select menuItem) 
                {
                    DefaultCombo = menuItem.Name;
                }

                if (DefaultCombo == null)
                {
                    DefaultCombo = "W-R";
                    menuComboOption.Item("W-R").SetValue(true);
                }

            }
            catch (Exception)
            {
                Game.PrintChat("There is a problem about Loading Combo Menu");
                return;
            }
            //Harass menu:
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassUseQT", "Use Q (toggle)!").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Farming menu:
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "Harass!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            //JungleFarm menu:
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "Harass!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            try
            {
                MenuPlayOptions = new Menu("Play Options", "PlayOptions");
                Config.AddSubMenu(MenuPlayOptions);

                /* [ Assassin manager ] */
                try
                {
                    new AssassinManager();
                }
                catch (Exception)
                {
                    Game.PrintChat("Something wrong 'Loading Assassing Manager'");
                    return;
                }

                /* [ E HitChance ] */
                var menuEHitChance = new Menu("E HitChance", "EHitChange");
                foreach (KeyValuePair<HitChance, string> eHitChange in EHitChangeList)
                {
                    var langItem = menuEHitChance.AddItem(new MenuItem(eHitChange.Key.ToString(), eHitChange.Value).SetValue(false));

                    KeyValuePair<HitChance, string> eHitChange1 = eHitChange;
                    langItem.ValueChanged += (sender, argsEvent) =>
                    {
                        if (argsEvent.GetNewValue<bool>())
                        {
                            menuEHitChance.Items.ForEach(
                                x =>
                                {
                                    if (x.GetValue<bool>() && x.Name != eHitChange1.Key.ToString())
                                        x.SetValue(false);
                                });
                            DefaultEHitChance = eHitChange1.Key;
                        }
                    };
                }

                MenuPlayOptions.AddSubMenu(menuEHitChance);

                foreach (var menuItem in from menuItem in menuEHitChance.Items
                                         where menuItem.GetValue<bool>()
                                                from eHitChance in EHitChangeList
                                                where menuItem.Name == eHitChance.Key.ToString()
                                         select menuItem)
                {
                    switch (menuItem.Name)
                    {
                        case "Low": 
                            DefaultEHitChance = HitChance.Low;
                            break;

                        case "Medium":
                            DefaultEHitChance = HitChance.Medium;
                            break;

                        case "High":
                            DefaultEHitChance = HitChance.High;
                            break;

                        case "Very High":
                            DefaultEHitChance = HitChance.VeryHigh;
                            break;

                        case "Immobile":
                            DefaultEHitChance = HitChance.Immobile;
                            break;

                        default:
                            DefaultEHitChance = HitChance.High;
                            break;
                    }
                }


                /* [ Double Stun ] */
                MenuPlayOptions.AddItem(
                    new MenuItem("OptDoubleStun", "Double Stun!").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));
            }
            catch (Exception)
            {
                Game.PrintChat("There is a problem about Loading Oplay Options Menu");
                return;
            }


            var menuRun = new Menu("Run", "Run");
            menuRun.AddItem(new MenuItem("RunUseW", "Use W").SetValue(true));
            menuRun.AddItem(new MenuItem("RunUseR", "Use R").SetValue(true));
            menuRun.AddItem(
                new MenuItem("RunActive", "Run!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            Config.AddSubMenu(menuRun);

            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            //Drawings menu:
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(false, Color.Honeydew)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(true, Color.Honeydew)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, Color.Honeydew)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, Color.Honeydew)));

            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("ActiveERange", "Active E Range").SetValue(new Circle(false, Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WObjPosition", "W Obj. Pos.").SetValue(new Circle(true, Color.GreenYellow)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WObjTimeTick", "W Obj. Tick").SetValue(true));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WQRange", "W+Q Range").SetValue(new Circle(false, Color.GreenYellow)));

            new PotionManager();
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            
            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx</font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'>Loaded!</font>",
                    ChampionName));
            Game.PrintChat("__________________________________");

            Game.PrintChat(string.Format("LeBlanc Default E Hit Chance: <font color='#FFF9C200'>{0}</font>",
                DefaultEHitChance));

            Game.PrintChat(string.Format("LeBlanc Default Combo: <font color='#FFF9C200'>{0}</font>", DefaultCombo));
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
                return (from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => Player.Distance(hero) <= 1100)
                    where hero.IsEnemy
                    from buff in hero.Buffs
                    where buff.Name.Contains("LeblancSoulShackle")
                    select hero).FirstOrDefault();
            }
        }
        private static bool DrawEnemySoulShackle
        {
            get
            {
                return (from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => Player.Distance(hero) <= 1100)
                    where hero.IsEnemy
                    from buff in hero.Buffs
                    select (buff.Name.Contains("LeblancSoulShackle"))).FirstOrDefault();
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;

            var isValidTarget = unit.IsValidTarget(E.Range) && spell.DangerLevel == InterruptableDangerLevel.High;

            if (E.IsReady() && isValidTarget)
            {
                E.CastIfHitchanceEquals(unit, DefaultEHitChance);
            }
            else if (R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM" && isValidTarget)
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

        private static void UserSummoners(Obj_AI_Base target)
        {
            if (Dfg.IsReady())
            {
                Dfg.Cast(target);
            }

            if (Fqc.IsReady())
            {
                Fqc.Cast(target.ServerPosition);
            }
          
            if (IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.Distance(target) < 650 && GetComboDamage() >= target.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, target);
                }
            }
        }

        static Obj_AI_Hero GetTarget(float vDefaultRange = 0, TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (vDefaultRange == 0)
                vDefaultRange = Q.Range;

            if (!TargetSelectorMenu.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = TargetSelectorMenu.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy = ObjectManager.Get<Obj_AI_Hero>()
                .Where(
                    enemy =>
                        enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                        TargetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null &&
                        TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                        ObjectManager.Player.Distance(enemy) < assassinRange);

            if (TargetSelectorMenu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private static void Combo()
        {
            var t = GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();
            var useR = Config.Item("ComboUseR").GetValue<bool>();

            var cdQEx = Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
            var cdWEx = Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;

            var cdQ = Game.Time < cdQEx ? cdQEx - Game.Time : 0;
            var cdW = Game.Time < cdWEx ? cdWEx - Game.Time : 0;

            useR = (Config.Item("DontCombo" + t.BaseSkinName) != null &&
                    Config.Item("DontCombo" + t.BaseSkinName).GetValue<bool>() == false) && useR;
            
            if (R.IsReady() || Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
            {
                if (!useR) return;

                switch (DefaultCombo)
                {
                    case "Auto":
                        if (W.IsReady() && !LeBlancStillJumped && Player.Distance(t) <= W.Range)
                        { 
                            W.Cast(t.Position);
                            if (Player.Distance(t) <= W.Range)
                                R.Cast(t.Position);
                        } else if (Q.IsReady() && Player.Distance(t) <= Q.Range)
                        {
                            Q.CastOnUnit(t, true);
                            if (Player.Distance(t) <= Q.Range && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancChaosOrbM")
                                R.CastOnUnit(t, true);
                        }
                        break; 
                    case "W-R":
                        if (W.IsReady() && !LeBlancStillJumped && Player.Distance(t) <= W.Range)
                            W.Cast(t.Position);
                        if (Player.Distance(t) <= W.Range)
                            R.Cast(t.Position);
                        break;

                    case "Q-R":
                        if (Q.IsReady() && Player.Distance(t) <= Q.Range)
                            Q.CastOnUnit(t, true);
                        if (Player.Distance(t) <= Q.Range && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancChaosOrbM")
                            R.CastOnUnit(t, true);
                        break;
                }
            }
            else
            {
                if (Q.IsReady() && useQ && Player.Distance(t) <= Q.Range)
                {
                    Q.CastOnUnit(t);
                }

                if (W.IsReady() && useW && Player.Distance(t) <= W.Range && !LeBlancStillJumped)
                {
                    W.Cast(t);
                }

                if (E.IsReady() && useE && Player.Distance(t) <= E.Range)
                {
                    E.CastIfHitchanceEquals(t, DefaultEHitChance);
                }
            }

            UserSummoners(t);
        }

        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

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
                E.CastIfHitchanceEquals(eTarget, DefaultEHitChance);
            }
        }

        private static float GetComboDamage()
        {
            var fComboDamage = 0d;

            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (Q.IsReady() && qTarget != null)
                fComboDamage += Player.GetSpellDamage(qTarget, SpellSlot.Q);

            if (W.IsReady() && wTarget != null)
                fComboDamage += Player.GetSpellDamage(wTarget, SpellSlot.W);

            if (E.IsReady() && eTarget != null)
                fComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);

/*            if (RQ.IsReady() || RW.IsReady() || RE.IsReady())
                fComboDamage += Player.GetSpellDamage(qTarget, SpellSlot.R);
*/
            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(wTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.Dfg); 

            if (Items.CanUseItem(3092))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.FrostQueenClaim);

            return (float)fComboDamage;
        }
        private static bool xEnemyHaveSoulShackle(Obj_AI_Hero vTarget)
        {
            return (vTarget.HasBuff("LeblancSoulShackle"));
        }

        private static void Run()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var useW = Config.Item("RunUseW").GetValue<bool>();
            var useR = Config.Item("RunUseR").GetValue<bool>();

            if (useW && W.IsReady() && !LeBlancStillJumped)
            {
                W.Cast(Game.CursorPos);
            }

            if (useR && R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM") 
            {
                R.Cast(Game.CursorPos);
            }
        }

        private static void DoubleStun()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            
            if (Config.Item("OptDoubleStun").GetValue<KeyBind>().Active)
            {
                if (Q.IsReady())
                    Config.Item("HarassUseQT").SetValue(false);

                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.80f, Color.GreenYellow, 
                        "Double Stun Active!");

                /*
                var onPlayerPositionEnemyCount2 =
                    (from enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(enemy => enemy.Team != Player.Team && Player.Distance(enemy) < E.Range + 200)
                        select enemy).Count();

                if (onPlayerPositionEnemyCount2 >= 2)
                {
                */
                foreach (
                    var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                enemy =>
                                    enemy.IsEnemy && !enemy.IsDead && enemy.IsVisible && Player.Distance(enemy) < E.Range + 200 &&
                                    !xEnemyHaveSoulShackle(enemy))) 
                    {
                        //foreach (var buff in enemy.Buffs)
                       // {
                            //if (buff.Name.Contains("LeblancSoulShackle"))
                            //    Game.PrintChat(enemy.ChampionName);
                        //}

                        //Render.Circle.DrawCircle(enemy.Position, 75f, Color.GreenYellow);

                        if (E.IsReady() && Player.Distance(enemy) < E.Range)
                        {
                            E.CastIfHitchanceEquals(enemy, DefaultEHitChance);
                        }
                        else
                        if (R.IsReady() && Player.Distance(enemy) < E.Range &&
                            Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM")
                        {
                            R.CastIfHitchanceEquals(enemy, DefaultEHitChance);
                        }
               /*}*/
                }
            }


        }

        private static void RefresySpellR()
        {
            /*
            var rMode = Player.Spellbook.GetSpell(SpellSlot.R).Name;

            switch (rMode)
            {
                case "LeblancChaosOrbM":
                    {
                        R.Range = Q.Range;
                        R.SetTargetted(0.5f, float.MaxValue);
                        break;
                    }
                case "LeblancSlideM":
                    {
                        R.Range = W.Range;
                        R.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
                        break;
                    }
                case "LeblancSoulShackleM":
                    {
                        R.Range = E.Range;
                        R.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);
                        break;
                    }
            }
            */
        }

        private static void SmartW()
        {
            /*
            if (!Config.Item("ComboSmartW").GetValue<bool>())
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


                if (Config.Item("OptDoubleStun").GetValue<KeyBind>().Active && E.IsReady() && R.IsReady())
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
                        var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                        if (qTarget == null)
                            return;
                        if ((Player.Health < qTarget.Health || Player.Level < qTarget.Level) &&
                            vTarget.Health > GetComboDamage())
                            W.Cast();
                        else
                        {
                            if (Q.IsReady())
                                Q.CastOnUnit(qTarget);
                            if (RQ.IsReady())
                                RQ.CastOnUnit(qTarget);
                            if (E.IsReady())
                                E.Cast(qTarget);
                            W.Cast();
                        }
                    }

                }
                Game.PrintChat(slide.Position.ToString());
                Render.Circle.DrawCircle(slide.Position, 400f, Color.Red);

                Game.PrintChat("Slide Pos. Enemy Count: " + onSlidePositionEnemyCount);
                Game.PrintChat("Player Pos. Enemy Count: " + onPlayerPositionEnemyCount);


                Game.PrintChat("W Posision : " + existingSlide.Position);
                Game.PrintChat("Target Position : " + vTarget.Position);
            }
            */
        }


        private static void LaneClear()
        {
            if (!Orbwalking.CanMove(40)) return;

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                    MinionTeam.NotAlly);
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
            
            if (minionsW.MinionsHit < 2 || !W.IsInRange(minionsW.Position.To3D())) 
                return;
            
            W.Cast(minionsW.Position);

        }

        private static void JungleFarm()
        {
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
                W.Cast(mob.Position);

            if (useE && E.IsReady())
                E.Cast(mob);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            
            RefresySpellR();
            //Mode();
//            if (Config.Item("ComboSmartW").GetValue<KeyBind>().Active)
//                SmartW();

            Orbwalker.SetAttack(true);

            if (Config.Item("OptDoubleStun").GetValue<KeyBind>().Active)
            {
                DoubleStun();
            }

            if (Config.Item("RunActive").GetValue<KeyBind>().Active)
            {
                Run();
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassUseQT").GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                    if (t != null && Q.IsReady())
                        Q.CastOnUnit(t);
                }

                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                {
                    var existsMana = Player.MaxMana/100*Config.Item("HarassMana").GetValue<Slider>().Value;
                    if (Player.Mana >= existsMana)
                        Harass();
                }

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                {
                    var existsMana = Player.MaxMana/100*Config.Item("LaneClearMana").GetValue<Slider>().Value;
                    if (Player.Mana >= existsMana)
                        LaneClear();
                }

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                {
                    var existsMana = Player.MaxMana/100*Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                    if (Player.Mana >= existsMana)
                        JungleFarm();                    
                }

            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            var wObjPosition = Config.Item("WObjPosition").GetValue<Circle>();
            var wObjTimeTick = Config.Item("WObjTimeTick").GetValue<bool>();


            var wqRange = Config.Item("WQRange").GetValue<Circle>();
            if (wqRange.Active && Q.IsReady() && W.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, W.Range + Q.Range, wqRange.Color);
            }
            
            var ActiveERange = Config.Item("ActiveERange").GetValue<Circle>();
            if (ActiveERange.Active && EnemyHaveSoulShackle != null)
            {
                Render.Circle.DrawCircle(Player.Position, 1100f, ActiveERange.Color);
            }

            foreach (var existingSlide in ExistingSlide)
            {
                if (wObjPosition.Active)
                    Render.Circle.DrawCircle(existingSlide.Position, 110f, wObjPosition.Color);

                if (!wObjTimeTick) continue;
                if (!(existingSlide.ExpireTime > Game.Time)) continue;

                var time = TimeSpan.FromSeconds(existingSlide.ExpireTime - Game.Time);

                var pos = Drawing.WorldToScreen(existingSlide.Position);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 65, Color.GreenYellow, display);
            }

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            enemy =>
                                enemy.IsEnemy && !enemy.IsDead && enemy.IsVisible && Player.Distance(enemy) < E.Range + 1400 &&
                                !xEnemyHaveSoulShackle(enemy)))
            {
                

                Render.Circle.DrawCircle(enemy.Position, 75f, Color.GreenYellow);

            }
        }
    }
}
