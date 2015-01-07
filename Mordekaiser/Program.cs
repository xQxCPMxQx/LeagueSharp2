using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Mordekaiser
{
    class Program
    {
        public const string ChampionName = "Mordekaiser";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;

        public static List<Items.Item> ItemList = new List<Items.Item>();
        public static Items.Item Dfg, Hex;

        public static Menu Config;
        public static Menu MenuExtras;
        public static float SlaveDelay = 0;

        public static float SlaveTimer;

        public static SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        private const float WDamageRange = 270f;
        private const float SlaveActivationRange = 2200f;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName) return;
            if (Player.IsDead) return;

            SlaveTimer = Game.Time;
            /* [ Set Items ]*/
            Dfg = new Items.Item(3128, 750);
            ItemList.Add(Dfg);
            
            Hex = new Items.Item(3146, 750);
            ItemList.Add(Hex);

            /* [ Set Spells ]*/
            Q = new Spell(SpellSlot.Q, 300);
            SpellList.Add(Q);

            W = new Spell(SpellSlot.W, 780);
            W.SetTargetted(0.5f, 1500f);
            SpellList.Add(W);

            E = new Spell(SpellSlot.E, 670);
            E.SetSkillshot(0.25f, 15f*2*(float) Math.PI/180, 2000f, false, SkillshotType.SkillshotCone);
            SpellList.Add(E);

            R = new Spell(SpellSlot.R, 850);
            R.SetTargetted(0.5f, 1500f);
            SpellList.Add(R);

            /* [ Set Menu ] */
            Config = new Menu(string.Format("xQx | {0}", ChampionName), ChampionName, true);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            {
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));

                Config.SubMenu("Combo").AddSubMenu(new Menu("Don't Use Ult On", "DontUlt"));
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                    {
                        Config.SubMenu("Combo")
                            .SubMenu("DontUlt")
                            .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
                    }
                }
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboActive", "Combo!").SetValue(
                            new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            }
            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseW", "Use W").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseE", "Use E").SetValue(true));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActiveT", "Harass (Toggle)!").SetValue(new KeyBind("H".ToCharArray()[0],
                            KeyBindType.Toggle)));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0],
                            KeyBindType.Press)));
            }
            /* [ Farming ] */
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            {
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(true));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(true));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(true));
                Config.SubMenu("LaneClear")
                    .AddItem(new MenuItem("LaneClearActive", "Lane Clear!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            }
            /* [ JungleFarm ] */
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseE", "Use E").SetValue(true));
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("JungleFarmActive", "Jungle Farm!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            }
            /* [ Extras ] */
            MenuExtras = new Menu("Extras", "Extras");
            {
                MenuExtras.AddItem(new MenuItem("ShieldSelf", "Sheild Self").SetValue(true));
                MenuExtras.AddItem(new MenuItem("ShieldAlly", "Sheild Ally").SetValue(false));
                Config.AddSubMenu(MenuExtras);
            }

            /* [ Drawing ] */
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            {
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawW", "W Available Range").SetValue(new Circle(true, Color.Pink)));
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("DrawWAffectedRange", "W Affected Range").SetValue(new Circle(true, Color.Pink)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawE", "E Range").SetValue(new Circle(true, Color.Pink)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawR", "R Range").SetValue(new Circle(true, Color.Pink)));

                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawAloneEnemy", "Q Alone Target").SetValue(new Circle(true, Color.Pink)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawSlavePos", "Ult Slave Pos.").SetValue(new Circle(true, Color.Pink)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawSlaveRange", "Ult Slave Range").SetValue(new Circle(true, Color.Pink)));

                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEmpty", ""));
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawDisable", "Disable All").SetValue(false));
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEmpty", ""));

                /* [ Damage After Combo ] */
                var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
                Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);

                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
                dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
            }
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            WelcomeMessage();
        }

        private static bool MordekaiserHaveSlave
        {
            get { return Player.Spellbook.GetSpell(SpellSlot.R).Name == "mordekaisercotgguide"; }
        }
        private static void MordekaiserHaveSlave2()
        {
            if (Player.Spellbook.GetSpell(SpellSlot.R).Name == "mordekaisercotgguide")
            {
                if (SlaveTimer + 11000 < Game.Time)
                    SlaveTimer = Game.Time;
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("DrawDisable").GetValue<bool>())
                return;

            foreach (var spell in SpellList.Where(spell => spell != Q && spell != W))
            {
                var menuItem = Config.Item("Draw" + spell.Slot).GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            var drawSlaveRange = Config.Item("DrawSlaveRange").GetValue<Circle>();
            
            if (MordekaiserHaveSlave)
            {
                MordekaiserHaveSlave2();

                if (drawSlaveRange.Active)
                    Render.Circle.DrawCircle(Player.Position, SlaveActivationRange, drawSlaveRange.Color);

                if (!Config.Item("DrawSlavePos").GetValue<Circle>().Active) return;
                var drawSlavePos = Config.Item("DrawSlavePos").GetValue<Circle>();

                var xMinion =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion => Player.Distance(minion) < SlaveActivationRange && Player.IsAlly && !Player.IsDead);
                var xEnemy = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy);

                var xList = from xM in xMinion
                    join xE in xEnemy on new {pEquals1 = xM.BaseSkinName}
                        equals new { pEquals1 = xE.BaseSkinName }
                            select new { xM.Position, xM.Name, xM.NetworkId, xM.BaseSkinName };

                foreach (var xL in xList)
                {
                 //   Game.PrintChat(xL.BaseSkinName);
                    Render.Circle.DrawCircle(xL.Position, 70f, Color.White);
                    Render.Circle.DrawCircle(xL.Position, 75f, drawSlavePos.Color);
                    Render.Circle.DrawCircle(xL.Position, 80f, Color.White);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (!Orbwalking.CanMove(100)) return;
           
            Orbwalker.SetAttack(true);

            
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                Config.Item("HarassActiveT").GetValue<KeyBind>().Active) 
                Harass();

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                JungleFarm();
            }
        }

        private static void Combo()
        {
            var wTarget = TargetSelector.GetTarget(W.Range / 2, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var rTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            var rGhostArea = TargetSelector.GetTarget(1500f, TargetSelector.DamageType.Magical);

            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();
            var useR = Config.Item("ComboUseR").GetValue<bool>();

            if (useQ && Q.IsReady() &&
                Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) 
                Q.Cast();

            if (useW && wTarget != null && Player.Distance(wTarget) <= WDamageRange)
                W.CastOnUnit(Player);

            if (useE && eTarget != null)
                E.Cast(eTarget.Position);

            if (MordekaiserHaveSlave && rGhostArea != null && Environment.TickCount >= SlaveDelay)
            {
                R.Cast(rGhostArea);
                SlaveDelay = Environment.TickCount + 1000;
            }
            if (rTarget != null && !MordekaiserHaveSlave)
            {
                useR = (Config.Item("DontUlt" + rTarget.BaseSkinName) != null &&
                        Config.Item("DontUlt" + rTarget.BaseSkinName).GetValue<bool>() == false) && useR;

                if (useR && rTarget.Health < Player.GetSpellDamage(rTarget, SpellSlot.R))
                    R.CastOnUnit(rTarget);
            }
           
            foreach (var item in ItemList.Where(item => item.IsReady()))
                item.Cast(wTarget);
        }

        private static void Harass()
        {
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();

            if (useQ && Q.IsReady() &&
                Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) 
                Q.Cast();

            if (useW && wTarget != null && Player.Distance(wTarget) <= WDamageRange)
                W.CastOnUnit(Player);

            if (useE && eTarget != null)
                E.Cast(eTarget.Position);
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();
            var useE = Config.Item("LaneClearUseE").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition,
                    Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.NotAlly);
                foreach (var vMinion in from vMinion in minionsQ
                    let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    //where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                    select vMinion) 
                {
                    Q.Cast(vMinion);
                }
            }

            if (useW && W.IsReady())
            {
                var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range);
                var minionsW = W.GetCircularFarmLocation(rangedMinionsW, W.Range * 0.3f);
                if (minionsW.MinionsHit < 1 || !W.IsInRange(minionsW.Position.To3D()))
                    return;
                W.CastOnUnit(Player);
            }

            if (useE && E.IsReady())
            {
                var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);
                var minionsE = E.GetCircularFarmLocation(rangedMinionsE, E.Range);
                if (minionsE.MinionsHit < 1 || !E.IsInRange(minionsE.Position.To3D()))
                    return;
                E.Cast(minionsE.Position);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("JungleFarmUseQ").GetValue<bool>();
            var useW = Config.Item("JungleFarmUseW").GetValue<bool>();
            var useE = Config.Item("JungleFarmUseE").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range /2 , MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;
            var mob = mobs[0];

            if (useQ && Q.IsReady())
                Q.Cast();

            if (useW && W.IsReady())
                W.CastOnUnit(Player);

            if (useE && E.IsReady())
                E.Cast(mob.Position);
        }
        private static bool TargetAlone(Obj_AI_Hero vTarget)
        {
            var objects =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(x => x.IsMinion && x.IsEnemy && x.IsValid && vTarget.Distance(x) < 240);
            return !objects.Any();
        }

        private static double CalcQDamage
        {
            get
            {
                var qDamageVisitors = new float[] {80, 110, 140, 170, 200};
                var qDamageAlone = new float[] {132, 181, 230, 280, 330};

                var qTarget = TargetSelector.GetTarget(600, TargetSelector.DamageType.Magical);

                var fxQDamage = TargetAlone(qTarget)
                    ? qDamageAlone[Q.Level] + Player.BaseAttackDamage*1.65 + Player.BaseAbilityDamage*.66
                    : qDamageVisitors[Q.Level] + Player.BaseAttackDamage + Player.BaseAbilityDamage*.40;
                return fxQDamage;
            }
        }

        private static float GetComboDamage(Obj_AI_Hero vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady() && Player.Distance(vTarget) < Orbwalking.GetRealAutoAttackRange(Player))
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.Q);

            if (W.IsReady() && Player.Distance(vTarget) < WDamageRange)
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.W);

            if (E.IsReady() && Player.Distance(vTarget) < E.Range)
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.E);

            if (R.IsReady() && Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.R);

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                Player.Distance(vTarget) < R.Range) 
                fComboDamage += Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128) && Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetItemDamage(vTarget, Damage.DamageItems.Dfg);

            if (Items.CanUseItem(3092) && Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetItemDamage(vTarget, Damage.DamageItems.FrostQueenClaim);

            return (float)fComboDamage;
        }

        private static void WelcomeMessage()
        {
            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx</font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'>Loaded!</font>",
                    ChampionName));
        }
    }
}
