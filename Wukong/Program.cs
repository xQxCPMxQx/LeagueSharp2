#region

using System;
using System.Collections.Generic;
using System.Drawing;
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
        private static readonly string space = "    ";
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SpellSlot SmiteSlot = SpellSlot.Unknown;
        public static Spell Smite;
        private static readonly int[] SmitePurple = {3713, 3726, 3725, 3726, 3723};
        private static readonly int[] SmiteGrey = {3711, 3722, 3721, 3720, 3719};
        private static readonly int[] SmiteRed = {3715, 3718, 3717, 3716, 3714};
        private static readonly int[] SmiteBlue = {3706, 3710, 3709, 3708, 3707};
        private static readonly Items.Item Tiamat = new Items.Item(3077, 450);
        private static readonly SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");
        //Menu
        public static Menu Config;
        public static Menu MenuMisc;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;
        public static int ultUsed;

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Items.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Items.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "MonkeyKing")
                return;
            if (Player.IsDead)
                return;

            Q = new Spell(SpellSlot.Q, 375f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 640f);
            R = new Spell(SpellSlot.R, 375f);

            E.SetTargetted(0.5f, 2000f);

            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            Config = new Menu("xQx | Monkey King", "MonkeyKing", true);
            
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            AssassinManager.Load();

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            SetSmiteSlot();

            var menuCombo = new Menu("R", "R");
            // Combo
            Config.AddSubMenu(menuCombo);
            menuCombo.AddItem(new MenuItem("UseRComboEnemyCount", "Use R if enemy count >= (0 = off)").SetValue(new Slider(1, 5, 0)));
            menuCombo.AddItem(new MenuItem("UseRForTheEnemy", "Force Ultimate For:"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                menuCombo.AddItem(new MenuItem("forceUlti" + enemy.ChampionName, space + enemy.ChampionName).SetValue(new StringList(new[] { "Off", "Everytime", "Just Killable" })));

            if (SmiteSlot != SpellSlot.Unknown)
                menuCombo.AddItem(new MenuItem("ComboSmite", "Use Smite").SetValue(true));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarassTurret", "Don't Under Turret E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseTiamat", "Tiamat").SetValue(true));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseTiamat", "Tiamat").SetValue(true));

            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            // Extras -> Use Items 
            MenuMisc = new Menu("Misc", "Misc");
            Config.AddSubMenu(MenuMisc);
            MenuMisc.AddItem(new MenuItem("Misc.AutoQ", "Auto Q if it'll hit").SetValue(true));
            MenuMisc.AddItem(new MenuItem("Misc.BlockR", "Block R if it won't hit").SetValue(false));
            MenuMisc.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            var menuUseItems = new Menu("Use Items", "menuUseItems");
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
                    new MenuItem("QRange", "Q Range").SetValue(
                        new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E Range").SetValue(
                        new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("RRange", "R Range").SetValue(
                        new Circle(false, Color.FromArgb(255, 255, 255, 255))));

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

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Notifications.AddNotification(string.Format("{0} Loaded", ChampionName));

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color, 1);
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "MonkeyKingSpinToWin")
            {
                ultUsed = (int) Game.Time;
            }
        }

        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Config.Item("Misc.BlockR").GetValue<bool>())
            {
                return;
            }

            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (args.Slot == SpellSlot.R && !t.IsValidTarget())
            {
                args.Process = false;
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100))
                return;

            if (Config.Item("Misc.AutoQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Q.Range - 20f, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && Q.IsReady())
                {
                    Q.CastOnUnit(t);
                    Player.IssueOrder(GameObjectOrder.AttackTo, t);
                }
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && (int) Game.Time > ultUsed + 4)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active && (int) Game.Time > ultUsed + 4)
            {
                var vMana = Config.Item("HarassMana").GetValue<Slider>().Value;
                if (Player.ManaPercent >= vMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var vMana = Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.ManaPercent >= vMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var vMana = Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (Player.ManaPercent >= vMana)
                    JungleFarm();
            }
        }

        private static void Combo()
        {
            var t = GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            if (E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.CastOnUnit(t);
            }

            if (SmiteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
            {
                Smiteontarget(t);
            }

            if (Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }

            if (E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.CastOnUnit(t);
            }

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health && Player.Distance(t) <= 550)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, t);
                }
            }

            if (R.IsReady())
            {
                var valR = Config.Item("UseRComboEnemyCount").GetValue<Slider>().Value;
                if (valR > 0 && Player.CountEnemiesInRange(R.Range) >= valR)
                {
                    R.Cast();
                }
                else if (Config.Item("forceUlti" + t.ChampionName) != null && t.IsValidTarget(R.Range))
                {
                    switch (Config.Item("forceUlti" + t.ChampionName).GetValue<StringList>().SelectedIndex)
                    {
                        case 1:
                            R.CastIfHitchanceEquals(t, HitChance.High);
                            break;
                        case 2:
                            {
                                if (t.Health < GetComboDamage(t))
                                    R.CastIfHitchanceEquals(t, HitChance.High);
                            }
                            break;
                    }
                }
            }

            UseItems(t);
        }

        private static void Harass()
        {
            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady();
            var useE = Config.Item("UseEHarass").GetValue<bool>() && E.IsReady();

            if (useQ && t.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }

            if (useE && t.IsValidTarget(E.Range))
            {
                if (Config.Item("UseEHarassTurret").GetValue<bool>())
                {
                    if (!t.UnderTurret())
                        E.CastOnUnit(t);
                }
                else
                    E.CastOnUnit(t);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(
                Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            var mob = mobs[0];
            if (mobs.Count > 0)
            {
                if (useQ && Q.IsReady())
                    Q.Cast();

                if (useE && E.IsReady())
                    E.CastOnUnit(mob);
            }

            if (Tiamat.IsReady() && Config.Item("JungleFarmUseTiamat").GetValue<bool>())
            {
                if (mobs.Count >= 2)
                    Tiamat.Cast(Player);
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>() && Q.IsReady();
            var useE = Config.Item("UseELaneClear").GetValue<bool>() && E.IsReady();

            if (useQ)
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

                foreach (var vMinion in from vMinion in minionsQ
                    let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                    select vMinion)
                {
                    Q.Cast();
                }
            }

            if (useE)
            {
                var allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range);
                if (allMinionsE.Count >= 2)
                    E.CastOnUnit(allMinionsE[0]);
                /*
                var locE = E.GetCircularFarmLocation(allMinionsE);
                
                if (allMinionsE.Count == allMinionsE.Count(m => Player.Distance(m) < E.Range) && locE.MinionsHit >= 2 &&
                    locE.Position.IsValid())
                    E.Cast(locE.Position);
                */
            }

            if (Tiamat.IsReady() && Config.Item("LaneClearUseTiamat").GetValue<bool>())
            {
                var allMinions = MinionManager.GetMinions(
                    Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player));
                var locTiamat = E.GetCircularFarmLocation(allMinions);
                if (locTiamat.MinionsHit >= 3)
                    Tiamat.Cast(Player);
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

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            return (float) fComboDamage;
        }
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells)
                return;

            if (Player.Distance(unit) < Orbwalking.GetRealAutoAttackRange(Player) && R.IsReady())
            {
                R.Cast();
            }
        }

       
        private static InventorySlot GetInventorySlot(int id)
        {
            return
                Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId) id && item.Stacks >= 1) || (item.Id == (ItemId) id && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget == null)
                return;

            foreach (var itemId in from menuItem in MenuTargetedItems.Items
                let useItem = MenuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                into itemId
                where Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                select itemId)
            {
                Items.UseItem(itemId, vTarget);
            }

            foreach (var itemId in from menuItem in MenuNonTargetedItems.Items
                let useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                into itemId
                where Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                select itemId)
            {
                if (Player.Distance(vTarget) < 350)
                    Items.UseItem(itemId);
            }
        }

        private static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
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
                            enemy.Team != Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Config.Item("Assassin" + enemy.ChampionName) != null &&
                            Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            Player.Distance(enemy) < assassinRange);

            if (Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
                Smite = new Spell(SmiteSlot, 700);
            }
        }

        private static void Smiteontarget(Obj_AI_Hero t)
        {
            var useSmite = Config.Item("ComboSmite").GetValue<bool>();
            var itemCheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
            if (itemCheck && useSmite &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(ObjectManager.Player.Position) < Smite.Range)
            {
                ObjectManager.Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }
    }
}
