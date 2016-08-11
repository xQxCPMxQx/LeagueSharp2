#region
using System;
using System.Drawing;
using System.Linq;
using System.Resources;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace Marksman.Champions
{
    internal class MissFortune : Champion
    {
        public static Spell Q, W, E;
        private static float UltiCastedTime = 0;
        public static Obj_AI_Hero Player = ObjectManager.Player;

        public MissFortune()
        {
            Q = new Spell(SpellSlot.Q, 650);
            Q.SetTargetted(0.29f, 1400f);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 1000);
            E.SetSkillshot(0.5f, 330f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Utils.Utils.PrintMessage("MissFortune loaded.");
        }

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "MissFortuneBulletTime")
                UltiCastedTime = Game.Time;
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit vTarget)
        {
            var t = vTarget as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (useQ)
                    Q.CastOnUnit(t);

                if (useW && W.IsReady())
                    W.CastOnUnit(ObjectManager.Player);
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = {Q, E};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active && spell.Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        private static void CastQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range + 450, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(t);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            var ultCasting = Game.Time - UltiCastedTime < 0.2 || ObjectManager.Player.IsChannelingImportantSpell();
            Orbwalking.Attack = !ultCasting;
            Orbwalking.Move = !ultCasting;

            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                    return;
                CastQ();
            }

            if (E.IsReady() && GetValue<KeyBind>("UseETH").Active)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) ||
                                          t.HasBuffOfType(BuffType.Charm) || t.HasBuffOfType(BuffType.Fear) ||
                                          t.HasBuffOfType(BuffType.Taunt) || t.HasBuff("zhonyasringshield") ||
                                          t.HasBuff("Recall")))
                {
                    E.CastIfHitchanceEquals(t, HitChance.Low);
                }
            }

            if (ComboActive || HarassActive)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                {
                    CastQ();
                }

                if (E.IsReady() && useE)
                {
                    var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                    {
                        if (ObjectManager.Player.Distance(t) > 600)
                            E.CastIfHitchanceEquals(t, t.Path.Count() > 1 ? HitChance.High : HitChance.Medium);
                        else
                            E.CastIfHitchanceEquals(t, HitChance.Low);
                    }                }
            }

            if (LaneClearActive)
            {
                var useQ = GetValue<bool>("UseQL");

                if (Q.IsReady() && useQ)
                {
                    var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                    foreach (
                        var minions in
                            vMinions.Where(
                                minions =>
                                    minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q) - 20))
                        Q.Cast(minions);
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            var jungleMobs = Marksman.Utils.Utils.GetMobs(Q.Range, Marksman.Utils.Utils.MobTypes.All);

            if (jungleMobs != null)
            {
                if (Q.IsReady())
                {
                    switch (Program.Config.Item("Jungle.UseQ").GetValue<StringList>().SelectedIndex)
                    {
                        case 1:
                        {
                            Q.CastOnUnit(jungleMobs);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.Utils.GetMobs(Q.Range, Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                Q.CastOnUnit(jungleMobs);
                            }
                            break;
                        }
                    }
                }

                if (W.IsReady())
                {
                    var jW = Program.Config.Item("Jungle.UseW").GetValue<StringList>().SelectedIndex;
                    if (jW != 0)
                    {
                        if (jW == 1)
                        {
                            jungleMobs = Utils.Utils.GetMobs(Orbwalking.GetRealAutoAttackRange(null) + 65,
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
                                            m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 165))
                                    .Sum(mob => (int) mob.Health);

                            totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
                            if (totalAa >= jW)
                            {
                                W.Cast();
                            }

                        }
                    }
                }

                if (E.IsReady())
                {
                    var jE = Program.Config.Item("Jungle.UseE").GetValue<StringList>().SelectedIndex;
                    if (jE != 0)
                    {
                        var aMobs = MinionManager.GetMinions(ObjectManager.Player.Position, E.Range, MinionTypes.All,
                            MinionTeam.Neutral);
                        if (aMobs.Count > jE)
                        {
                            E.Cast(aMobs[0]);
                        }
                    }
                }
            }
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!W.IsReady())
            {
                return;
            }

            if (GetValue<bool>("Misc.UseW.Turret") && args.Target is Obj_AI_Turret)
            {
                if (((Obj_AI_Turret) args.Target).Health >= Player.TotalAttackDamage*3)
                {
                    W.Cast();
                }
            }

            if (GetValue<bool>("Misc.UseW.Inhibitor") && args.Target is Obj_BarracksDampener)
            {
                if (((Obj_BarracksDampener) args.Target).Health >= Player.TotalAttackDamage*3)
                {
                    W.Cast();
                }
            }

            if (GetValue<bool>("Misc.UseW.Nexus") && args.Target is Obj_HQ)
            {
                W.Cast();
            }
        }

        public override void ExecuteLaneClear()
        {
            if (Q.IsReady())
            {
                var lQ = Program.Config.Item("Lane.UseQ").GetValue<StringList>().SelectedIndex;
                if (lQ != 0)
                {
                    {
                        var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                        foreach (var minions in
                            vMinions.Where(
                                minions => minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q)))
                        {
                            Q.CastOnUnit(minions);
                        }
                    }
                }
            }

            if (W.IsReady())
            {
                var lW = Program.Config.Item("Lane.UseW").GetValue<StringList>().SelectedIndex;
                if (lW != 0)
                {
                    var totalAa =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                m =>
                                    m.IsEnemy && !m.IsDead &&
                                    m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null)))
                            .Sum(mob => (int) mob.Health);

                    totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
                    if (totalAa > lW)
                    {
                        W.Cast();
                    }
                }
            }

            if (E.IsReady())
            {
                var lE = Program.Config.Item("Lane.UseE").GetValue<StringList>().SelectedIndex;
                if (lE != 0)
                {
                    var mE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All);
                    if (mE != null)
                    {
                        var locE = E.GetCircularFarmLocation(mE, 175);
                        if (locE.MinionsHit >= lE && E.IsInRange(locE.Position.To3D()))
                        {
                            foreach (
                                var x in
                                    ObjectManager.Get<Obj_AI_Minion>()
                                        .Where(m => m.IsEnemy && !m.IsDead && m.Distance(locE.Position) < 500))
                            {
                                E.Cast(x);
                            }
                        }
                    }
                }
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));

            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(true));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle))).Permashow(true, "Marksman | Toggle Q", SharpDX.Color.Aqua);
            config.AddItem(
                new MenuItem("UseETH" + Id, "Use E (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0],
                    KeyBindType.Toggle))).Permashow(true, "Marksman | Toggle E", SharpDX.Color.Aqua);

            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.FromArgb(50, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, Color.FromArgb(50, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));

            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("Lane.UseQ", Utils.Utils.Tab + "Use Q:").SetValue(new StringList(new[] {"Off", "On"})));

            string[] strW = new string[7];
            {
                strW[0] = "Off";

                for (var i = 1; i < 7; i++)
                {
                    strW[i] = "If need to AA more than >= " + i;
                }
                config.AddItem(new MenuItem("Lane.UseW", Utils.Utils.Tab + "Use W:").SetValue(new StringList(strW, 0)));
            }

            string[] strE = new string[5];
            {
                strE[0] = "Off";

                for (var i = 1; i < 5; i++)
                {
                    strE[i] = "Minion Count >= " + i;
                }
                config.AddItem(new MenuItem("Lane.UseE", Utils.Utils.Tab + "Use E:").SetValue(new StringList(strE, 0)));
            }
            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("Jungle.UseQ", "Use Q").SetValue(new StringList(new[] {"Off", "On", "Just big Monsters"}, 2)));

            string[] strW = new string[8];
            strW[0] = "Off";
            strW[1] = "Just for big Monsters";

            for (var i = 2; i < 8; i++)
            {
                strW[i] = "If need to AA more than >= " + i;
            }

            config.AddItem(new MenuItem("Jungle.UseW", "Use W").SetValue(new StringList(strW, 4)));

            string[] strE = new string[4];
            strE[0] = "Off";

            for (var i = 1; i < 4; i++)
            {
                strE[i] = "Mob Count >= " + i;
            }

            config.AddItem(new MenuItem("Jungle.UseE", "Use E:").SetValue(new StringList(strE, 3)));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("Misc.UseW.Turret" + Id, "Use W for Turret").SetValue(true));
            config.AddItem(new MenuItem("Misc.UseW.Inhibitor" + Id, "Use W for Inhibitor").SetValue(true));
            config.AddItem(new MenuItem("Misc.UseW.Nexus" + Id, "Use W for Nexus").SetValue(true));
            return true;
        }
    }
}
