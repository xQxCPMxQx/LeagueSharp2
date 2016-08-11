#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using SharpDX.Direct3D9;

#endregion

namespace Marksman.Champions
{

    internal class Twitch : Champion
    {
        internal class EnemyMarker
        {
            public string ChampionName { get; set; }
            public double ExpireTime { get; set; }
            public int BuffCount { get; set; }
        }
        public static Spell W;
        public static Spell E;
        private static string twitchEBuffName = "TwitchDeadlyVenom";
        public Twitch()
        {
            W = new Spell(SpellSlot.W, 950);
            W.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotCircle);
            E = new Spell(SpellSlot.E, 1200);

            //Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            //Utility.HpBarDamageIndicator.Enabled = true;
            Utils.Utils.PrintMessage("Twitch loaded.");
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t == null || (!ComboActive && !HarassActive) || !unit.IsMe)
                return;

            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

            if (useW && W.IsReady())
                W.Cast(t, false, true);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = {W};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //var killableMinionCount = 0;
            //foreach (
            //    var m in
            //        MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range)
            //            .Where(x => E.CanCast(x) && x.Health <= E.GetDamage(x)))
            //{
            //    if (m.SkinName == "SRU_ChaosMinionSiege" || m.SkinName == "SRU_ChaosMinionSuper")
            //        killableMinionCount += 2;
            //    else
            //        killableMinionCount++;
            //    Render.Circle.DrawCircle(m.Position, (float) (m.BoundingRadius*1.5), Color.White);
            //}

            //if (killableMinionCount >= 3 && E.IsReady() && ObjectManager.Player.ManaPercent > 15)
            //{
            //    E.Cast();
            //}

            //foreach (
            //    var m in
            //        MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionGroup.All,
            //            MinionTeam.Neutral).Where(m => E.CanCast(m) && m.Health <= E.GetDamage(m)))
            //{
            //    if (m.SkinName.ToLower().Contains("baron") || m.SkinName.ToLower().Contains("dragon") && E.CanCast(m))
            //        E.Cast(m);
            //    else
            //        Render.Circle.DrawCircle(m.Position, (float) (m.BoundingRadius*1.5), Color.White);
            //}
         
            if (Orbwalking.CanMove(100) && (ComboActive || HarassActive))
            {
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
                var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.HasKindredUltiBuff())
                    return;

                if (useW)
                {
                    if (W.IsReady() && t.IsValidTarget(W.Range))
                        W.Cast(t, false, true);
                }

                if (useE && E.IsReady() && t.IsValidTarget())
                {
                    var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    if (eTarget.IsValidTarget(E.Range) && eTarget.GetBuffCount("TwitchDeadlyVenom") == 6)
                    {
                        E.Cast();
                    }
                }
                
                if (ObjectManager.Get<Obj_AI_Hero>().Find(e1 => e1.IsValidTarget(E.Range) && E.IsKillable(e1)) != null)
                {
                    E.Cast();
                }
            }

            if (GetValue<bool>("UseEM") && E.IsReady())
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(E.Range) &&
                                    (ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E) - 10 > hero.Health)))
                {
                    E.Cast();
                }
            }
        }

        public override void ExecuteLaneClear()
        {
            var prepareMinions = Program.Config.Item("PrepareMinionsE.Lane").GetValue<StringList>().SelectedIndex;
            if (prepareMinions != 0)
            {
                List<Obj_AI_Minion> list = new List<Obj_AI_Minion>();

                IEnumerable<Obj_AI_Minion> minions =
                    from m in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                m =>
                                    m.Health > ObjectManager.Player.TotalAttackDamage &&
                                    m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                    select m;

                var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
                foreach (var m in objAiMinions)
                {
                    if (m.GetBuffCount(twitchEBuffName) > 0)
                    {
                        list.Add(m);
                    }
                    else
                    {
                        list.Remove(m);
                    }
                }

                foreach (var l in objAiMinions.Except(list).ToList())
                {
                    Program.CClass.Orbwalker.ForceTarget(l);
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            var jungleWValue = Program.Config.Item("UseW.Jungle").GetValue<StringList>().SelectedIndex;
            if (W.IsReady() && jungleWValue != 0)
            {
                var jungleMobs = Utils.Utils.GetMobs(W.Range, 
                    jungleWValue != 3 ? Utils.Utils.MobTypes.All : Utils.Utils.MobTypes.BigBoys,
                    jungleWValue != 3 ? jungleWValue : 1);

                if (jungleMobs != null)
                {
                    W.Cast(jungleMobs);
                }
            }

            if (E.IsReady() && Program.Config.Item("UseE.Jungle").GetValue<StringList>().SelectedIndex != 0)
            {
                var jungleMobs = Utils.Utils.GetMobs(E.Range, Program.Config.Item("UseE.Jungle").GetValue<StringList>().SelectedIndex == 1
                        ? Utils.Utils.MobTypes.All
                        : Utils.Utils.MobTypes.BigBoys);

                if (jungleMobs != null && E.CanCast(jungleMobs) && jungleMobs.Health <= E.GetDamage(jungleMobs) + 20)
                {
                    E.Cast();
                }
            }
        }


        private static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (E.IsReady())
                fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);

            if (ObjectManager.Player.GetSpellSlot("summonerdot") != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(ObjectManager.Player.GetSpellSlot("summonerdot")) ==
                SpellState.Ready && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3144) && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(3153) && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            return fComboDamage;
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E max Stacks").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E at max Stacks").SetValue(false));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
            config.AddItem(dmgAfterComboItem);

            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseEM" + Id, "Use E KS").SetValue(true));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("PrepareMinionsE.Lane", "Prepare Minions for E").SetValue(new StringList(new []{ "Off", "Everytime", "Just Under Ally Turret" }, 2)));

            string[] strW = new string[6];
            strW[0] = "Off";

            for (var i = 1; i < 6; i++)
            {
                strW[i] = "If Could Infect Minion Count>= " + i;
            }

            config.AddItem(new MenuItem("UseW.Lane", "Use W:").SetValue(new StringList(strW, 0)));


            string[] strE = new string[6];
            strE[0] = "Off";

            for (var i = 1; i < 6; i++)
            {
                strE[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("UseE.Lane", "Use E:").SetValue(new StringList(strE, 0)));
            return true;
        }
        public override bool JungleClearMenu(Menu config)
        {

            string[] strW = new string[4];
            strW[0] = "Off";
            strW[3] = "Just big Monsters";

            for (var i = 1; i < 3; i++)
            {
                strW[i] = "If Could Infect Mobs Count>= " + i;
            }
            
            config.AddItem(new MenuItem("UseW.Jungle", "Use W:").SetValue(new StringList(strW, 3)));

            //config.AddItem(new MenuItem("UseW.Jungle", "Use W").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 2)));
            config.AddItem(new MenuItem("UseE.Jungle", "Use E").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 2)));

            return true;
        }
    }
}
