#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Utils;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

#endregion

namespace Marksman.Champions
{
    using System.Threading;

    using Utils = LeagueSharp.Common.Utils;

    internal interface IKindred
    {
        void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target);
        void Drawing_OnDraw(EventArgs args);
        void Game_OnGameUpdate(EventArgs args);
        bool ComboMenu(Menu config);
        bool HarassMenu(Menu config);
        bool MiscMenu(Menu config);
        bool DrawingMenu(Menu config);
        bool LaneClearMenu(Menu config);
        //bool JungleClearMenu(Menu config);
    }

    internal class Kindred : Champion, IKindred
    {
        public static Spell Q;
        public static Spell E;
        public static Spell W;
        public static Spell R;
        public static Obj_AI_Hero KindredECharge;
        public static List<DangerousSpells> DangerousList = new List<DangerousSpells>();

        public Kindred()
        {
            Q = new Spell(SpellSlot.Q, 375);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 740);
            R = new Spell(SpellSlot.R, 1100);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotCircle);

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

            DangerousList.Add(new DangerousSpells("darius", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("garen", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("leesin", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("nautilius", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("syndra", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("warwick", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("zed", SpellSlot.R));
            DangerousList.Add(new DangerousSpells("chogath", SpellSlot.R));

            Marksman.Utils.Utils.PrintMessage("Kindred loaded.");
        }

        public override void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (args.Buff.Name.ToLower() == "kindredecharge" && !sender.IsMe)
            {
                KindredECharge = sender as Obj_AI_Hero;
            }
        }

        public override void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (args.Buff.Name.ToLower() == "kindredecharge" && !sender.IsMe)
            {
                KindredECharge = null;
            }
        }

        public override void OnCreateObject(GameObject sender, EventArgs args)
        {
        }

        public override void OnDeleteObject(GameObject sender, EventArgs args)
        {
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = GetValue<StringList>("DrawQ").SelectedIndex;
            switch (drawQ)
            {
                case 1:
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.Aqua);
                    break;
                case 2:
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range + Orbwalking.GetRealAutoAttackRange(null) + 65, Color.Aqua);
                    break;
            }
            Spell[] spellList = { W, E, R };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        public void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!R.IsReady())
            {
                return;
            }

            if (sender == null)
            {
                return;
            }

            if (sender.IsEnemy && sender is Obj_AI_Hero && args.Target.IsMe)
            {
                foreach (
                    var c in
                        DangerousList.Where(c => ((Obj_AI_Hero)sender).ChampionName.ToLower() == c.ChampionName)
                            .Where(c => args.SData.Name == ((Obj_AI_Hero)sender).GetSpell(c.SpellSlot).Name))
                {
                    if (ObjectManager.Player.HealthPercent < 10)
                        R.Cast(ObjectManager.Player.Position);
                }
            }

            
            if (R.IsReady())
            {
                var x = 0d;
                if (ObjectManager.Player.HealthPercent < 20 && ObjectManager.Player.CountEnemiesInRange(500) > 0)
                {
                    x = HeroManager.Enemies.Where(e => e.IsValidTarget(1000))
                        .Aggregate(0, (current, enemy) => (int)(current + enemy.Health));
                }
                if (ObjectManager.Player.Health < x)
                {
                    R.Cast(ObjectManager.Player.Position);
                }
                
                if (Program.Config.Item("UserRC").GetValue<bool>() &&
                    ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * .2)
                {
                    if (!sender.IsMe && sender.IsEnemy && R.IsReady() && args.Target.IsMe) // for minions attack
                    {
                        R.Cast(ObjectManager.Player.Position);
                    }
                    else if (!sender.IsMe && sender.IsEnemy && (sender is Obj_AI_Hero || sender is Obj_AI_Turret) &&
                             args.Target.IsMe && R.IsReady())
                    {
                        R.Cast(ObjectManager.Player.Position);
                    }
                }
            }
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            foreach (
                var target in
                    HeroManager.Enemies.Where(
                        e =>
                            e.IsValid && e.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(null) + 65 &&
                            e.IsVisible).Where(target => target.HasBuff("kindredcharge")))
            {
                Orbwalker.ForceTarget(target);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (R.IsReady())
            {
                var x = 0d;
                if (ObjectManager.Player.HealthPercent < 20 && ObjectManager.Player.CountEnemiesInRange(500) > 0)
                {
                    x = HeroManager.Enemies.Where(e => e.IsValidTarget(1000))
                        .Aggregate(0, (current, enemy) => (int)(current + enemy.Health));
                }
                if (ObjectManager.Player.Health < x)
                {
                    R.Cast(ObjectManager.Player.Position);
                }
            }

            Obj_AI_Hero t = null;
            if (KindredECharge != null)
            {
                t = KindredECharge;
                TargetSelector.SetTarget(KindredECharge);
            }
            else
            {
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            }


            if (!t.IsValidTarget())
            {
                return;
            }

            if (ComboActive && !t.HasKindredUltiBuff())
            {
                if (t.IsValidTarget(Q.Range + Orbwalking.GetRealAutoAttackRange(null) + 65) && !t.HasKindredUltiBuff())
                {
                    if (Q.IsReady())
                    {
                        Q.Cast(Game.CursorPos);
                    }

                    if (E.IsReady() && t.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(t);
                    }

                    if (W.IsReady() && t.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }
                }
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "W").SetValue(true));
            config.AddItem(
                new MenuItem("UseEH" + Id, "Use E").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            config.AddItem(
                new MenuItem("UseETH", "E (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQL" + Id, "Use Q").SetValue(true)).ValueChanged +=
                delegate (object sender, OnValueChangeEventArgs args)
                {
                    config.Item("UseQLM").Show(args.GetNewValue<bool>());
                    Program.CClass.Config.Item("LaneMinMana").Show(args.GetNewValue<bool>());
                };
            config.AddItem(new MenuItem("UseQLM", "Min. Minion:").SetValue(new Slider(2, 1, 3)));
            config.AddItem(new MenuItem("UseWL", "Use W").SetValue(false));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new StringList(new[] { "Off", "Q Range", "Q + AA Range" }, 2)));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawR" + Id, "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);

            config.AddItem(dmgAfterComboItem);

            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            return false;
        }

        public override void ExecuteLaneClear()
        {
            var useQ = Program.Config.Item("UseQL").GetValue<StringList>().SelectedIndex;

            var minion =
                MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range)
                    .FirstOrDefault(m => m.Health < ObjectManager.Player.GetSpellDamage(m, SpellSlot.Q));

            if (minion != null)
            {
                switch (useQ)
                {
                    case 1:
                        minion =
                            MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range)
                                .FirstOrDefault(
                                    m =>
                                        m.Health < ObjectManager.Player.GetSpellDamage(m, SpellSlot.Q)
                                        && m.Health > ObjectManager.Player.TotalAttackDamage);
                        Q.Cast(minion);
                        break;

                    case 2:
                        minion =
                            MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range)
                                .FirstOrDefault(
                                    m =>
                                        m.Health < ObjectManager.Player.GetSpellDamage(m, SpellSlot.Q)
                                        && ObjectManager.Player.Distance(m)
                                        > Orbwalking.GetRealAutoAttackRange(null) + 65);
                        Q.Cast(minion);
                        break;
                }
            }
        }

        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQJ" + Id, "Use Q").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 1)));
            config.AddItem(new MenuItem("UseWJ" + Id, "Use W").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 1)));
            config.AddItem(new MenuItem("UseEJ" + Id, "Use E").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 1)));

            return true;
        }

        public override void ExecuteJungleClear()
        {
            var jungleMobs = Marksman.Utils.Utils.GetMobs(Q.Range + Orbwalking.GetRealAutoAttackRange(null) + 65,
                Marksman.Utils.Utils.MobTypes.All);

            if (jungleMobs != null)
            {
                switch (GetValue<StringList>("UseQJ").SelectedIndex)
                {
                    case 1:
                        {
                            if (jungleMobs.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                                Q.Cast(jungleMobs.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65)
                                    ? Game.CursorPos
                                    : jungleMobs.Position);
                            break;
                        }
                    case 2:
                        {
                            jungleMobs = Marksman.Utils.Utils.GetMobs(
                                Q.Range + Orbwalking.GetRealAutoAttackRange(null) + 65,
                                Marksman.Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                Q.Cast(jungleMobs.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65)
                                    ? Game.CursorPos
                                    : jungleMobs.Position);
                            }
                            break;
                        }
                }

                switch (GetValue<StringList>("UseWJ").SelectedIndex)
                {
                    case 1:
                        {
                            if (jungleMobs.IsValidTarget(W.Range))
                                W.Cast(jungleMobs.Position);
                            break;
                        }
                    case 2:
                        {
                            jungleMobs = Marksman.Utils.Utils.GetMobs(E.Range, Marksman.Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                W.Cast(jungleMobs.Position);
                            }
                            break;
                        }
                }

                switch (GetValue<StringList>("UseEJ").SelectedIndex)
                {
                    case 1:
                        {
                            if (jungleMobs.IsValidTarget(E.Range))
                                E.CastOnUnit(jungleMobs);
                            break;
                        }
                    case 2:
                        {
                            jungleMobs = Marksman.Utils.Utils.GetMobs(E.Range, Marksman.Utils.Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                E.CastOnUnit(jungleMobs);
                            }
                            break;
                        }
                }

            }
        }
    }
}
