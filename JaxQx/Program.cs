
#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace JaxQx
{
    internal class Program
    {
        public const string ChampionName = "Jax";
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell E;
        public static Spell W;
        public static Spell R;

        public static string[] TestSpells = { "RelicSmallLantern", 
                                              "RelicLantern", 
                                              "SightWard", 
                                              "wrigglelantern", 
                                              "ItemGhostWard", 
                                              "VisionWard", 
                                              "BantamTrap", 
                                              "JackInTheBox", 
                                              "CaitlynYordleTrap", 
                                              "Bushwhack" };

        public static Map map;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;
        public static float SmiteRange = 700f;
        public static float wardRange = 600f;
        public static int DelayTick = 0;
        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;
        
        private static void Main(string[] args)
        {
            map = new Map();
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != "Jax") return;
            if (Player.IsDead) return;
            
            Q = new Spell(SpellSlot.Q, 680f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 190f);
            R = new Spell(SpellSlot.R);
            
            Q.SetTargetted(0.50f, 75f);
            
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            
            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");
            
            //Create the menu
            Config = new Menu("xQx | Jax", "Jax", true);
            
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttacks(true);
            
            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQComboDontUnderTurret", "Don't Under Turret Q")
                .SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));

            Config.SubMenu("Combo")
                  .AddItem(
                       new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0],
                           KeyBindType.Press)));
            
            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Under Turret Q")
                .SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                  .AddItem(new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q+W", "Q+E", "Default" })));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                  .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0],
                      KeyBindType.Press)));
            
            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClearDontUnderTurret", "Don't Under Turret Q")
                .SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                  .AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                      KeyBindType.Press)));
            
            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJungleFarm", "Use W").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "Auto Smite").SetValue<KeyBind>(new KeyBind('N', KeyBindType.Toggle)));

            Config.SubMenu("JungleFarm")
                  .AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],
                      KeyBindType.Press)));
            
            // Extra
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            Config.AddSubMenu(new Menu("WardJump", "WardJump"));
            Config.SubMenu("WardJump").AddItem(new MenuItem("Ward", "Ward Jump")).SetValue(new KeyBind('T', KeyBindType.Press, false));
            
            // Extras -> Use Items 
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
            MenuNonTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));
            
            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQRange", "Q range").SetValue(new Circle(true,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawWard", "Ward Range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("SmiteRange", "Smite Range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            new PotionManager();
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));
        }
        
        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color, 1, 5);
            }

            var drawQRange = Config.Item("DrawQRange").GetValue<Circle>();
            if (drawQRange.Active)
            {
                Utility.DrawCircle(Player.Position, Q.Range, drawQRange.Color);
            }

            var drawWard = Config.Item("DrawWard").GetValue<Circle>();
            if (drawWard.Active)
            {
                Utility.DrawCircle(Player.Position, wardRange, drawWard.Color);
            }

        }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Missile") || sender.Name.Contains("Minion"))
                return;
        }

        public static void Obj_AI_Base_OnProcessSpellCast(LeagueSharp.Obj_AI_Base obj, LeagueSharp.GameObjectProcessSpellCastEventArgs arg)
        {
            if (!TestSpells.ToList().Contains(arg.SData.Name)) return;

            Jumper.testSpellCast = arg.End.To2D();
            Polygon pol;
            if ((pol = map.getInWhichPolygon(arg.End.To2D())) != null)
            {
                Jumper.testSpellProj = pol.getProjOnPolygon(arg.End.To2D());
            }
        }
        
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100))
                return;

            
            if (DelayTick - Environment.TickCount <= 250)
            {
                UseSummoners();
                DelayTick = Environment.TickCount;
            }
            
            if (Config.Item("Ward").GetValue<KeyBind>().Active)
            {
                Jumper.wardJump(Game.CursorPos.To2D());
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
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

            if (Q.IsReady() && useQ && qTarget != null)
            {
                if (useQDontUnderTurret)
                {
                    if (!Utility.UnderTurret(qTarget))
                        Q.CastOnUnit(qTarget);
                }
                else
                {
                    Q.CastOnUnit(qTarget);
                }

            }

            if (eTarget != null)
                UseItems(eTarget);

            if (W.IsReady() && useW && eTarget != null)
                W.Cast();

            if (E.IsReady() && useE && eTarget != null)
                E.Cast();

            if (IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(eTarget, Damage.SummonerSpell.Ignite) > eTarget.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, eTarget);
                }
            }

            if (R.IsReady() && useR && eTarget != null)
            {
                if (Player.Distance(eTarget) < Player.AttackRange)
                {
                    if (Utility.CountEnemysInRange((int)Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) >= 2 || eTarget.Health > Player.Health)
                    {
                        R.CastOnUnit(Player);
                    }
                }
            }
        }
        private static void Harass()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQHarassDontUnderTurret").GetValue<bool>();

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

            switch (Config.Item("HarassMode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    {
                        if (Q.IsReady() && W.IsReady() && qTarget != null)
                        {
                            if (useQDontUnderTurret)
                            {
                                if (!Utility.UnderTurret(qTarget))
                                {
                                    Q.CastOnUnit(qTarget);
                                    W.Cast();
                                }
                            }
                            else
                            {
                                Q.CastOnUnit(qTarget);
                                W.Cast();
                            }
                        }
                        break;
                    }
                case 1:
                    {
                        if (Q.IsReady() && E.IsReady() && qTarget != null)
                        {
                            if (useQDontUnderTurret)
                            {
                                if (!Utility.UnderTurret(qTarget))
                                {
                                    Q.CastOnUnit(qTarget);
                                    E.Cast();
                                }
                            }
                            else
                            {
                                Q.CastOnUnit(qTarget);
                                E.Cast();
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        if (Q.IsReady() && useQ && qTarget != null)
                        {
                            if (useQDontUnderTurret)
                            {
                                if (!Utility.UnderTurret(qTarget))
                                    Q.CastOnUnit(qTarget);
                            }
                            else
                                Q.CastOnUnit(qTarget);
                            UseItems(qTarget);
                        }

                        if (W.IsReady() && useW && eTarget != null)
                        {
                            W.Cast();
                        }

                        if (E.IsReady() && useE && eTarget != null)
                        {
                            E.CastOnUnit(Player);
                        }
                        break;
                    }
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQLaneClearDontUnderTurret").GetValue<bool>();

            var vMinions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
            foreach (var vMinion in vMinions)
            {
                if (useQ && Q.IsReady() && Player.Distance(vMinion) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    if (useQDontUnderTurret)
                    {
                        if (!Utility.UnderTurret(vMinion))
                            Q.CastOnUnit(vMinion);
                    }
                    else
                        Q.CastOnUnit(vMinion);
                }

                if (useW && W.IsReady())
                    W.Cast();

                if (useE && E.IsReady())
                    E.CastOnUnit(Player);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useW = Config.Item("UseWJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            if (Q.IsReady() && useQ && Player.Distance(mobs[0]) > Player.AttackRange)
                Q.CastOnUnit(mobs[0]);

            if (W.IsReady() && useW)
                W.Cast();

            if (E.IsReady() && useE)
                E.CastOnUnit(Player);
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells)
                return;
            if (!E.IsReady())
                return;

            if (Player.Distance(vTarget) < Q.Range && Player.Distance(vTarget) > E.Range && Q.IsReady())
            {
                E.Cast();
                Q.Cast(vTarget);
            }

            if (Player.Distance(vTarget) <= E.Range)
            {
                E.Cast();
            }
        }
        
        private static InventorySlot GetInventorySlot(int ID)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(item => (item.Id == (ItemId)ID && item.Stacks >= 1) || (item.Id == (ItemId)ID && item.Charges >= 1));
        }
        
        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget == null) return;

            foreach (var itemID in from menuItem in MenuTargetedItems.Items let useItem = MenuTargetedItems.Item(menuItem.Name).GetValue<bool>() where useItem select Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4)) into itemID where Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null select itemID)
            {
                Items.UseItem(itemID, vTarget);
            }

            foreach (var itemID in from menuItem in MenuNonTargetedItems.Items let useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>() where useItem select Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4)) into itemID where Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null select itemID)
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

            var vMonsters = MinionManager.GetMinions(Player.ServerPosition, firstOrDefault.SData.CastRange[0], MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
            foreach (var vMonster in vMonsters.Where(vMonster => vMonster != null
                                                              && !vMonster.IsDead
                                                              && !Player.IsDead
                                                              && !Player.IsStunned
                                                              && SmiteSlot != SpellSlot.Unknown
                                                              && Player.SummonerSpellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                                                              .Where(vMonster => (vMonster.Health < Player.GetSummonerSpellDamage(vMonster, Damage.SummonerSpell.Smite)) && (monsterNames.Any(name => vMonster.BaseSkinName.StartsWith(name)))))
            {
                Player.SummonerSpellbook.CastSpell(SmiteSlot, vMonster);
            }
        }

    }
}