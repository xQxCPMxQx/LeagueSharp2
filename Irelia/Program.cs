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

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        //Menu
        public static Menu Config;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
        }

        public static void LoadSpellList()
        {
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
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (vIrelia.BaseSkinName != "Irelia") return;
            string message = String.Format("{0} Loaded!", ChampionName);

            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx:</font> <font color='#FFFFFF'>{0}</font>", message));
            LoadSpellList();

            //Create the menu
            Config = new Menu("Irelia", "Irelia", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            
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
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q", "E", "Q+E"}))); 

            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));
            
            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            //Config.SubMenu("LaneClear").AddItem(new MenuItem("QFarmDelay", "Q Farm Delay").SetValue(new Slider(250, 500, 0)));

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
            MenuTargetedItems.AddItem(new MenuItem("item3188", "Blackfire Torch").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3128", "Deathfire Grasp").SetValue(true));
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

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
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
            if (!Orbwalking.CanMove(100)) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
                return;
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Combo();
                return;
            }
            
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                LaneClear();

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                JungleFarm();
        }

        private static void Combo()
        {
            var ComboActive = Config.Item("ComboActive").GetValue<KeyBind>().Active;
            var HarassActive = Config.Item("HarassActive").GetValue<KeyBind>().Active;

            if (ComboActive || HarassActive)
            {
                var useQ = Config.Item("UseQ" + (ComboActive ? "Combo" : "Harass"))
                    .GetValue<bool>();
                var useW = Config.Item("UseQ" + (ComboActive ? "Combo" : "Harass"))
                    .GetValue<bool>();
                var useE = Config.Item("UseQ" + (ComboActive ? "Combo" : "Harass"))
                    .GetValue<bool>();
                var useR = Config.Item("UseQCombo").GetValue<bool>();

                var mana = ObjectManager.Player.MaxMana * (Config.Item("HarassMana")
                    .GetValue<Slider>().Value / 100.0);

                if (HarassActive)
                {
                    var vTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                    int vHarassMode = Config.Item("HarassMode").GetValue<StringList>().SelectedIndex;

                    switch (vHarassMode)
                    {
                        case 0:
                            {
                                if (Q.IsReady())
                                    Q.CastOnUnit(vTarget);
                                break;
                            }
                        case 1:
                            {
                                if (E.IsReady())
                                    E.CastOnUnit(vTarget);
                                break;
                            }
                        case 2:
                            {
                                if (Q.IsReady() && E.IsReady())
                                {
                                    Q.CastOnUnit(vTarget);
                                    E.CastOnUnit(vTarget);
                                }
                                break;
                            }
                    }
                }

                if (Q.IsReady() && useQ)
                {
                    var vTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                    if (vTarget != null)
                        if (IsPositionSafe(vTarget, Q))
                            Q.CastOnUnit(vTarget);
                }

                if (E.IsReady() && useE)
                {
                    var vTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
                    if (vTarget != null)
                        E.CastOnUnit(vTarget);
                }

                if (W.IsReady() && useW)
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

                if (R.IsReady() && useR)
                {
                    var vTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                    if (vTarget != null)
                        if (R.IsReady() && useR && GetComboDamage(vTarget) > vTarget.Health)
                        {
                            R.Cast(vTarget, false, true);
                            UseItems(vTarget);
                        }
                }
              
            }
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
                    var vMinionQDamage = DamageLib.getDmg(vMinion, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage);
                    var vMinionEDamage = DamageLib.getDmg(vMinion, DamageLib.SpellType.E, DamageLib.StageType.FirstDamage);

                    if (vMinion.IsValidTarget(Q.Range) && HealthPrediction.GetHealthPrediction(vMinion, (int)Q.Delay) < vMinionQDamage && Q.IsReady() && useQ)
                    {
                        if (IsPositionSafe(vMinion, Q))
                        {
                            Q.CastOnUnit(vMinion);
                            /*
                            var qFarmDelay = (Config.Item("QFarmDelay").GetValue<Slider>().Value);
                            Utility.DelayAction.Add(500, () => { Q.CastOnUnit(vMinion); });
                            */
                        }
                    }

                    if (W.IsReady() && useW)
                        W.Cast();

                    if (vMinion.IsValidTarget(Q.Range) && HealthPrediction.GetHealthPrediction(vMinion, (int)E.Delay) < vMinionEDamage && E.IsReady() && useE)
                    {
                        E.CastOnUnit(vMinion);
                    }
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Base target)
        {
            float comboDamage = 0;

            if ((vIrelia.Spellbook.GetSpell(SpellSlot.Q).Level) > 0)
                comboDamage += Q.GetDamage(target);
            if ((vIrelia.Spellbook.GetSpell(SpellSlot.E).Level) > 0)
                comboDamage += E.GetDamage(target);
            if ((vIrelia.Spellbook.GetSpell(SpellSlot.R).Level) > 0)
                comboDamage += R.GetDamage(target) * 4;

            return comboDamage;
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

            foreach (Obj_Turret tower in ObjectManager.Get<Obj_Turret>().Where(tower => tower.IsValid && !tower.IsDead && tower.Health != 0))
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
        
        private static void OnProcessSpell(Obj_AI_Base vTarget, GameObjectProcessSpellCastEventArgs args)
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
                        var itemCode = menuItem.Name.ToString().Substring(4, 4);
                        var itemID = Convert.ToInt16(itemCode);
                        if (Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null) 
                            Items.UseItem(Convert.ToInt16(itemCode), vTarget);
                    }
                }

                foreach (MenuItem menuItem in MenuNonTargetedItems.Items)
                {
                    var useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>();
                    if (useItem)
                    {
                        var itemCode = menuItem.Name.ToString().Substring(4, 4);
                        var itemID = Convert.ToInt16(itemCode);
                        if (Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null)
                            Items.UseItem(Convert.ToInt16(itemCode));
                    }
                }

            }
        }


    }
}
