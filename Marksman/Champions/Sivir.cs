#region
using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Utils;
#endregion

namespace Marksman.Champions
{
    using System.Collections.Generic;
    using SharpDX;
    using Color = System.Drawing.Color;
    using Marksman.Utils;

    internal class DangerousSpells
    {
        public string ChampionName { get; private set; }
        public SpellSlot SpellSlot { get; private set; }

        public DangerousSpells(string championName, SpellSlot spellSlot)
        {
            ChampionName = championName;
            SpellSlot = spellSlot;
        }
    }

    internal class Sivir : Champion
    {
        public static Spell Q;
        public Spell E;
        public Spell W;
        public static List<DangerousSpells> DangerousList = new List<DangerousSpells>();

        public Sivir()
        {
            Q = new Spell(SpellSlot.Q, 1220);
            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 593);

            E = new Spell(SpellSlot.E);

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

            DangerousList.Add(new DangerousSpells("darius", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("fiddlesticks", SpellSlot.Q));
            DangerousList.Add(new DangerousSpells("garen", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("leesin", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("nautilius", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("skarner", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("syndra", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("warwick", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("zed", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("tristana", SpellSlot.R));

            Utils.PrintMessage("Sivir loaded.");
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

        public void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!E.IsReady())
            {
                return;
            }

            if (sender == null)
            {
                return;
            }

            if (sender.IsEnemy && sender is Obj_AI_Hero && args.Target.IsMe && E.IsReady())
            {
                foreach (
                    var c in
                        DangerousList.Where(c => ((Obj_AI_Hero) sender).ChampionName.ToLower() == c.ChampionName)
                            .Where(c => args.SData.Name == ((Obj_AI_Hero) sender).GetSpell(c.SpellSlot).Name))
                {
                    E.Cast();
                }
            }

            if (((Obj_AI_Hero) sender).ChampionName.ToLower() == "vayne" &&
                args.SData.Name == ((Obj_AI_Hero) sender).GetSpell(SpellSlot.E).Name)
            {
                for (var i = 1; i < 8; i++)
                {
                    var championBehind = ObjectManager.Player.Position +
                                         Vector3.Normalize(((Obj_AI_Hero) sender).ServerPosition -
                                                           ObjectManager.Player.Position)*(-i*50);
                    if (championBehind.IsWall())
                    {

                        E.Cast();
                    }
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (GetValue<bool>("AutoQ"))
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (Q.IsReady() && t.IsValidTarget())
                {
                    if ((t.HasBuffOfType(BuffType.Slow) || t.HasBuffOfType(BuffType.Stun) ||
                         t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Fear) ||
                         t.HasBuffOfType(BuffType.Taunt)))
                    {
                        CastQ();
                    }
                }
            }

            if (ComboActive || HarassActive)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                {
                    var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                    if (t != null)
                    {
                        CastQ();
                    }
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            var jungleMobs = Utils.GetMobs(Q.Range, Marksman.Utils.Utils.MobTypes.All);

            if (jungleMobs != null)
            {
                if (Q.IsReady())
                {
                    switch (Program.Config.Item("UseQ.Jungle").GetValue<StringList>().SelectedIndex)
                    {
                        case 1:
                        {
                            Q.Cast(jungleMobs);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.GetMobs(Q.Range, Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                Q.Cast(jungleMobs);
                            }
                            break;
                        }
                    }
                }

                if (W.IsReady())
                {
                    var jW = Program.Config.Item("UseW.Jungle").GetValue<StringList>().SelectedIndex;
                    if (jW != 0)
                    {
                        if (jW == 1)
                        {
                            jungleMobs = Utils.GetMobs(Orbwalking.GetRealAutoAttackRange(null) + 65,
                                Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                Q.Cast();
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
                            if (totalAa > jW)
                            {
                                W.Cast();
                            }
                        }
                    }
                }
            }
        }

        public override void ExecuteLaneClear()
        {
            var qJ = Program.Config.Item("UseQ.Lane").GetValue<StringList>().SelectedIndex;
            if (qJ != 0)
            {
                var minionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
                if (minionsQ != null)
                {
                    if (Q.IsReady())
                    {
                        var locQ = Q.GetLineFarmLocation(minionsQ);
                        if (minionsQ.Count == minionsQ.Count(m => ObjectManager.Player.Distance(m) < Q.Range) &&
                            locQ.MinionsHit > qJ && locQ.Position.IsValid())
                        {
                            Q.Cast(locQ.Position);
                        }
                    }
                }
            }
            var wJ = Program.Config.Item("UseW.Lane").GetValue<StringList>().SelectedIndex;
            if (wJ != 0)
            {
                var minionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                    Orbwalking.GetRealAutoAttackRange(null) + 165, MinionTypes.All);
                if (minionsW != null && minionsW.Count >= wJ)
                {
                    if (W.IsReady())
                    {
                        W.Cast();
                    }
                }
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseWC");

                if (W.IsReady() && useW)
                {
                    W.Cast();
                }
                else if (Q.IsReady() && useQ)
                {
                    CastQ();
                }
            }
        }

        private static void CastQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() && Q.IsReady() &&
                ObjectManager.Player.Distance(t.ServerPosition) <= Q.Range)
            {
                var Qpredict = Q.GetPrediction(t);
                var hithere = Qpredict.CastPosition.Extend(ObjectManager.Player.Position, -140);

                var Hitchance = HitChance.High;
                if (ObjectManager.Player.Distance(t) >= 850)
                    Hitchance = HitChance.VeryHigh;
                else if (ObjectManager.Player.Distance(t) < 850 && ObjectManager.Player.Distance(t) > 600)
                    Hitchance = HitChance.High;
                else
                    Hitchance = HitChance.Medium;
                if (Qpredict.Hitchance >= Hitchance)
                    Q.Cast(hithere);
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = {Q};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(false));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("AutoQ" + Id, "Auto Q on Stun/Slow/Fear/Taunt/Snare").SetValue(true));
            config.AddItem(new MenuItem("Misc.UseW.Turret" + Id, "Use W for Turret").SetValue(false));
            config.AddItem(new MenuItem("Misc.UseW.Inhibitor" + Id, "Use W for Inhibitor").SetValue(true));
            config.AddItem(new MenuItem("Misc.UseW.Nexus" + Id, "Use W for Nexus").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            string[] strQ = new string[5];
            strQ[0] = "Off";

            for (var i = 1; i < 5; i++)
            {
                strQ[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("UseQ.Lane", Utils.Tab + "Use Q:").SetValue(new StringList(strQ, 0)));
            config.AddItem(new MenuItem("UseQR.Lane", Utils.Tab + "Use Q for out of AA Range").SetValue(true));


            string[] strW = new string[5];
            strW[0] = "Off";

            for (var i = 1; i < 5; i++)
            {
                strW[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("UseW.Lane", Utils.Tab + "Use W:").SetValue(new StringList(strW, 0)));

            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("UseQ.Jungle", "Use Q").SetValue(
                    new StringList(new[] {"Off", "On", "Just for big Monsters"}, 1)));

            string[] strW = new string[8];
            strW[0] = "Off";
            strW[1] = "Just for big Monsters";

            for (var i = 2; i < 8; i++)
            {
                strW[i] = "If need to AA more than >= " + i;
            }

            config.AddItem(new MenuItem("UseW.Jungle", "Use W").SetValue(new StringList(strW, 4)));

            return true;
        }
    }
}
