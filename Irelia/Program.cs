#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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

        public static Utils Utils;
        public static Enemies Enemies;


        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (vPlayer.ChampionName != "Irelia")
                return;

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
            Config = new Menu("xQx | Irelia", "Irelia", true);

            var TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(TargetSelectorMenu);
            Config.AddSubMenu(TargetSelectorMenu);


            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            Utils = new Utils();
            Enemies = new Enemies();
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
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Menu harassUseQ = new Menu("Use Q", "harassUseQ");
            Config.SubMenu("Harass").AddSubMenu(harassUseQ);
            harassUseQ.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            harassUseQ.AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassMode", "Harass Mode: ").SetValue(new StringList(new[] { "Q", "E", "Q+E" })));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass").SetValue(
                        new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Menu laneClearUseQ = new Menu("Use Q", "laneClearUseQ");
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("QFarmDelay", "Q Farm Delay (Default: 300)").SetValue(new Slider(200, 500, 0)));
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

            Game.PrintChat("xxxxxxxxxx");

            // Extras -> Use Items 
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "InterruptSpells").SetValue(true));
            MenuExtras.AddItem(new MenuItem("StopUlties", "Interrupt Ulti With E").SetValue(true));
            MenuExtras.AddItem(new MenuItem("ForceInterruptUlties", "Force Interrupt Ulti With Q+E").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("QRange", "Q range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("RRange", "R range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("drawQMinionKill", "Q Minion Kill").SetValue(
                        new Circle(true, System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("drawMinionLastHit", "Minion Last Hit").SetValue(
                        new Circle(true, System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("drawMinionNearKill", "Minion Near Kill").SetValue(
                        new Circle(true, System.Drawing.Color.Gray)));

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

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            QUsedTime = Game.Time;

            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx:</font> <font color='#FFFFFF'>{0} Loaded!</font>", ChampionName));
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList.Where(xSlot => xSlot != W))
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color);
            }

            var drawMinionLastHit = Config.Item("drawMinionLastHit").GetValue<Circle>();
            var drawMinionNearKill = Config.Item("drawMinionNearKill").GetValue<Circle>();
            if (drawMinionLastHit.Active || drawMinionNearKill.Active)
            {
                var xMinions = MinionManager.GetMinions(
                    ObjectManager.Player.Position,
                    ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius + 300, MinionTypes.All,
                    MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                foreach (var xMinion in xMinions)
                {
                    if (drawMinionLastHit.Active &&
                        ObjectManager.Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health)
                    {
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color);
                    }
                    else if (drawMinionNearKill.Active &&
                             ObjectManager.Player.GetAutoAttackDamage(xMinion, true) * 2 >= xMinion.Health)
                    {
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionNearKill.Color);
                    }
                }
            }
            var drawQMinionKill = Config.Item("drawQMinionKill").GetValue<Circle>();
            if (drawQMinionKill.Active)
            {
                var xMinions = MinionManager.GetMinions(
                    ObjectManager.Player.Position, Q.Range + 300, MinionTypes.All, MinionTeam.Enemy,
                    MinionOrderTypes.MaxHealth);

                foreach (var xMinion in
                    xMinions.Where(
                        xMinion => ObjectManager.Player.GetSpellDamage(xMinion, SpellSlot.Q) - 20 >= xMinion.Health))
                {
                    Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawQMinionKill.Color);
                }
            }
        }

        

        private static int BladesSpellCount
        {
            get
            {
                return
                    ObjectManager.Player.Buffs.Where(buff => buff.Name.ToLower() == "ireliatranscendentbladesspell")
                        .Select(buff => buff.Count)
                        .FirstOrDefault();
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(50))
                return;
            /*
            foreach (
                var xEnemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            xEnemy =>
                                Q.IsInRange(xEnemy) && xEnemy.IsEnemy && xEnemy.HasBuff("teleport_target", true) &&
                                xEnemy.Health < ObjectManager.Player.Health)) 
            {
                if (E.IsReady() && E.IsInRange(xEnemy))
                    E.CastOnUnit(xEnemy, true);
                else if (Q.IsReady() && E.IsReady())
                {
                    Q.CastOnUnit(xEnemy, true);
                    E.CastOnUnit(xEnemy, true);
                }
            }
            */

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();

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
                if (Utility.UnderTurret(t))
                    return;

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    if (t.Health < GetComboDamage(t) || t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q))
                        Q.CastOnUnit(t);

                    if (ObjectManager.Player.Distance(t) > E.Range)
                        Q.CastOnUnit(t);
                }

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    Utility.DelayAction.Add(qFarmDelay, () => Q.CastOnUnit(t));
                }
            }
            else
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    if (t.Health < GetComboDamage(t) || t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q))
                        Q.CastOnUnit(t);

                    if (ObjectManager.Player.Distance(t) > E.Range)
                        Q.CastOnUnit(t);
                }

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    Utility.DelayAction.Add(qFarmDelay, () => Q.CastOnUnit(t));
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

            var t = Enemies.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t == null)
            {
                return;
            }

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
                ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) >= t.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
            }

            UseItems(t);
        }


        private static void Harass()
        {
            var vTarget = Enemies.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            var mana = ObjectManager.Player.MaxMana * (Config.Item("HarassMana").GetValue<Slider>().Value / 100.0);

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

                var mobs = MinionManager.GetMinions(
                    vPlayer.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

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

                var vMinions = MinionManager.GetMinions(
                    vPlayer.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    var vMinionQDamage = vPlayer.GetSpellDamage(vMinion, SpellSlot.Q);
                    var vMinionEDamage = vPlayer.GetSpellDamage(vMinion, SpellSlot.E);

                    var qFarmDelay = (Config.Item("QFarmDelay").GetValue<Slider>().Value);

                    if (useQ && vMinion.Health <= vMinionQDamage - 20)
                    {
                        CastSpellQ(vMinion);
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
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.R) * 4;

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3153))
                fComboDamage += ObjectManager.Player.GetItemDamage(vTarget, Damage.DamageItems.Botrk);

            return (float) fComboDamage;
        }

        public static bool IsPositionSafe(Obj_AI_Base vTarget, Spell vSpell)
        {
            var predPos = vSpell.GetPrediction(vTarget).CastPosition.To2D();
            var myPos = ObjectManager.Player.Position.To2D();
            var newPos = (vTarget.Position.To2D() - myPos).Normalized();

            var checkPos = predPos + newPos * (vSpell.Range - Vector2.Distance(predPos, myPos));
            Obj_Turret closestTower = null;

            foreach (
                Obj_Turret tower in
                    ObjectManager.Get<Obj_Turret>()
                        .Where(tower => tower.IsValid && !tower.IsDead && !tower.IsDead && tower.IsEnemy)
                        .Where(tower => Vector3.Distance(tower.Position, ObjectManager.Player.Position) < 1450))
            {
                closestTower = tower;
            }

            if (closestTower == null)
                return true;

            return !(Vector2.Distance(closestTower.Position.To2D(), checkPos) <= 910);
        }

        private static InventorySlot GetInventorySlot(int ID)
        {
            return
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId) ID && item.Stacks >= 1) || (item.Id == (ItemId) ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero t)
        {
            if (t == null)
                return;

            int[] targeted = new[] { 3153, 3144, 3146, 3184 };
            foreach (
                var itemId in
                    targeted.Where(
                        itemId =>
                            Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null &&
                            t.IsValidTarget(450)))
            {
                Items.UseItem(itemId, t);
            }

            int[] nonTarget = new[] { 3180, 3143, 3131, 3074, 3077, 3142 };
            foreach (
                var itemId in
                    nonTarget.Where(
                        itemId =>
                            Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null &&
                            t.IsValidTarget(450)))
            {
                Items.UseItem(itemId);
            }
        }
    }
}
