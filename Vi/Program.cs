#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
#endregion

namespace Vi
{
    internal class Program
    {
        public const string ChampionName = "Vi";
        private static readonly Obj_AI_Hero vPlayer = ObjectManager.Player;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell E;
        public static Spell E2;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;
        public static int DelayTick = 0;
        //Menu
        private static Menu Config;
        private static Menu MenuTargetedItems;
        private static Menu MenuNonTargetedItems;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (vPlayer.BaseSkinName != "Vi") return;
            if (vPlayer.IsDead) return;

            Q = new Spell(SpellSlot.Q, 850f);
            E = new Spell(SpellSlot.E, 200f);
            E2 = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 800f);

            Q.SetSkillshot(0.50f, 75f, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 150f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.15f, 80f, 1500f, false, SkillshotType.SkillshotCone);

            Q.SetCharged("ViQ", "ViQ", 100, 850, 1f);

            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vPlayer.GetSpellSlot("SummonerDot");
            SmiteSlot = vPlayer.GetSpellSlot("SummonerSmite");

            //Create the menu
            Config = new Menu("xQx | Vi", "Vi", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttacks(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0],
                        KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q", "E", "Q+E" })));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Extras
            Config.AddSubMenu(new Menu("Extras", "Extras"));
            Config.SubMenu("Extras").AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            // Extras -> Use Items 
            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Extras").AddSubMenu(menuUseItems);
            
            // Extras -> Other
            Config.SubMenu("Extras").AddItem(new MenuItem("AutoSmite", "Auto Smite").SetValue<KeyBind>(new KeyBind('N', KeyBindType.Toggle)));
               

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

            Config.SubMenu("Drawings").AddItem(new MenuItem("WQRange", "W-Q range").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));

        }
         private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color, 1, 5);
            }
        }

        private static void UseSummoners()
        {
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                float[] SmiteDmg = { 20 * vPlayer.Level + 370, 30 * vPlayer.Level + 330, 40 * vPlayer.Level + 240, 50 * vPlayer.Level + 100 };
                string[] MonsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
                var vMinions = MinionManager.GetMinions(vPlayer.ServerPosition, vPlayer.SummonerSpellbook.Spells.FirstOrDefault(
                    spell => spell.Name == "SummonerSmite").SData.CastRange[0], MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    if (vMinion != null 
                        && !vMinion.IsDead 
                        && !vPlayer.IsDead 
                        && !vPlayer.IsStunned 
                        && SmiteSlot != SpellSlot.Unknown 
                        && vPlayer.SummonerSpellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                    {
                        if ((vMinion.Health < SmiteDmg.Max()) && (MonsterNames.Any(name => vMinion.BaseSkinName.StartsWith(name))))
                        {
                            vPlayer.SummonerSpellbook.CastSpell(SmiteSlot, vMinion);
                        }
                    }
                }
            }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100)) return;
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
                var existsMana = vPlayer.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
                if (vPlayer.Mana >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = vPlayer.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (vPlayer.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = vPlayer.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value; 
                if (vPlayer.Mana >= existsMana)
                    JungleFarm();
            }

        }

        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            if (Q.IsReady() && useQ)
            {
                var vTarget = SimpleTs.GetTarget(Q.ChargedMaxRange, SimpleTs.DamageType.Physical);
                if (vTarget != null)
                {
                    UseItems(vTarget);
                    if (Q.IsCharging)
                    {
                        Q.Cast(vTarget);
                        UseItems(vTarget);
                    }
                    else 
                    {
                        Q.StartCharging();
                    }

                }
            }

            if (E.IsReady() && useE)
            {
                var ETarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
                var E2Target = SimpleTs.GetTarget(E2.Range, SimpleTs.DamageType.Physical);
                UseItems(E2Target);
                if (ETarget != null)
                    E.Cast(ETarget);
                else 
                if (E2Target != null && EMinion != null)
                    E.Cast(EMinion);
            }

            if (R.IsReady() || useR)
            {
                var vTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                if (vTarget != null)
                    if (R.IsReady() && GetComboDamage(vTarget) > vTarget.Health)
                    {
                        R.Cast(vTarget, false, true);
                        UseItems(vTarget);
                    }
            }
        }

        private static void Harass()
         {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

            if (Q.IsReady() && useQ)
            {
                var vTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                if (vTarget != null)
                    Q.Cast(vTarget);
            }

            if (E.IsReady() && useE)
            {
                var vTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
                if (vTarget != null)
                    E.Cast(vTarget);
            }
        }
        
        private static void JungleFarm()
        {
            var jungleFarmActive = Config.Item("JungleFarmActive").GetValue<KeyBind>().Active;

            if (jungleFarmActive)
            {
                var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
                var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

                var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E2.Range, MinionTypes.All,
                    MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (useE && E.IsReady())
                    {
                        E.Cast(mob);
                    }
                    else if (useQ && Q.IsReady())
                    {
                        if (!Q.IsCharging)
                            Q.StartCharging();
                        else
                            Q.Cast(mob);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var laneClearActive = Config.Item("LaneClearActive").GetValue<KeyBind>().Active;
            if (laneClearActive)
            {
                var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
                var useE = Config.Item("UseELaneClear").GetValue<bool>();
                
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.ChargedMaxRange, MinionTypes.All);
                var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E2.Range, MinionTypes.All);

                if (useQ && Q.IsReady())
                {
                    if (Q.IsCharging)
                    {
                        var locQ = Q.GetLineFarmLocation(allMinionsQ);
                        if (allMinionsQ.Count == allMinionsQ.Count(m => vPlayer.Distance(m) < Q.Range) && locQ.MinionsHit > 2 && locQ.Position.IsValid())
                            Q.Cast(locQ.Position);
                    }
                    else if (allMinionsQ.Count > 2)
                        Q.StartCharging();
                }

                if (useE && E.IsReady())
                {
                    var locE = E.GetLineFarmLocation(allMinionsE);
                    if (allMinionsQ.Count == allMinionsQ.Count(m => vPlayer.Distance(m) < E2.Range) && locE.MinionsHit > 2 && locE.Position.IsValid())
                        E.Cast(locE.Position);
                }
            }
        }

        public static bool Intersection(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        /* Credits to DETUKS https://github.com/detuks/LeagueSharp/blob/master/YasuoSharp/YasMath.cs */
        {
            var p3 = new Vector2(pC.X + radius, pC.Y + radius);
            var m = ((p2.Y - p1.Y) / (p2.X - p1.X));
            var constant = (m * p1.X) - p1.Y;
            var b = -(2f * ((m * constant) + p3.X + (m * p3.Y)));
            var a = (1 + (m * m));
            var c = ((p3.X * p3.X) + (p3.Y * p3.Y) - (radius * radius) + (2f * constant * p3.Y) + (constant * constant));
            var d = ((b * b) - (4f * a * c));

            return d > 0;
        }

        public static Obj_AI_Base EMinion
        {
            get
            {
                var vTarget = SimpleTs.GetTarget(E2.Range, SimpleTs.DamageType.Physical);
                var vMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.None);

                return (from vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(E.Range))
                        let endPoint =
                            vMinion.ServerPosition.To2D()
                                .Extend(ObjectManager.Player.ServerPosition.To2D(), - E.Range)
                                .To3D()
                        where
                            Intersection(
                                ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(), vTarget.ServerPosition.To2D(),
                                vTarget.BoundingRadius + E.Width / 2)
                        select vMinion).FirstOrDefault();
            }
        }
        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.Q);

            if (E.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.E) * E.Instance.Ammo;
            
            if (R.IsReady())
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.R);

            if (Config.Item("item3153").GetValue<bool>() && Items.CanUseItem(3153))
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.BOTRK);

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += DamageLib.getDmg(vTarget, DamageLib.SpellType.IGNITE);

            return (float)fComboDamage;
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells) return;

            if (vPlayer.Distance(vTarget) < Q.Range)
            {
                Q.Cast(vTarget);
            }
            else if (vPlayer.Distance(vTarget) < R.Range)
            {
                R.Cast(vTarget);
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
