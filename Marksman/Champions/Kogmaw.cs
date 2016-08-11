#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Marksman.Champions
{
    internal class Kogmaw : Champion
    {
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static bool bioArcaneActive = false;
        public int UltiBuffStacks;

        public Kogmaw()
        {
            Utils.Utils.PrintMessage("KogMaw loaded.");

            Q = new Spell(SpellSlot.Q, 1175f);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, 1280f);
            R = new Spell(SpellSlot.R, float.MaxValue);

            Q.SetSkillshot(0.25f, 70f, 1650f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.50f, 120f, 1350, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.2f, 120f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = {Q, W, E, R};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position,
                        spell.Slot == SpellSlot.W
                            ? Orbwalking.GetRealAutoAttackRange(null) + 65 + W.Range
                            : spell.Range,
                        menuItem.Color);
            }
        }

        public override void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender.IsMe && args.Buff.Name.ToLower() == "kogmawbioarcanebarrage")
            {
                bioArcaneActive = true;
            }
        }

        public override void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (sender.IsMe && args.Buff.Name.ToLower() == "kogmawbioarcanebarrage")
            {
                bioArcaneActive = false;
            }

        }

        private static float GetRealAARange
        {
            get
            {
                return Orbwalking.GetRealAutoAttackRange(null) + 65 + (bioArcaneActive ? W.Range : 0);
            }
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!W.IsReady())
            {
                return;
            }

            if (GetValue<bool>("Misc.UseW.Inhibitor") && args.Target is Obj_BarracksDampener)
            {
                W.Cast();
            }

            if (GetValue<bool>("Misc.UseW.Nexus") && args.Target is Obj_HQ)
            {
                W.Cast();
            }

            if (GetValue<bool>("Misc.UseW.Turret") && args.Target is Obj_AI_Turret)
            {
                W.Cast();
            }
        }


        private static void CastQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() && Q.IsReady() &&
                ObjectManager.Player.Distance(t.ServerPosition) <= Q.Range)
            {
                var qPredict = Q.GetPrediction(t);
                var hithere = qPredict.CastPosition.Extend(ObjectManager.Player.Position, -140);
                if (qPredict.Hitchance >= HitChance.High)
                    Q.Cast(hithere);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            UltiBuffStacks = GetUltimateBuffStacks();

            W.Range = 110 + 20*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
            R.Range = 900 + 300*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;

            if (R.IsReady() && GetValue<bool>("UseRM"))
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero => hero.IsValidTarget(R.Range) && R.GetDamage(hero) > hero.Health))
                    R.Cast(hero, false, true);

            if ((!ComboActive && !HarassActive) ||
                (!Orbwalking.CanMove(100) &&
                 !(ObjectManager.Player.BaseAbilityDamage + ObjectManager.Player.FlatMagicDamageMod > 100))) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));
            var rLim = GetValue<Slider>("Rlim" + (ComboActive ? "C" : "H")).Value;

            if (useQ && Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    CastQ();
                //if (Q.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                //    return;
            }

            if (GetValue<bool>("UseRSC") && R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget() &&
                    (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Slow) ||
                     t.HasBuffOfType(BuffType.Fear) ||
                     t.HasBuffOfType(BuffType.Taunt)))
                {
                    R.Cast(t, false, true);
                }
            }

            if (useR && R.IsReady() && UltiBuffStacks < rLim)
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    R.Cast(t, false, true);
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (target != null && (!ComboActive && !HarassActive) || !unit.IsMe || !(target is Obj_AI_Hero))
            {
                return;
            }

            var t = target as Obj_AI_Hero;
            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
            var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));

            if (useW && W.IsReady())
                W.Cast();


            if (useE && E.IsReady())
                if (E.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                    return;

        }

        private static int GetUltimateBuffStacks()
        {
            return (from buff in ObjectManager.Player.Buffs
                where buff.DisplayName.ToLower() == "kogmawlivingartillery"
                select buff.Count).FirstOrDefault();
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("UseRSC" + Id, "Use R for Stunned Enemy").SetValue(true));
            config.AddItem(new MenuItem("RlimC" + Id, "R Limiter").SetValue(new Slider(3, 5, 1)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(false));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(false));
            config.AddItem(new MenuItem("UseRH" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RlimH" + Id, "R Limiter").SetValue(new Slider(1, 5, 1)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawR" + Id, "R range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseRM" + Id, "Use R To Killsteal").SetValue(true));
            config.AddItem(new MenuItem("Misc.UseW.Turret" + Id, "Use W for Turret").SetValue(false));
            config.AddItem(new MenuItem("Misc.UseW.Inhibitor" + Id, "Use W for Inhibitor").SetValue(true));
            config.AddItem(new MenuItem("Misc.UseW.Nexus" + Id, "Use W for Nexus").SetValue(true));

            return true;
        }

        public override void ExecuteLaneClear()
        {
            List<Obj_AI_Base> laneMinions;

            var laneWValue = GetValue<StringList>("Lane.UseW").SelectedIndex;

            if (laneWValue != 0 && W.IsReady())
            {
                var totalAa =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            m =>
                                m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65 + W.Range))
                        .Sum(mob => (int) mob.Health);

                totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
                if (totalAa > laneWValue*5)
                {
                    W.Cast();
                }
            }

            var laneQValue = GetValue<StringList>("Lane.UseQ").SelectedIndex;

            if (laneQValue != 0 && W.IsReady())
            {
                if (laneQValue == 1 || laneQValue == 3)
                {
                    var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                    foreach (var minions in vMinions
                        .Where(minions => minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q))
                        .Where(
                            m =>
                                m.IsValidTarget(Q.Range) &&
                                m.Distance(ObjectManager.Player.Position) > GetRealAARange)
                        )
                    {
                        var qP = Q.GetPrediction(minions);
                        var hit = qP.CastPosition.Extend(ObjectManager.Player.Position, -140);
                        if (qP.Hitchance >= HitChance.High)
                        {
                            Q.Cast(hit);
                        }
                    }
                }
                if (laneQValue == 2 || laneQValue == 3)
                {
                    var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range*1);

                    foreach (var n in from n in minions
                        let xH =
                            HealthPrediction.GetHealthPrediction(n,
                                (int) (ObjectManager.Player.AttackCastDelay*1000), Game.Ping/2 + 100)
                        where xH < 0
                        where n.Health < Q.GetDamage(n)
                        select n)
                    {
                        Q.Cast(n);
                    }
                }
            }

            var laneEValue = GetValue<StringList>("Lane.UseE").SelectedIndex;
            if (laneEValue != 0 && E.IsReady())
            {
                laneMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,
                    MinionTypes.All);

                if (laneMinions != null)
                {
                    var locE = E.GetLineFarmLocation(laneMinions);
                    if (laneMinions.Count == laneMinions.Count(m => ObjectManager.Player.Distance(m) < E.Range) &&
                        locE.MinionsHit > laneEValue && locE.Position.IsValid())
                    {
                        E.Cast(locE.Position);
                    }
                }
            }

            var laneRValue = GetValue<StringList>("Lane.UseR").SelectedIndex;
            if (laneRValue != 0 && R.IsReady() && UltiBuffStacks < GetValue<Slider>("Lane.UseRLim").Value)
            {
                switch (laneRValue)
                {
                    case 1:
                    {
                            var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                            foreach (var minions in vMinions
                                .Where(minions => minions.Health < R.GetDamage(minions))
                                .Where(
                                    m =>
                                        m.IsValidTarget(R.Range) &&
                                        m.Distance(ObjectManager.Player.Position) > GetRealAARange)
                                )
                            {
                                R.Cast(minions);
                            }

                            break;
                    }

                    case 2:
                        {
                            laneMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range + R.Width + 30,
                                MinionTypes.Ranged);

                            if (laneMinions != null)
                            {
                                var locR = R.GetCircularFarmLocation(laneMinions, R.Width * 0.75f);
                                if (locR.MinionsHit >= laneEValue && R.IsInRange(locR.Position.To3D()))
                                {
                                    R.Cast(locR.Position);
                                }
                            }

                            break;
                        }
                }

            }
        }

        public override void ExecuteJungleClear()
        {
            Obj_AI_Base jungleMobs;

            var jungleWValue = GetValue<StringList>("Jungle.UseW").SelectedIndex;
            if (jungleWValue != 0 && W.IsReady())
            {
                var jungleW = jungleWValue;
                if (jungleW != 0)
                {
                    if (jungleW == 1)
                    {
                        jungleMobs =
                            Utils.Utils.GetMobs(Orbwalking.GetRealAutoAttackRange(null) + 65 + W.Range,
                                Utils.Utils.MobTypes.BigBoys);
                        if (jungleMobs != null)
                        {
                            W.Cast();
                        }
                    }
                    else
                    {
                        var totalAa =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    m =>
                                        m.Team == GameObjectTeam.Neutral &&
                                        m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65 + W.Range))
                                .Sum(mob => (int) mob.Health);

                        totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
                        if (totalAa > jungleW*5)
                        {
                            W.Cast();
                        }

                    }
                }
            }


            var jungleQValue = GetValue<StringList>("Jungle.UseQ").SelectedIndex;
            if (jungleQValue != 0 && Q.IsReady())
            {
                jungleMobs = Marksman.Utils.Utils.GetMobs(Q.Range, Utils.Utils.MobTypes.All);

                if (jungleMobs != null)
                {
                    switch (jungleQValue)
                    {
                        case 1:
                        {
                            Q.Cast(jungleMobs);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.Utils.GetMobs(Q.Range, Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                Q.Cast(jungleMobs);
                            }
                            break;
                        }

                    }
                }
            }

            var jungleEValue = GetValue<StringList>("Jungle.UseE").SelectedIndex;
            if (jungleEValue != 0 && E.IsReady())
            {
                jungleMobs = Marksman.Utils.Utils.GetMobs(E.Range, Utils.Utils.MobTypes.All);

                if (jungleMobs != null)
                {
                    switch (jungleEValue)
                    {
                        case 1:
                        {
                            E.Cast(jungleMobs, false, true);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.Utils.GetMobs(E.Range, Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                E.Cast(jungleMobs, false, true);
                            }
                            break;
                        }

                    }
                }
            }

            var jungleRValue = GetValue<StringList>("Jungle.UseR").SelectedIndex;
            if (jungleRValue != 0 && R.IsReady() && UltiBuffStacks < GetValue<Slider>("Jungle.UseRLim").Value)
            {
                jungleMobs = Marksman.Utils.Utils.GetMobs(R.Range, Utils.Utils.MobTypes.All);

                if (jungleMobs != null)
                {
                    switch (jungleRValue)
                    {
                        case 1:
                        {
                            R.Cast(jungleMobs, false, true);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.Utils.GetMobs(R.Range, Utils.Utils.MobTypes.BigBoys, jungleRValue);
                            if (jungleMobs != null)
                            {
                                R.Cast(jungleMobs, false, true);
                            }
                            break;
                        }

                    }
                }
            }
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("Lane.UseQ" + Id, "Q:").SetValue(new StringList(new[] {"Off", "Just For Out of AA Range", "Just Non Killable Minions", "Both"}, 3)));

            string[] strW = new string[5];
            {
                strW[0] = "Off";
                for (var i = 1; i < 5; i++)
                {
                    var x = (i)*5;
                    strW[i] = "If need to AA more than >= " + x;
                }
                config.AddItem(new MenuItem("Lane.UseW" + Id, "W:").SetValue(new StringList(strW, 1)));
            }

            string[] strE = new string[7];
            strE[0] = "Off";

            for (var i = 1; i < 7; i++)
            {
                strE[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("Lane.UseE" + Id, "E:").SetValue(new StringList(strE, 3)))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);

            string[] strR = new string[5];
            {
                strR[0] = "Off";
                strR[1] = "Just Out of AA Range";
                for (var i = 2; i < 5; i++)
                {
                    strR[i] = "Minion Count >= " + i;
                }

                config.AddItem(new MenuItem("Lane.UseR" + Id, "R:").SetValue(new StringList(strR, 3)))
                    .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
                config.AddItem(
                    new MenuItem("Lane.UseRLim" + Id, Marksman.Utils.Utils.Tab + "R Limit:").SetValue(new Slider(3, 5, 1)))
                    .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
            }
            return true;
        }


        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("Jungle.UseQ" + Id, "Q:").SetValue(new StringList(
                    new[] {"Off", "On", "Just big Monsters"}, 2)));

            string[] strW = new string[5];
            {
                strW[0] = "Off";
                strW[1] = "Just big Monsters";

                for (var i = 2; i < 5; i++)
                {
                    var x = (i - 1)*5;
                    strW[i] = "If need to AA more than >= " + x;
                }
                config.AddItem(new MenuItem("Jungle.UseW" + Id, "W:").SetValue(new StringList(strW, 1)));
            }

            config.AddItem(
                new MenuItem("Jungle.UseE" + Id, "E:").SetValue(new StringList(
                    new[] {"Off", "On", "Just big Monsters"}, 2)));

            string[] strR = new string[4];
            strR[0] = "Off";
            strR[1] = "Just big Monsters";
            for (var i = 2; i < 4; i++)
            {
                strR[i] = "Mob Count >= " + i;
            }

            config.AddItem(new MenuItem("Jungle.UseR" + Id, "R:").SetValue(new StringList(strR, 3)))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
            config.AddItem(
                new MenuItem("Jungle.UseRLim" + Id, Marksman.Utils.Utils.Tab + "R Limit:").SetValue(new Slider(3, 5, 1)))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);

            return true;
        }
    }
}