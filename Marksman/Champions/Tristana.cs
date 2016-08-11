#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Utils;
using SharpDX;
using Font = SharpDX.Direct3D9.Font;

#endregion

namespace Marksman.Champions
{
    internal class Tristana : Champion
    {
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        
        public Tristana()
        {
            Q = new Spell(SpellSlot.Q, 703);

            W = new Spell(SpellSlot.W, 900);
            W.SetSkillshot(.50f, 250f, 1400f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 703);
            R = new Spell(SpellSlot.R, 703);

            Utility.HpBarDamageIndicator.DamageToUnit = TristanaData.GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;

            //font = new Font(
            //    Drawing.Direct3DDevice,
            //    new FontDescription
            //    {
            //        FaceName = "Segoe UI",
            //        Height = 25,
            //        OutputPrecision = FontPrecision.Default,
            //        Quality = FontQuality.Default
            //    });
            //fontsmall = new Font(
            //    Drawing.Direct3DDevice,
            //    new FontDescription
            //    {
            //        FaceName = "Segoe UI",
            //        Height = 15,
            //        OutputPrecision = FontPrecision.Default,
            //        Quality = FontQuality.Default
            //    });

            Utils.Utils.PrintMessage("Tristana loaded.");
        }

        public void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && gapcloser.Sender.IsValidTarget(R.Range) && GetValue<bool>("UseRMG"))
                R.CastOnUnit(gapcloser.Sender);
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (R.IsReady() && unit.IsValidTarget(R.Range) && GetValue<bool>("UseRMI"))
                R.CastOnUnit(unit);
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (GetValue<bool>("Misc.UseQ.Inhibitor") && args.Target is Obj_BarracksDampener && Q.IsReady())
            {
                if (((Obj_BarracksDampener) args.Target).Health >= Player.TotalAttackDamage*3)
                {
                    Q.Cast();
                }
            }

            if (GetValue<bool>("Misc.UseQ.Nexus") && args.Target is Obj_HQ && Q.IsReady())
            {
                Q.Cast();
            }

