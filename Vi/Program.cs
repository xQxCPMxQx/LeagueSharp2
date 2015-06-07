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
        private static bool canUseE;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell E;
        public static Spell E2;
        public static Spell R;

        public static Items.Item ItemBlade;
        public static Items.Item ItemBilge;
        public static Items.Item ItemHydra;
        public static Items.Item ItemLotis;
        public static Items.Item ItemRand;
        public static Items.Item ItemTiamat;

        private static SpellSlot IgniteSlot = SpellSlot.Unknown;
        private static SpellSlot FlashSlot = SpellSlot.Unknown;


        public static float FlashRange = 450f;
        public static int DelayTick = 0;

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        private static Menu _menuTargetedItems;
        private static Menu _menuNonTargetedItems;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnLoad;
        }

        private static void Game_OnLoad(EventArgs args)
        {
            if (vPlayer.ChampionName != "Vi")
                return;
            if (vPlayer.IsDead)
                return;

            Q = new Spell(SpellSlot.Q, 860f);
            E = new Spell(SpellSlot.E);
            E2 = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 800f);

            Q.SetSkillshot(0.5f, 75f, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q.SetCharged("ViQ", "ViQ", 100, 860, 1f);

            E.SetSkillshot(0.15f, 150f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetTargetted(0.15f, 1500f);

            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vPlayer.GetSpellSlot("SummonerDot");
            FlashSlot = vPlayer.GetSpellSlot("SummonerFlash");

            ItemBilge = new Items.Item(3144, 450f);
            ItemBlade = new Items.Item(3153, 450f);
            ItemHydra = new Items.Item(3074, 250f);
            ItemLotis = new Items.Item(3190, 590f);
            ItemRand = new Items.Item(3143, 490f);
            ItemTiamat = new Items.Item(3077, 250f);

            //Create the menu
            Config = new Menu("xQx | Vi", "Vi", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            new AssassinManager();

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));

            /* [ Don't Use Ult ] */
            Config.SubMenu("Combo").AddSubMenu(new Menu("Don't use Ult on", "DontUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != vPlayer.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }

            /* [ Find Him in Team Fight ] */
            Config.SubMenu("Combo").AddSubMenu(new Menu("Focus in TF", "FindHim"));
            Config.SubMenu("Combo")
                .SubMenu("FindHim")
                .AddItem(new MenuItem("ForceFocusActive", "Force Focus Active").SetValue(false));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != vPlayer.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("FindHim")
                    .AddItem(new MenuItem("FindHim" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }

            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboFlashQActive", "Combo Flash+Q!").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Menu harassUseQ = new Menu("Q Settings", "harassUseQ");
            Config.SubMenu("Harass").AddSubMenu(harassUseQ);
            harassUseQ.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            harassUseQ.AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass").SetValue(
                        new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
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
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            var menuRun = new Menu("Flee", "Flee");
            {
                menuRun.AddItem(
                    new MenuItem("FleeActive", "Flee!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                Config.AddSubMenu(menuRun);
            }

            // Extras -> Use Items 
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Extras").AddSubMenu(menuUseItems);
            // Extras -> Use Items -> Targeted Items
            _menuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(_menuTargetedItems);

            _menuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            _menuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));

            _menuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            _menuTargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));

            // Extras -> Use Items -> AOE Items
            _menuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(_menuNonTargetedItems);
            _menuNonTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            _menuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            _menuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            _menuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            _menuNonTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            _menuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("QRange", "Q Range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E Range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("RRange", "R Range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("FQRange", "Flash+Q Range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(0xFF, 0xCC, 0x00))));

            new PotionManager();
            Config.AddToMainMenu();

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;

            Notifications.AddNotification("xQx | Vi Loaded!", 4000);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color, 2);
            }

            var drawFqCombo = Config.Item("FQRange").GetValue<Circle>();
            if (drawFqCombo.Active)
            {
                Render.Circle.DrawCircle(vPlayer.Position, Q.Range + FlashRange, drawFqCombo.Color, 2);
            }
        }

        public static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe)
                return;
            
            var t = GetTarget(Orbwalking.GetRealAutoAttackRange(vPlayer) + 65, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            foreach (var xbuff in t.Buffs)
            {
                canUseE = !xbuff.Name.Contains("viq") && !xbuff.Name.Contains("knock");
            }

            var useE = canUseE && E.IsReady() && Config.Item("UseECombo").GetValue<bool>();

            if (useE)
            {
                E.Cast(true);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100))
                return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("ComboFlashQActive").GetValue<KeyBind>().Active)
            {
                ComboFlashQ();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Config.Item("HarassMana").GetValue<Slider>().Value;
                if (vPlayer.ManaPercent >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (vPlayer.ManaPercent >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (vPlayer.ManaPercent >= existsMana)
                    JungleFarm();
            }

            if (Config.Item("FleeActive").GetValue<KeyBind>().Active)
                Flee();

        }

        private static void Combo()
        {
            var t = GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var comboDamage = GetComboDamage(t);

            if (Q.IsReady() && useQ && t.IsValidTarget(Q.Range))
            {
                if (Q.IsCharging)
                {
                    Q.Cast(t);
                }
                else
                {
                    Q.StartCharging();
                }
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                UseItems(t);

            if (comboDamage > t.Health && IgniteSlot != SpellSlot.Unknown &&
                vPlayer.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                vPlayer.Spellbook.CastSpell(IgniteSlot, t);
            }

            if (R.IsReady())
            {
                useR = (Config.Item("DontUlt" + t.BaseSkinName) != null &&
                        Config.Item("DontUlt" + t.BaseSkinName).GetValue<bool>() == false) && useR;

                var qDamage = vPlayer.GetSpellDamage(t, SpellSlot.Q);
                var eDamage = vPlayer.GetSpellDamage(t, SpellSlot.E)*E.Instance.Ammo;
                var rDamage = vPlayer.GetSpellDamage(t, SpellSlot.R);

                if (Q.IsReady() && t.Health < qDamage)
                    return;

                if (E.IsReady() && Orbwalking.InAutoAttackRange(t) && t.Health < eDamage)
                    return;

                if (Q.IsReady() && E.IsReady() && t.Health < qDamage + eDamage)
                    return;

                if (useR)
                {
                    if (t.Health > rDamage)
                    {
                        if (Q.IsReady() && E.IsReady() && t.Health < rDamage + qDamage + eDamage)
                            R.CastOnUnit(t);
                        else if (E.IsReady() && t.Health < rDamage + eDamage)
                            R.CastOnUnit(t);
                        else if (Q.IsReady() && t.Health < rDamage + qDamage)
                            R.CastOnUnit(t);
                    }
                    else
                    {
                        if (!Orbwalking.InAutoAttackRange(t))
                            R.CastOnUnit(t);
                    }
                }
            }
        }

        private static void ComboFlashQ()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            var t = GetTarget(Q.Range + FlashRange - 20, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            if (vPlayer.Distance(t) > Q.Range)
            {
                if (FlashSlot != SpellSlot.Unknown && vPlayer.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready)
                {
                    if (Q.IsCharging && Q.Range >= Q.ChargedMaxRange)
                    {
                        vPlayer.Spellbook.CastSpell(FlashSlot, t.ServerPosition);
                        Q.Cast(t.ServerPosition);
                    }
                    else
                    {
                        Q.StartCharging();
                    }
                }
            }
        }

        private static void Harass()
        {
            var t = GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (!t.IsValidTarget())
                return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

            var useQDontUnderTurret = Config.Item("UseQHarassDontUnderTurret").GetValue<bool>();
            if (Q.IsReady() && useQ)
            {
                if (useQDontUnderTurret)
                {
                    if (!t.UnderTurret())
                        Q.Cast(t);
                }
                else
                    Q.Cast(t);
            }

            if (E.IsReady() && useE)
            {
                E.Cast();
            }
        }

        private static void JungleFarm()
        {
            if (!Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                return;

            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, E2.Range, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            var mob = mobs[0];
            if (useE && E.IsReady())
            {
                E.Cast();
            }

            if (useQ && Q.IsReady())
            {
                if (!Q.IsCharging)
                    Q.StartCharging();
                else
                    Q.Cast(mob);
            }
        }

        private static void Flee()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (Q.IsCharging && Q.Range >= Q.ChargedMaxRange)
            {
                Q.Cast(Game.CursorPos);
            }
            else
            {
                Q.StartCharging();
            }

        }

        private static void LaneClear()
        {
            if (!Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                return;

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.ChargedMaxRange);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E2.Range);

            if (useQ && Q.IsReady())
            {
                if (Q.IsCharging)
                {
                    var locQ = Q.GetLineFarmLocation(allMinionsQ);
                    if (allMinionsQ.Count == allMinionsQ.Count(m => vPlayer.Distance(m) < Q.Range) &&
                        locQ.MinionsHit > 2 && locQ.Position.IsValid())
                        Q.Cast(locQ.Position);
                }
                else if (allMinionsQ.Count > 2)
                    Q.StartCharging();
            }

            if (useE && E.IsReady())
            {
                var locE = E.GetLineFarmLocation(allMinionsE);
                if (allMinionsQ.Count == allMinionsQ.Count(m => vPlayer.Distance(m) < E2.Range) && locE.MinionsHit > 2 &&
                    locE.Position.IsValid())
                    E.Cast();
            }
        }

        public static bool Intersection(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        {
            var p3 = new Vector2(pC.X + radius, pC.Y + radius);
            var m = ((p2.Y - p1.Y)/(p2.X - p1.X));
            var constant = (m*p1.X) - p1.Y;
            var b = -(2f*((m*constant) + p3.X + (m*p3.Y)));
            var a = (1 + (m*m));
            var c = ((p3.X*p3.X) + (p3.Y*p3.Y) - (radius*radius) + (2f*constant*p3.Y) + (constant*constant));
            var d = ((b*b) - (4f*a*c));

            return d > 0;
        }

        public static Obj_AI_Base EMinion
        {
            get
            {
                var vTarget = GetTarget(E2.Range, TargetSelector.DamageType.Physical);
                var vMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.None);

                return (from vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(E.Range))
                    let endPoint =
                        vMinion.ServerPosition.To2D()
                            .Extend(ObjectManager.Player.ServerPosition.To2D(), -E.Range)
                            .To3D()
                    where
                        Intersection(
                            ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(), vTarget.ServerPosition.To2D(),
                            vTarget.BoundingRadius + E.Width/2)
                    select vMinion).FirstOrDefault();
            }
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.Q);

            fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.W);

            if (E.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.E)*E.Instance.Ammo;

            if (R.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.R);

            if (Items.CanUseItem(3128))
                fComboDamage += vPlayer.GetItemDamage(vTarget, Damage.DamageItems.Botrk);

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += vPlayer.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            return (float) fComboDamage;
        }
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells)
                return;

            if (vPlayer.Distance(unit) < Q.Range)
            {
                Q.Cast(unit);
            }
            else if (vPlayer.Distance(unit) < R.Range)
            {
                R.Cast(unit);
            }
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells)
                return;

            if (vPlayer.Distance(vTarget) < Q.Range)
            {
                Q.Cast(vTarget);
            }
            else if (vPlayer.Distance(vTarget) < R.Range)
            {
                R.Cast(vTarget);
            }
        }

        private static InventorySlot GetInventorySlot(int id)
        {
            return
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId) id && item.Stacks >= 1) || (item.Id == (ItemId) id && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget == null)
                return;

            foreach (var itemID in from menuItem in _menuTargetedItems.Items
                let useItem = _menuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                into itemId
                where Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                select itemId)
            {
                Items.UseItem(itemID, vTarget);
            }

            foreach (var itemID in from menuItem in _menuNonTargetedItems.Items
                let useItem = _menuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                where useItem
                select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                into itemId
                where Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                select itemId)
            {
                if (ObjectManager.Player.Distance(vTarget) <= 400)
                    Items.UseItem(itemID);
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
    }
}
