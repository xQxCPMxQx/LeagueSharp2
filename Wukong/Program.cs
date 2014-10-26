#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace Wukong
{
    internal class Program
    {
        public const string ChampionName = "MonkeyKing";
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, E, W, R;

        private static readonly SpellSlot SmiteSlot = Player.GetSpellSlot("SummonerSmite");
        private static readonly SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        public static Items.Item Tiamat = new Items.Item(3077, 375);
        public static Items.Item Hydra = new Items.Item(3074, 375); 


        private static Obj_AI_Hero selectedTarget;
        private static TargetSelector vTargetSelector;
        private static string vTargetSelectorStr = "";

        private static readonly Spell[] JunglerLevel = { E, Q, W, Q, Q, R, Q, E, Q, E, R, E, W, E, W, R, W, W };
        private static readonly Spell[] TopLaneLevel = { Q, E, Q, W, Q, R, Q, E, Q, E, R, E, W, E, W, R, W, W };

        private const float SmiteRange = 700f;

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;
        private static int DelayTick { get; set; }


        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != "MonkeyKing") return;
            if (Player.IsDead) return;

            DelayTick = 0;
            
            Q = new Spell(SpellSlot.Q, 420f);
            E = new Spell(SpellSlot.E, 640f);
            R = new Spell(SpellSlot.R, 355f);

            E.SetTargetted(0.5f, 2000f);
            
            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);

            vTargetSelector = new TargetSelector(1000, TargetSelector.TargetingMode.LowHP);

            //Create the menu
            Config = new Menu("xQx | Monkey King", "MonkeyKig", true);

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.SubMenu("TargetSelector")
                .AddItem(new MenuItem("Mode", "Mode"))
                .SetValue(
                    new StringList(
                        new[]
                        {"AutoPriority", "Closest", "LessAttack", "LessCast", "LowHP", "MostAD", "MostAP", "NearMouse"},
                        1));
            Config.SubMenu("TargetSelector")
                .AddItem(new MenuItem("TSRange", "Range"))
                .SetValue(new Slider(1000, 2000, 100));
            Config.SubMenu("TargetSelector")
                .AddItem(
                    new MenuItem("DrawTSRange", "Range Color").SetValue(new Circle(false,
                        System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttacks(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseEComboTurret", "Don't Under Turret E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRComboEnemyCount", "Min. Enemy Count : ")
                .SetValue(new Slider(1, 5, 0)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0],
                        KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarassTurret", "Don't Under Turret E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ")
                .SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            
            var menuLaneClearItems = new Menu("Use Items", "menuLaneClearItems");
            Config.SubMenu("LaneClear").AddSubMenu(menuLaneClearItems);
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "Auto Smite")
                .SetValue<KeyBind>(new KeyBind('N', KeyBindType.Toggle)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ")
                .SetValue(new Slider(50, 100, 0)));

            var menuJungleFarmItems = new Menu("Use Items", "menuJungleFarmItems");
            Config.SubMenu("JungleFarm").AddSubMenu(menuJungleFarmItems);
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Extras -> Use Items 
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));
            MenuExtras.AddItem(new MenuItem("AutoLevelUp", "Auto Level Up").SetValue(true));

            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Extras").AddSubMenu(menuUseItems);
            // Extras -> Use Items -> Targeted Items
            MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(MenuTargetedItems);
            
            MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));

            MenuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));

            // Extras -> Use Items -> AOE Items
            MenuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(MenuNonTargetedItems);
            MenuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("SmiteRange", "Smite Range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            new PotionManager();
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;

            CustomEvents.Unit.OnLevelUp += CustomEvents_Unit_OnLevelUp;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>" +
                                         "{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));
        }

  
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (selectedTarget != null && selectedTarget.IsValidTarget(Config.Item("TSRange").GetValue<Slider>().Value))
            {
                Utility.DrawCircle(selectedTarget.Position, 100f, System.Drawing.Color.GreenYellow);
            }

            var drawTSRange = Config.Item("DrawTSRange").GetValue<Circle>();
            if (drawTSRange.Active)
                Utility.DrawCircle(Player.Position, Config.Item("TSRange").GetValue<Slider>().Value, drawTSRange.Color);

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            var drawSmite = Config.Item("SmiteRange").GetValue<Circle>();
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active && drawSmite.Active)
            {
                Utility.DrawCircle(Player.Position, SmiteRange, drawSmite.Color);
            }
        }

        public static void CustomEvents_Unit_OnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (sender.NetworkId != Player.NetworkId) 
                return;

            if (!Config.Item("AutoLevelUp").GetValue<bool>())
                return;

            Player.Spellbook.LevelUpSpell(SmiteSlot != SpellSlot.Unknown
                ? JunglerLevel[args.NewLevel - 1].Slot
                : TopLaneLevel[args.NewLevel - 1].Slot);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100)) return;

            TargetSelectorMode();

            if (DelayTick - Environment.TickCount <= 250)
            {
                UseSummoners();
                DelayTick = Environment.TickCount;
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    JungleFarm();
            }
        }

        private static void Combo()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            if (qTarget != null & Q.IsReady() && useQ)
            {
                Q.CastOnUnit(Player);
            }

            if (eTarget != null && E.IsReady() && useE)
            {
                if (Config.Item("UseEComboTurret").GetValue<bool>())
                {
                    if (!Utility.UnderTurret(eTarget, true))
                        E.CastOnUnit(eTarget);
                }
                else
                    E.CastOnUnit(eTarget);
            }

            if (eTarget != null)
                UseItems(eTarget);

            if (qTarget != null && IgniteSlot != SpellSlot.Unknown && 
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                Player.GetSummonerSpellDamage(rTarget, Damage.SummonerSpell.Ignite) > rTarget.Health)
            {
                Player.SummonerSpellbook.CastSpell(IgniteSlot, rTarget);
            }

            if (R.IsReady() && useR)
            {
                if (Utility.CountEnemysInRange((int) Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) >=
                    Config.Item("UserRComboEnemyCount").GetValue<Slider>().Value)
                    R.Cast();
            }

            if (Tiamat.IsReady() && Player.Distance(qTarget) <= Tiamat.Range)
                Tiamat.Cast();

            if (Hydra.IsReady() && Player.Distance(qTarget) <= Hydra.Range)
                Tiamat.Cast();

        }
       
        private static void Harass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

            if (qTarget != null && Q.IsReady() && useQ)
            {
            Q.CastOnUnit(Player);
            }

            if (eTarget != null && E.IsReady() && useE)
            {
                if (Config.Item("UseEHarassTurret").GetValue<bool>())
                {
                    if (!Utility.UnderTurret(eTarget, true))
                        E.CastOnUnit(eTarget);
                }
                else
                    E.CastOnUnit(eTarget);
            }

            if (Tiamat.IsReady() && Player.Distance(qTarget) <= Tiamat.Range)
                Tiamat.Cast();

            if (Hydra.IsReady() && Player.Distance(qTarget) <= Hydra.Range)
                Tiamat.Cast();

        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            var mob = mobs[0];
            if (useQ && Q.IsReady() && mobs.Count >= 1)
                Q.Cast();

            if (useE && E.IsReady() && mobs.Count >= 2)
                E.CastOnUnit(mob);
            
            if (mobs.Count >= 2)
            {
                if (Tiamat.IsReady())
                    Tiamat.Cast();

                if (Hydra.IsReady())
                    Hydra.Cast();
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();


            var allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All,
                MinionTeam.NotAlly);

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

                foreach (var vMinion in from vMinion in minionsQ
                    let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                    select vMinion) 
                {
                    Q.CastOnUnit(Player);
                }
            }

            if (useE && E.IsReady())
            {
                var locE = E.GetCircularFarmLocation(allMinionsE);
                if (allMinionsE.Count == allMinionsE.Count(m => Player.Distance(m) < E.Range) && locE.MinionsHit >= 2 &&
                    locE.Position.IsValid()) 
                    E.Cast(locE.Position);
            }

            if (allMinionsE.Count >= 2)
            { 
                if (Tiamat.IsReady())
                    Tiamat.Cast();
                if (Hydra.IsReady())
                    Hydra.Cast();
            }
                
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.Q);

            if (E.IsReady())
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.E);

            if (R.IsReady())
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.R);

            if (Items.CanUseItem(3128))
                fComboDamage += Player.GetItemDamage(vTarget, Damage.DamageItems.Botrk);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            return (float)fComboDamage;
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells) return;

            if (Player.Distance(vTarget) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
            {
                R.Cast();
            }
        }

        private static InventorySlot GetInventorySlot(int id)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(
                item => (item.Id == (ItemId)id && item.Stacks >= 1) || (item.Id == (ItemId)id && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget == null) return;

            foreach (var itemID in from menuItem in MenuTargetedItems.Items
                let useItem = MenuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                into itemId
                where Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                select itemId) 
            {
                Items.UseItem(itemID, vTarget);
            }

            foreach (var itemID in from menuItem in MenuNonTargetedItems.Items
                let useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                into itemId
                where Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                select itemId) 
            {
                Items.UseItem(itemID);
            }
        }
        private static void UseSummoners()
        {
            if (SmiteSlot == SpellSlot.Unknown)
                return;

            if (!Config.Item("AutoSmite").GetValue<KeyBind>().Active) return;

            string[] monsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
            var firstOrDefault = Player.SummonerSpellbook.Spells.FirstOrDefault(
                spell => spell.Name.Contains("mite"));
            if (firstOrDefault == null) return;

            var vMonsters = MinionManager.GetMinions(Player.ServerPosition, firstOrDefault.SData.CastRange[0],
                MinionTypes.All, MinionTeam.NotAlly);
            foreach (
                var vMonster in
                    vMonsters.Where(
                        vMonster =>
                            vMonster != null && !vMonster.IsDead && !Player.IsDead && !Player.IsStunned &&
                            SmiteSlot != SpellSlot.Unknown &&
                            Player.SummonerSpellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                        .Where(
                            vMonster =>
                                (vMonster.Health < Player.GetSummonerSpellDamage(vMonster, Damage.SummonerSpell.Smite)) &&
                                (monsterNames.Any(name => vMonster.BaseSkinName.StartsWith(name)))))
                                                              
            {
                Player.SummonerSpellbook.CastSpell(SmiteSlot, vMonster);
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != 0x201)
            {
                return;
            }
            foreach (var objAIHero in from hero in ObjectManager.Get<Obj_AI_Hero>()
                where hero.IsValidTarget()
                select hero
                into h
                orderby h.Distance(Game.CursorPos, false) descending
                select h
                into enemy
                where enemy.Distance(Game.CursorPos, false) < 150f
                select enemy)
            {
                if (selectedTarget == null ||
                    objAIHero.NetworkId != selectedTarget.NetworkId && selectedTarget.IsVisible &&
                    !selectedTarget.IsDead)
                {
                    selectedTarget = objAIHero;
                    vTargetSelectorStr = objAIHero.ChampionName;
                    Game.PrintChat(
                        string.Format("<font color='#FFFFFF'>New Target: </font> <font color='#70DBDB'>{0}</font>",
                            objAIHero.ChampionName));
                }
                else
                {
                    selectedTarget = null;
                    vTargetSelectorStr = "";
                }
            }


        }
        private static void TargetSelectorMode()
        {

            float tsRange = Config.Item("TSRange").GetValue<Slider>().Value;
            vTargetSelector.SetRange(tsRange);
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