            var unit = args.Target as Obj_AI_Turret;
            if (unit != null)
            {
                if (GetValue<bool>("UseEM") && E.IsReady())
                {
                    if (((Obj_AI_Turret) args.Target).Health >= Player.TotalAttackDamage*3)
                    {
                        E.CastOnUnit(unit);
                    }
                }

                if (GetValue<bool>("Misc.UseQ.Turret") && Q.IsReady())
                {
                    if (((Obj_AI_Turret) args.Target).Health >= Player.TotalAttackDamage*3)
                    {
                        Q.Cast();
                    }
                }
            }
            if (args.Target is Obj_AI_Hero)
            {
                var t = args.Target as Obj_AI_Hero;
                if (t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null)) && ComboActive)
                {
                    var useQ = Q.IsReady() && GetValue<bool>("UseQC");
                    if (useQ)
                        Q.CastOnUnit(Player);
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }
            if (!Orbwalking.CanMove(100))
            {
                return;
            }

            var getEMarkedEnemy = TristanaData.GetEMarkedEnemy;
            if (getEMarkedEnemy != null)
            {
                TargetSelector.SetTarget(getEMarkedEnemy);
            }
            else
            {
                var attackRange = Orbwalking.GetRealAutoAttackRange(Player);
                TargetSelector.SetTarget(TargetSelector.GetTarget(attackRange, TargetSelector.DamageType.Physical));
            }

            Q.Range = 600 + 5*(Player.Level - 1);
            E.Range = 630 + 7*(Player.Level - 1);
            R.Range = 630 + 7*(Player.Level - 1);

            if (!Player.HasBuff("Recall") && GetValue<KeyBind>("UseETH").Active && ToggleActive && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget(E.Range))
                {
                    if (Program.Config.Item("DontEToggleHarass" + t.ChampionName) != null &&
                        Program.Config.Item("DontEToggleHarass" + t.ChampionName).GetValue<bool>() == false)
                    {
                        E.CastOnUnit(t);
                    }
                }
            }

            var useW = W.IsReady() && GetValue<bool>("UseWC");
            var useWc = W.IsReady() && GetValue<bool>("UseWCS");
            var useWks = W.IsReady() && GetValue<bool>("UseWKs");
            var useE = E.IsReady() && GetValue<bool>("UseEC");
            var useR = R.IsReady() && GetValue<bool>("UseRM") && R.IsReady();

            if (ComboActive)
            {
                Obj_AI_Hero t;
                if (TristanaData.GetEMarkedEnemy != null)
                {
                    t = TristanaData.GetEMarkedEnemy;
                    TargetSelector.SetTarget(TristanaData.GetEMarkedEnemy);
                }
                else
                {
                    t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                }

                if (useE && E.IsReady())
                {
                    if (E.IsReady() && t.IsValidTarget(E.Range))
                        E.CastOnUnit(t);
                }

                if (useW)
                {
                    t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        W.Cast(t);
                }
                /*
                else if (useWks)
                {
                    t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget() && t.Health < TristanaData.GetWDamage)
                        W.Cast(t);
                }
                else if (useWc)
                {
                    t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget() && TristanaData.GetEMarkedCount == 4)
                        W.Cast(t);
                }
                */
            }

            if (ComboActive)
            {
                if (useR)
                {
                    var t = TargetSelector.GetTarget(R.Range - 10, TargetSelector.DamageType.Physical);

                    if (!t.IsValidTarget())
                        return;

                    if (Player.GetSpellDamage(t, SpellSlot.R) - 30 < t.Health ||
                        t.Health < Player.GetAutoAttackDamage(t, true))
                        return;

                    R.CastOnUnit(t);
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            var jungleMobs = Marksman.Utils.Utils.GetMobs(E.Range, Marksman.Utils.Utils.MobTypes.All);

            if (jungleMobs != null)
            {
                if (E.IsReady())
                {
                    switch (GetValue<StringList>("Jungle.UseE").SelectedIndex)
                    {
                        case 1:
                        {
                            E.CastOnUnit(jungleMobs);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.Utils.GetMobs(E.Range, Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                E.CastOnUnit(jungleMobs);
                            }
                            break;
                        }
                    }
                }

                if (Q.IsReady())
                {
                    var jE = GetValue<StringList>("Jungle.UseQ").SelectedIndex;
                    if (jE != 0)
                    {
                        if (jE == 1)
                        {
                            jungleMobs = Utils.Utils.GetMobs(
                                Orbwalking.GetRealAutoAttackRange(null) + 65,
                                Utils.Utils.MobTypes.BigBoys);
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
                            if (totalAa > jE)
                            {
                                Q.Cast();
                            }

                        }
                    }
                }
            }
        }

        public override void ExecuteLaneClear()
        {

            if (E.IsReady())
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.Position, E.Range, MinionTypes.All,
                    MinionTeam.Enemy);

                if (minions != null)
                {
                    var eJ = Program.Config.Item("UseE.Lane").GetValue<StringList>().SelectedIndex;
                    if (eJ != 0)
                    {
                        var mE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + 175,
                            MinionTypes.All);
                        var locW = E.GetCircularFarmLocation(mE, 175);
                        if (locW.MinionsHit >= eJ && E.IsInRange(locW.Position.To3D()))
                        {
                            foreach (
                                var x in
                                    ObjectManager.Get<Obj_AI_Minion>()
                                        .Where(m => m.IsEnemy && !m.IsDead && m.Distance(locW.Position) < 100))
                            {
                                E.CastOnUnit(x);
                            }
                        }
                    }
                }
                if (Q.IsReady())
                {
                    var jE = Program.Config.Item("UseQ.Lane").GetValue<StringList>().SelectedIndex;
                    if (jE != 0)
                    {
                        var totalAa =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    m =>
                                        m.IsEnemy && !m.IsDead &&
                                        m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null)))
                                .Sum(mob => (int) mob.Health);

                        totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
                        if (totalAa > jE)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            Spell[] spellList = {W};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color, 1);
            }

            var drawE = GetValue<Circle>("DrawE");
            if (drawE.Active)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, drawE.Color, 1);
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(false));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseWKs" + Id, "Use W Kill Steal").SetValue(false));
            config.AddItem(new MenuItem("UseWCS" + Id, "Complete E stacks with W").SetValue(false));

            config.AddSubMenu(new Menu("Don't Use E to", "DontUseE"));
            {
                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                {
                    config.SubMenu("DontUseE")
                        .AddItem(new MenuItem("DontUseE" + enemy.ChampionName, enemy.ChampionName).SetValue(false));
                }
            }

            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddSubMenu(new Menu("Don't E Toggle to", "DontEToggleHarass"));
            {
                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
                {
                    config.SubMenu("DontEToggleHarass")
                        .AddItem(
                            new MenuItem("DontEToggleHarass" + enemy.ChampionName, enemy.ChampionName).SetValue(false));
                }
            }

            config.AddItem(
                new MenuItem("UseETH" + Id, "Use E (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle))).Permashow(true, "Tristana | Toggle E");

            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(true, System.Drawing.Color.Beige)));

            var drawE = new Menu("Draw E", "menuDrawE");
            {
                drawE.AddItem(
                    new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(true, System.Drawing.Color.Beige)));
