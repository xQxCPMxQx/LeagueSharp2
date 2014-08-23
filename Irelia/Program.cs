#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
#endregion

namespace Irelia
{
    internal class Program
    {
        public const string ChampionName = "Irelia";
        private static readonly Obj_AI_Hero vIrelia = ObjectManager.Player;

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
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;
        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (vIrelia.BaseSkinName != "Irelia") return;
            if (vIrelia.IsDead) return;
            
            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 1000f);

            Q.SetSkillshot(0.25f, 75f, 1500f, false, Prediction.SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 75f, 1500f, false, Prediction.SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.15f, 80f, 1500f, false, Prediction.SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vIrelia.GetSpellSlot("SummonerDot");
            
            //Create the menu
            Config = new Menu("Irelia", "Irelia", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttacks(true);
          
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
                .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));
            
            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Menu laneClearUseQ = new Menu("Use Q", "laneClearUseQ");
            Config.SubMenu("LaneClear").AddSubMenu(laneClearUseQ);
                laneClearUseQ.AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
                laneClearUseQ.AddItem(new MenuItem("UseQLaneClearDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("QFarmDelay", "Q Farm Delay").SetValue(new Slider(200, 500, 0)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJungleFarm", "Use W").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Extras
            Config.AddSubMenu(new Menu("Extras", "Extras"));
            Config.SubMenu("Extras").AddItem(new MenuItem("StopUlties", "Stop Ulties").SetValue(true));

            // Extras -> Use Items 
            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Extras").AddSubMenu(menuUseItems);

            // Extras -> Use Items -> Targeted Items
            MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(MenuTargetedItems);
            if (Utility.Map.GetMap() == Utility.Map.MapType.SummonersRift)
                MenuTargetedItems.AddItem(new MenuItem("item3128", "Deathfire Grasp").SetValue(true));
            else
                MenuTargetedItems.AddItem(new MenuItem("item3188", "Blackfire Torch").SetValue(true));
        
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
            MenuNonTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));

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
                    Utility.DrawCircle(vIrelia.Position, spell.Range, menuItem.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //if (!Orbwalking.CanMove(50)) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                {
                   // var existsMana = ObjectManager.Player.MaxMana * (Config.Item("LaneClearMana").GetValue<Slider>().Value / 100.0);
                   // if (vIrelia.Mana > existsMana)
                        LaneClear();
                }

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }
        }

        private static void CastSpellQ(bool CheckEnemyUnderTurretPosition = false)
        {
            var vTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (vTarget != null)
            {
                if (CheckEnemyUnderTurretPosition)
                {
                    if (!Utility.UnderTurret(vTarget))
                        Q.CastOnUnit(vTarget);
                }
                else
                    Q.CastOnUnit(vTarget);
            }
        }

        private static void CastSpellE()
        {
            var vTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            if (vTarget != null)
                E.CastOnUnit(vTarget);
        }

        private static void CastSpellW()
        {
            var vTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.True);
            if (vTarget != null && Vector3.Distance(vTarget.ServerPosition, vIrelia.Position) <=
                vIrelia.AttackRange)
            {
                if (R.IsReady()) /* Protect the mana for the Spell R. */
                {
                    if (vIrelia.Mana >= vIrelia.Spellbook.GetSpell(SpellSlot.W).ManaCost +
                            vIrelia.Spellbook.GetSpell(SpellSlot.E).ManaCost)
                        W.Cast();
                }
                else
                {
                    W.Cast();
                }
            }
        }
        private static void CastSpellR()
        {
            var vTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            if (vTarget != null)
                if (R.IsReady() && GetComboDamage(vTarget) > vTarget.Health)
                {
                    R.Cast(vTarget, false, true);
                    UseItems(vTarget);
                }
        }
        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            if (Q.IsReady() && useQ)// && vIrelia.Mana > existsMana)
                CastSpellQ(useQDontUnderTurret);

            if (E.IsReady() && useE)
                CastSpellE();

            if (W.IsReady() && useW)
                CastSpellW();

            if (R.IsReady() && useR)
                CastSpellR();
        }


        private static void Harass()
        {
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
                            CastSpellQ(useQDontUnderTurret);
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
                            CastSpellQ(useQDontUnderTurret);
                            CastSpellE();
                        }
                        break;
                    }
            }


            if (Q.IsReady() && useQ)
                CastSpellQ();

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

                var mobs = MinionManager.GetMinions(vIrelia.ServerPosition, Q.Range,
                    MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Count > 0)
                {
                    if (Q.IsReady() && useQ) 
                        Q.CastOnUnit(mobs[0]);

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
                
                var vMinions = MinionManager.GetMinions(vIrelia.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    var vMinionQDamage = DamageLib.getDmg(vMinion, DamageLib.SpellType.Q);
                    var vMinionEDamage = DamageLib.getDmg(vMinion, DamageLib.SpellType.E);

                    var qFarmDelay = (Config.Item("QFarmDelay").GetValue<Slider>().Value);

                    if (vMinion.Health <= vMinionQDamage)
                    {
                            Game.PrintChat((Game.Time * 1000).ToString());
                            if ((Game.Time * 1000 - QUsedTime) > qFarmDelay * 3)
                            {
                                Q.CastOnUnit(vMinion);
                                QUsedTime = Game.Time * 1000;
                            }
                    }

                    if (W.IsReady() && useW)
                        W.Cast();

                    if (E.IsReady() && useE)
                    {
                        if (vMinion.Health <= vMinionEDamage)
                            E.CastOnUnit(vMinion);
                    }
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.Q);
            
            if (E.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.E);
            
            if (R.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.R) * 4;

            if (IgniteSlot != SpellSlot.Unknown && vIrelia.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.IGNITE);

            if (Config.Item("item3153").GetValue<bool>() && Items.CanUseItem(3153))
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.BOTRK);

            return (float)fComboDamage;
        }
        
        private static bool isStunPossible(Obj_AI_Base vTarget)
        {
            return vIrelia.Health < vTarget.Health;
        }

        public static bool IsPositionSafe(Obj_AI_Base vTarget, Spell vSpell) 
        {
            Vector2 predPos = vSpell.GetPrediction(vTarget).Position.To2D();
            Vector2 myPos = ObjectManager.Player.Position.To2D();
            Vector2 newPos = (vTarget.Position.To2D() - myPos);
            newPos.Normalize();

            Vector2 checkPos = predPos + newPos * (vSpell.Range - Vector2.Distance(predPos, myPos));
            Obj_Turret closestTower = null;

            foreach (Obj_Turret tower in ObjectManager.Get<Obj_Turret>().Where(tower => tower.IsValid && !tower.IsDead && tower.Health != 0 && tower.IsEnemy))
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

            if (stopUlties)
            {
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
                    if (vTarget.Team != vIrelia.Team && args.SData.Name == interruptSpellName)
                    {
                        if (vIrelia.Distance(vTarget) <= E.Range && E.IsReady() && isStunPossible(vTarget)) /* stun possible */
                            E.CastOnUnit(vTarget);
                    }
                }
            }
        }

        private static InventorySlot GetInventorySlot(int ID)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(item => (item.Id == (ItemId)ID && item.Stacks >= 1) || (item.Id == (ItemId)ID && item.Charges >= 1));
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
