#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
#endregion

namespace Irelia
{
    internal class Program
    {
        public const string ChampionName = "Irelia";
        private static readonly Obj_AI_Hero vPlayer = ObjectManager.Player;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        public static float QUsedTime;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;
        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;

        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (vPlayer.BaseSkinName != "Irelia") return;
            
            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 1000f);

            Q.SetSkillshot(0.25f, 75f, 1500f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 75f, 1500f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.15f, 80f, 1500f, false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vPlayer.GetSpellSlot("SummonerDot");
            
            //Create the menu
            Config = new Menu("Irelia", "Irelia", true);

            var TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(TargetSelectorMenu);
            Config.AddSubMenu(TargetSelectorMenu);

            new AssassinManager();

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);
          
            var menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.AddSubMenu(menuUseItems);
            // Extras -> Use Items -> Targeted Items
            MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));
            menuUseItems.AddSubMenu(MenuTargetedItems);

            // Extras -> Use Items -> AOE Items
            MenuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            MenuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));
            menuUseItems.AddSubMenu(MenuNonTargetedItems);
            
            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Menu comboUseQ = new Menu("Use Q", "comboUseQ");
            Config.SubMenu("Combo").AddSubMenu(comboUseQ);
                comboUseQ.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                comboUseQ.AddItem(new MenuItem("UseQComboDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0], 
                        KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Menu harassUseQ = new Menu("Use Q", "harassUseQ");
            Config.SubMenu("Harass").AddSubMenu(harassUseQ);
                harassUseQ.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harassUseQ.AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q", "E", "Q+E"})));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));
            
            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Menu laneClearUseQ = new Menu("Use Q", "laneClearUseQ");
            Config.SubMenu("LaneClear").AddSubMenu(laneClearUseQ);
                laneClearUseQ.AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            laneClearUseQ.AddItem(new MenuItem("UseQLaneClearDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("QFarmDelay", "Q Farm Delay").SetValue(new Slider(200, 500, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJungleFarm", "Use W").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Extras
            
//            Config.AddSubMenu(new Menu("Extras", "Extras"));
 //           Config.SubMenu("Extras").AddItem(new MenuItem("StopUlties", "Interrupt Ulti With E").SetValue(true));
  //          Config.SubMenu("Extras").AddItem(new MenuItem("ForceInterruptUlties", "Force Interrupt Ulti With Q+E").SetValue(true));

            // Extras -> Use Items 
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "InterruptSpells").SetValue(true));
            MenuExtras.AddItem(new MenuItem("StopUlties", "Interrupt Ulti With E").SetValue(true));
            MenuExtras.AddItem(new MenuItem("ForceInterruptUlties", "Force Interrupt Ulti With Q+E").SetValue(true));
            
            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
    
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
                dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            
            new PotionManager();
            

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;

            QUsedTime = Game.Time;

            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx:</font> <font color='#FFFFFF'>{0} Loaded!</font>", ChampionName));

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color);
            }
        }

        static Obj_AI_Hero GetEnemy(float vDefaultRange = 0, TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = Q.Range;

            if (!Config.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Config.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy = ObjectManager.Get<Obj_AI_Hero>()
                .Where(
                    enemy =>
                        enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                        Config.Item("Assassin" + enemy.ChampionName) != null &&
                        Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                        ObjectManager.Player.Distance(enemy) < assassinRange);

            if (Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        static int BladesSpellCount 
        {
            get
            {
                return
                    ObjectManager.Player.Buffs.Where(
                        buff => buff.Name.ToLower() == "ireliatranscendentbladesspell")
                        .Select(buff => buff.Count).FirstOrDefault();
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(50)) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Config.Item("HarassMana").GetValue<Slider>().Value;
                if (vPlayer.ManaPercentage() >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (vPlayer.ManaPercentage() >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (vPlayer.ManaPercentage() >= existsMana)
                    JungleFarm();
            }
        }

        private static bool canUseQ()
        {
            var qFarmDelay = (Config.Item("QFarmDelay").GetValue<Slider>().Value);
            return (Game.Time * 1000 - QUsedTime) > qFarmDelay * 3;
        }

        private static void CastSpellQ(Obj_AI_Base t, bool dontUnderTurret = false)
        {
            var qFarmDelay = (Config.Item("QFarmDelay").GetValue<Slider>().Value);

            if (dontUnderTurret)
            {
                if (!Utility.UnderTurret(t))
                {
                    if (canUseQ())
                    {
                        Q.CastOnUnit(t);
                        QUsedTime = Game.Time * 1000;
                    }
                }
            }
            else
            {
                if (canUseQ())
                {
                    Q.CastOnUnit(t);
                    QUsedTime = Game.Time * 1000;
                }
            }
        }

        private static void CastSpellE()
        {
            var vTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (vTarget != null)
                E.CastOnUnit(vTarget);
        }

        private static void CastSpellW()
        {
            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.True);
            if (t != null && ObjectManager.Player.Distance(t) <= vPlayer.AttackRange + 30)
            {
                W.Cast();
            }
        }
        private static void CastSpellR()
        {
            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (t != null)
            {
                if (R.IsReady() && (GetComboDamage(t) > t.Health || BladesSpellCount > 0)) 
                {
                    R.Cast(t, false, true);
                }
            }
        }
        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            var t = GetEnemy(Q.Range, TargetSelector.DamageType.Physical);
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            if (Q.IsReady() && useQ)
                CastSpellQ(t, useQDontUnderTurret);

            if (E.IsReady() && useE)
                CastSpellE();

            if (W.IsReady() && useW)
                CastSpellW();

            if (R.IsReady() && useR)
                CastSpellR();
            
            if (ObjectManager.Player.Distance(t) < 650 &&
                ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) >=
                t.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
            }
            
            UseItems(t);
        }


        private static void Harass()
        {
            var vTarget = GetEnemy(Q.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            var mana = ObjectManager.Player.MaxMana * (Config.Item("HarassMana")
                .GetValue<Slider>().Value / 100.0);
            
            int vHarassMode = Config.Item("HarassMode").GetValue<StringList>().SelectedIndex;

            switch (vHarassMode)
            {
                case 0:
                    {
                        if (Q.IsReady() && useQ)
                            CastSpellQ(vTarget, useQDontUnderTurret);
                        break;
                    }
                case 1:
                    {
                        CastSpellE();
                        break;
                    }
                case 2:
                    {
                        if (Q.IsReady() && E.IsReady())
                        {
                            CastSpellQ(vTarget, useQDontUnderTurret);
                            CastSpellE();
                        }
                        break;
                    }
            }

            if (Q.IsReady() && useQ)
                CastSpellQ(vTarget, useQDontUnderTurret);

            if (E.IsReady() && useE)
                CastSpellE();

            if (W.IsReady() && useW)
                CastSpellW();
        }
        
        private static void JungleFarm()
        {
            var JungleFarmActive = Config.Item("JungleFarmActive").GetValue<KeyBind>().Active;

            if (JungleFarmActive)
            {
                var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
                var useW = Config.Item("UseWJungleFarm").GetValue<bool>();
                var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

                var mobs = MinionManager.GetMinions(vPlayer.ServerPosition, Q.Range,
                    MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Count > 0)
                {
                    if (Q.IsReady() && useQ)
                        CastSpellQ(mobs[0]);

                    if (W.IsReady() && useW)
                        W.Cast();

                    if (E.IsReady() && useE)
                        E.CastOnUnit(mobs[0]);
                }
            }
        }

        private static void LaneClear()
        {
            var laneClearActive = Config.Item("LaneClearActive").GetValue<KeyBind>().Active;
            if (laneClearActive)
            {
                var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
                var useW = Config.Item("UseWLaneClear").GetValue<bool>();
                var useE = Config.Item("UseELaneClear").GetValue<bool>();
                var useQDontUnderTurret = Config.Item("UseQLaneClearDontUnderTurret").GetValue<bool>();

                var vMinions = MinionManager.GetMinions(vPlayer.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    var vMinionQDamage = vPlayer.GetSpellDamage(vMinion, SpellSlot.Q);
                    var vMinionEDamage = vPlayer.GetSpellDamage(vMinion, SpellSlot.E); 

                    var qFarmDelay = (Config.Item("QFarmDelay").GetValue<Slider>().Value);

                    if (useQ && vMinion.Health <= vMinionQDamage)
                    {
                        CastSpellQ(vMinion, useQDontUnderTurret);
                    }

                    if (useW && W.IsReady())
                        W.Cast();

                    if (useE && E.IsReady() && vMinion.Health <= vMinionEDamage)
                        E.CastOnUnit(vMinion);
                }
            }
        }

        private static void LaneClearQ(int qFarmDelay, Obj_AI_Base vMinion)
        {
            if ((Game.Time * 1000 - QUsedTime) > qFarmDelay * 3)
            {
                Q.CastOnUnit(vMinion);
                QUsedTime = Game.Time * 1000;
            }
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.Q); 
            
            if (W.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.W); 

            if (E.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.E); 
            
            if (R.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.R) *4;

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            if (Config.Item("item3153").GetValue<bool>() && Items.CanUseItem(3153))
                fComboDamage += ObjectManager.Player.GetItemDamage(vTarget, Damage.DamageItems.Botrk);

            return (float)fComboDamage;
        }

        public static bool IsPositionSafe(Obj_AI_Base vTarget, Spell vSpell) 
        {
            Vector2 predPos = vSpell.GetPrediction(vTarget).CastPosition.To2D();
            Vector2 myPos = ObjectManager.Player.Position.To2D();
            Vector2 newPos = (vTarget.Position.To2D() - myPos);
            newPos.Normalize();

            Vector2 checkPos = predPos + newPos * (vSpell.Range - Vector2.Distance(predPos, myPos));
            Obj_Turret closestTower = null;

            foreach (
                Obj_Turret tower in
                    ObjectManager.Get<Obj_Turret>()
                        .Where(tower => tower.IsValid && !tower.IsDead && tower.Health != 0 && tower.IsEnemy))
            {
                if (Vector3.Distance(tower.Position, ObjectManager.Player.Position) < 1450)
                    closestTower = tower;
            }

            if (closestTower == null)
                return true;

            if (Vector2.Distance(closestTower.Position.To2D(), checkPos) <= 910)
                return false;

            return true;
        }

        private static void OnProcessSpellCast(Obj_AI_Base vTarget, GameObjectProcessSpellCastEventArgs args)
        {
            var stopUlties = Config.Item("StopUlties").GetValue<KeyBind>().Active;
            var forceInterrupt = Config.Item("ForceInterruptUlties").GetValue<KeyBind>().Active;

            if (!stopUlties || forceInterrupt) return;

                String[] interruptSpells = {
                    "AbsoluteZero",
                    "AlZaharNetherGrasp", 
		            "CaitlynAceintheHole", 
		            "Crowstorm", 
		            "DrainChannel", 
		            "FallenOne", 
		            "GalioIdolOfDurand", 
		            "InfiniteDuress", 
		            "KatarinaR", 
		            "MissFortuneBulletTime", 
		            "Teleport", 
		            "Pantheon_GrandSkyfall_Jump", 
		            "ShenStandUnited", 
		            "UrgotSwap2"
                };

            foreach (string interruptSpellName in interruptSpells)
            {
                if (vTarget.Team != vPlayer.Team && args.SData.Name == interruptSpellName)
                {
                    if (vPlayer.Health < vTarget.Health)
                    {
                        if (forceInterrupt || vPlayer.Distance(vTarget) >= E.Range || vPlayer.Distance(vTarget) <= Q.Range + E.Range)
                        {
                            var vMinions = MinionManager.GetMinions(vPlayer.ServerPosition, Q.Range, MinionTypes.All,
                                MinionTeam.NotAlly);
                            foreach (var vMinion in vMinions)
                            {
                                if (vMinion.Distance(vTarget) <= E.Range || vMinion.Distance(vPlayer) <= Q.Range) 
                                {
                                    Q.CastOnUnit(vMinion);
                                    if (vPlayer.Distance(vTarget) <= E.Range)
                                        E.CastOnUnit(vTarget);
                                }
                            }
                        }
                        else if (vPlayer.Distance(vTarget) <= E.Range && E.IsReady())
                            E.CastOnUnit(vTarget);
                    }
                }
            }
            /*
            if (stopUlties)
            {
                foreach (string interruptSpellName in interruptSpells)
                {
                    if (vTarget.Team != vPlayer.Team && args.SData.Name == interruptSpellName)
                    {
                        if (vPlayer.Distance(vTarget) <= E.Range && E.IsReady() && isStunPossible(vTarget))
                            E.CastOnUnit(vTarget);
                    }
                }
            }
            */
        }

        private static InventorySlot GetInventorySlot(int ID)
        {
            return
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId) ID && item.Stacks >= 1) || (item.Id == (ItemId) ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget != null)
            {
                foreach (MenuItem menuItem in MenuTargetedItems.Items)
                {
                    var useItem = MenuTargetedItems.Item(menuItem.Name).GetValue<bool>();
                    if (useItem)
                    {
                        var itemID = Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4));
                        if (Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null)
                            Items.UseItem(itemID, vTarget);
                    }
                }

                foreach (MenuItem menuItem in MenuNonTargetedItems.Items)
                {
                    var useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>();
                    if (useItem)
                    {
                        var itemID = Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4));
                        if (Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null)
                            Items.UseItem(itemID);
                    }
                }

            }
        }
    }
}
