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
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static string[] xWards =
        {
            "RelicSmallLantern", "RelicLantern", "SightWard", "wrigglelantern",
            "ItemGhostWard", "VisionWard", "BantamTrap", "JackInTheBox", "CaitlynYordleTrap", "Bushwhack"
        };

        public static Map map;

        private static SpellSlot IgniteSlot;
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
            if (Player.BaseSkinName != "Jax")
                return;
            if (Player.IsDead)
                return;

            Q = new Spell(SpellSlot.Q, 680f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Q.SetTargetted(0.50f, 75f);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            //Create the menu
            Config = new Menu("xQx | Jax", "Jax", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            new AssassinManager();

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("UseQComboDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboUseQMinRange", "Min. Q Range").SetValue(new Slider(250, (int) Q.Range)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));

            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassMode", "Harass Mode: ").SetValue(
                        new StringList(new[] { "Q+W", "Q+E", "Default" })));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass").SetValue(
                        new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("UseQLaneClearDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJungleFarm", "Use W").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            // Extra
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            Config.AddSubMenu(new Menu("WardJump", "WardJump"));
            Config.SubMenu("WardJump")
                .AddItem(new MenuItem("Ward", "Ward Jump"))
                .SetValue(new KeyBind('T', KeyBindType.Press));

            // Extras -> Use Items 
            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Extras").AddSubMenu(menuUseItems);

            // Extras -> Use Items -> Targeted Items
            MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(MenuTargetedItems);
            MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));

            // Extras -> Use Items -> AOE Items
            MenuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(MenuNonTargetedItems);
            MenuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawQRange", "Q range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawQMinRange", "Min. Q range").SetValue(
                        new Circle(true, System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawWard", "Ward Range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            /* [ Damage After Combo ] */
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

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>",
                    ChampionName));
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQRange = Config.Item("DrawQRange").GetValue<Circle>();
            if (drawQRange.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawQRange.Color, 1);
            }

            var drawWard = Config.Item("DrawWard").GetValue<Circle>();
            if (drawWard.Active)
            {
                Render.Circle.DrawCircle(Player.Position, wardRange, drawWard.Color, 1);
            }

            var drawMinQRange = Config.Item("DrawQMinRange").GetValue<Circle>();
            if (drawMinQRange.Active)
            {
                var minQRange = Config.Item("ComboUseQMinRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(Player.Position, minQRange, drawMinQRange.Color, 1);
            }
        }

        private static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

            if (E.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Config.Item("item3153").GetValue<bool>() && Items.CanUseItem(3128))
                fComboDamage += ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            return (float) fComboDamage;
        }

        private static Obj_AI_Hero GetEnemy(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = Q.Range;

            if (!Config.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Config.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
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

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //   if (sender.Name.Contains("Missile") || sender.Name.Contains("Minion"))
        }

        public static void Obj_AI_Base_OnProcessSpellCast(LeagueSharp.Obj_AI_Base obj,
            LeagueSharp.GameObjectProcessSpellCastEventArgs arg)
        {
            if (!xWards.ToList().Contains(arg.SData.Name))
                return;

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
                DelayTick = Environment.TickCount;
            }

            if (Config.Item("Ward").GetValue<KeyBind>().Active)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
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
            var t = GetEnemy(Q.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            var minQRange = Config.Item("ComboUseQMinRange").GetValue<Slider>().Value;
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            if (Q.IsReady() && useQ && Player.Distance(t) >= minQRange && ObjectManager.Player.Distance(t) <= Q.Range)
            {
                if (E.IsReady())
                    E.Cast();

                if (useQDontUnderTurret)
                {
                    if (!t.UnderTurret())
                        Q.Cast(t);
                }
                else
                {
                    Q.Cast(t);
                }
            }

            if (ObjectManager.Player.Distance(t) <= E.Range)
                UseItems(t);

            if (W.IsReady() && useW &&
                ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(t)) > 0)
                W.Cast();

            if (E.IsReady() && useE &&
                ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(t)) > 0)
                E.Cast();

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health &&
                    ObjectManager.Player.Distance(t) <= 500)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, t);
                }
            }

            if (R.IsReady() && useR)
            {
                if (Player.Distance(t) < Player.AttackRange)
                {
                    if (
                        ObjectManager.Player.CountEnemiesInRange(
                            (int) Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) >= 2 ||
                        t.Health > Player.Health)
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

            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            switch (Config.Item("HarassMode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                {
                    if (Q.IsReady() && W.IsReady() && qTarget != null && useQ && useW)
                    {
                        if (useQDontUnderTurret)
                        {
                            if (!Utility.UnderTurret(qTarget))
                            {
                                Q.Cast(qTarget);
                                W.Cast();
                            }
                        }
                        else
                        {
                            Q.Cast(qTarget);
                            W.Cast();
                        }
                    }
                    break;
                }
                case 1:
                {
                    if (Q.IsReady() && E.IsReady() && qTarget != null && useQ && useE)
                    {
                        if (useQDontUnderTurret)
                        {
                            if (!Utility.UnderTurret(qTarget))
                            {
                                Q.Cast(qTarget);
                                E.Cast();
                            }
                        }
                        else
                        {
                            Q.Cast(qTarget);
                            E.Cast();
                        }
                    }
                    break;
                }
                case 2:
                {
                    if (Q.IsReady() && useQ && qTarget != null && useQ)
                    {
                        if (useQDontUnderTurret)
                        {
                            if (!Utility.UnderTurret(qTarget))
                                Q.Cast(qTarget);
                        }
                        else
                            Q.Cast(qTarget);
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

            var vMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            foreach (var vMinion in vMinions)
            {
                if (useQ && Q.IsReady() && Player.Distance(vMinion) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    if (useQDontUnderTurret)
                    {
                        if (!Utility.UnderTurret(vMinion))
                            Q.Cast(vMinion);
                    }
                    else
                        Q.Cast(vMinion);
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

            var mobs = MinionManager.GetMinions(
                Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            if (Q.IsReady() && useQ && Player.Distance(mobs[0]) > Player.AttackRange)
                Q.Cast(mobs[0]);

            if (W.IsReady() && useW)
                W.Cast();

            if (E.IsReady() && useE)
                E.CastOnUnit(Player);
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base t, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells)
                return;
            if (!E.IsReady())
                return;

            if (Player.Distance(t) < Q.Range &&
                ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(t)) > 0 && Q.IsReady())
            {
                E.Cast();
                Q.Cast(t);
            }

            if (Player.Distance(t) <= E.Range)
            {
                E.Cast();
            }
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
            if (vTarget == null)
                return;

            foreach (var itemID in from menuItem in MenuTargetedItems.Items
                let useItem = MenuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4))
                into itemID
                where Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null
                select itemID)
            {
                Items.UseItem(itemID, vTarget);
            }

            foreach (var itemID in from menuItem in MenuNonTargetedItems.Items
                let useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4))
                into itemID
                where Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null
                select itemID)
            {
                Items.UseItem(itemID);
            }
        }
    }
}