//                drawE.AddItem(new MenuItem("DrawEMarkEnemy" + Id, "E Marked Enemy").SetValue(new Circle(true,System.Drawing.Color.GreenYellow)));
//                drawE.AddItem(new MenuItem("DrawEMarkStatus" + Id, "E Marked Status").SetValue(true));
                config.AddSubMenu(drawE);
            }

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
            config.AddItem(dmgAfterComboItem);

            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            var menuMiscQ = new Menu("Q Spell", "MiscQ");
            menuMiscQ.AddItem(new MenuItem("Misc.UseQ.Turret" + Id, "Use Q for Turret").SetValue(true));
            menuMiscQ.AddItem(new MenuItem("Misc.UseQ.Inhibitor" + Id, "Use Q for Inhibitor").SetValue(true));
            menuMiscQ.AddItem(new MenuItem("Misc.UseQ.Nexus" + Id, "Use Q for Nexus").SetValue(true));
            config.AddSubMenu(menuMiscQ);

            var menuMiscW = new Menu("W Spell", "MiscW");
            menuMiscW.AddItem(
                new MenuItem("ProtectWMana", "[Soon/WIP] Protect my mana for [W] if my Level < ").SetValue(new Slider(
                    8, 2, 18)));
            menuMiscW.AddItem(new MenuItem("UseWM" + Id, "Use W KillSteal").SetValue(false));
            config.AddSubMenu(menuMiscW);

            var menuMiscE = new Menu("E Spell", "MiscE");
            menuMiscE.AddItem(new MenuItem("UseEM" + Id, "Use E for Enemy Turret").SetValue(true));
            config.AddSubMenu(menuMiscE);

            var menuMiscR = new Menu("R Spell", "MiscR");
            {
                menuMiscR.AddItem(
                    new MenuItem("ProtectRMana", "[Soon/WIP] Protect my mana for [R] if my Level < ").SetValue(
                        new Slider(11, 6, 18)));
                menuMiscR.AddItem(new MenuItem("UseRM" + Id, "Use R KillSteal").SetValue(true));
                menuMiscR.AddItem(new MenuItem("UseRMG" + Id, "Use R Gapclosers").SetValue(true));
                menuMiscR.AddItem(new MenuItem("UseRMI" + Id, "Use R Interrupt").SetValue(true));
                config.AddSubMenu(menuMiscR);
            }

            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            string[] strQ = new string[7];
            strQ[0] = "Off";

            for (var i = 1; i < 7; i++)
            {
                strQ[i] = "If need to AA more than >= " + i;
            }

            config.AddItem(new MenuItem("UseQ.Lane", "Q:").SetValue(new StringList(strQ, 0)))
                .SetFontStyle(FontStyle.Regular, Q.MenuColor());


            string[] strE = new string[5];
            strE[0] = "Off";

            for (var i = 1; i < 5; i++)
            {
                strE[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("UseE.Lane", "E:").SetValue(new StringList(strE, 0)))
                .SetFontStyle(FontStyle.Regular, E.MenuColor());
            ;
            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            string[] strLaneMinCount = new string[8];
            strLaneMinCount[0] = "Off";
            strLaneMinCount[1] = "Just for big Monsters";

            for (var i = 2; i < 8; i++)
            {
                strLaneMinCount[i] = "If need to AA more than >= " + i;
            }

            config.AddItem(new MenuItem("Jungle.UseQ", "Q:").SetValue(new StringList(strLaneMinCount, 4)))
                .SetFontStyle(FontStyle.Regular, Q.MenuColor());
            config.AddItem(
                new MenuItem("Jungle.UseE", "E:").SetValue(new StringList(new[] {"Off", "On", "Just for big Monsters"},
                    1))).SetFontStyle(FontStyle.Regular, E.MenuColor());

            return true;
        }

        public class TristanaData
        {
            public static double GetWDamage
            {
                get
                {
                    if (W.IsReady())
                    {
                        var wDamage = new double[] {80, 105, 130, 155, 180}[W.Level - 1] + 0.5*Player.FlatMagicDamageMod;
                        if (GetEMarkedCount > 0 && GetEMarkedCount < 4)
                        {
                            return wDamage + (wDamage*GetEMarkedCount*.20);
                        }
                        switch (GetEMarkedCount)
                        {
                            case 0:
                                return wDamage;
                            case 4:
                                return wDamage*2;
                        }
                    }
                    return 0;
                }
            }

            public static float GetComboDamage(Obj_AI_Hero t)
            {
                if (!t.IsValidTarget(W.Range))
                {
                    return 0;
                }

                var fComboDamage = 0d;
                /*
                    if (Q.IsReady())
                    {
                        var baseAttackSpeed = 0.656 + (0.656 / 100 * (Player.Level - 1) * 1.5);
                        var qExtraAttackSpeed = new double[] { 30, 50, 70, 90, 110 }[Q.Level - 1];
                        var attackDelay = (float) (baseAttackSpeed + (baseAttackSpeed / 100 * qExtraAttackSpeed));
                        attackDelay = (float) Math.Round(attackDelay, 2);

                        attackDelay *= 5;
                        attackDelay *= (float) Math.Floor(Player.TotalAttackDamage);
                        fComboDamage += attackDelay;
                    }
                    */
                if (W.IsReady())
                {
                    //fComboDamage += GetWDamage;
                    fComboDamage += W.GetDamage(t);
                }

                if (E.IsReady())
                {
                    fComboDamage += E.GetDamage(t);
                }

                if (R.IsReady())
                {
                    fComboDamage += R.GetDamage(t);
                    //new double[] {300, 400, 500}[R.Level - 1] + Player.FlatMagicDamageMod);
                }
                return (float) fComboDamage;
            }

            public static Obj_AI_Hero GetEMarkedEnemy
                =>
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            enemy =>
                                !enemy.IsDead &&
                                enemy.IsValidTarget(W.Range + Orbwalking.GetRealAutoAttackRange(Player)))
                        .FirstOrDefault(enemy => enemy.Buffs.Any(buff => buff.DisplayName == "TristanaEChargeSound"));

            public static int GetEMarkedCount
                =>
                    GetEMarkedEnemy?.Buffs.Where(buff => buff.DisplayName == "TristanaECharge")
                        .Select(xBuff => xBuff.Count)
                        .FirstOrDefault() ?? 0;
        }
    }
}