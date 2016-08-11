#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Marksman.Champions
{
    using System.Diagnostics.Eventing.Reader;

    internal class Quinn : Champion
    {
        public static float ValorMinDamage;
        public static float ValorMaxDamage;
        public Spell E;
        public Spell Q;
        public Spell R;

        public Quinn()
        {
            Utils.Utils.PrintMessage("Quinn loaded.");

            Q = new Spell(SpellSlot.Q, 1010);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 160f, 1150, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.25f, 2000f);

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        public override void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
        }

        public override void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            
        }

        public void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Program.Config.Item("Misc.AntiGapCloser").GetValue<bool>())
            {
                return;
            }

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                E.CastOnUnit(gapcloser.Sender);
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t == null || (!ComboActive && !HarassActive) || unit.IsMe) return;

            if (Q.IsReady() && GetValue<bool>("UseQ" + (ComboActive ? "C" : "H")))
                Q.Cast(t, false, true);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, E};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);

                if (menuItem.Active && spell.Level > 0 && IsValorMode)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, menuItem.Color);
            }
        }

        public static bool IsPositionSafe(Obj_AI_Hero target, Spell spell)
            // use underTurret and .Extend for this please
        {
            var predPos = spell.GetPrediction(target).UnitPosition.To2D();
            var myPos = ObjectManager.Player.Position.To2D();
            var newPos = (target.Position.To2D() - myPos);
            newPos.Normalize();

            var checkPos = predPos + newPos*(spell.Range - Vector2.Distance(predPos, myPos));
            Obj_Turret closestTower = null;

            foreach (var tower in ObjectManager.Get<Obj_Turret>()
                .Where(tower => tower.IsValid && !tower.IsDead && Math.Abs(tower.Health) > float.Epsilon)
                .Where(tower => Vector3.Distance(tower.Position, ObjectManager.Player.Position) < 1450))
            {
                closestTower = tower;
            }

            if (closestTower == null)
                return true;

            if (Vector2.Distance(closestTower.Position.To2D(), checkPos) <= 910)
                return false;

            return true;
        }

        public static bool isHePantheon(Obj_AI_Hero target)
        {
            /* Quinn's Spell E can do nothing when Pantheon's passive is active. */
            return target.Buffs.All(buff => buff.Name == "pantheonpassivebuff");
        }

        private static bool IsValorMode
        {
            get
            {
                return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "QuinnRFinale";
            }
        }

        public static void calculateValorDamage()
        {
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level > 0)
            {
                ValorMinDamage = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level*50 + 50;
                ValorMinDamage += ObjectManager.Player.BaseAttackDamage*50;

                ValorMaxDamage = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level*100 + 100;
                ValorMaxDamage += ObjectManager.Player.BaseAttackDamage*100;
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            var enemy =
                HeroManager.Enemies.Find(
                    e => e.Buffs.Any(b => b.Name.ToLower() == "quinnw_cosmetic" && e.IsValidTarget(E.Range)));
            if (enemy != null)
            {
                if (enemy.Distance(ObjectManager.Player.Position) > Orbwalking.GetRealAutoAttackRange(null) + 65)
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, enemy);
                }
                Orbwalker.ForceTarget(enemy);
            }

            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                    return;
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Q.Cast(t, false, true);
            }

            if (ComboActive || HarassActive)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));

                if (Orbwalking.CanMove(100))
                {
                    if (E.IsReady() && useE)
                    {
                        var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (t.IsValidTarget() && !t.IsZombie && !isHePantheon(t) && !t.HasBuff("QuinnW_Cosmetic", true))
                        {
                            E.CastOnUnit(t);
                        }
                    }

                    if (Q.IsReady() && useQ)
                    {
                        var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                        if (t.IsValidTarget() && !t.IsZombie)
                            Q.Cast(t);
                    }

                    if (IsValorMode && !E.IsReady())
                    {
                        var vTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                        if (vTarget != null)
                        {
                            calculateValorDamage();
                            if (vTarget.Health >= ValorMinDamage && vTarget.Health <= ValorMaxDamage)
                                R.Cast();
                        }
                    }
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            if (Q.IsReady())
            {
                var jQ = Marksman.Utils.Utils.GetMobs(Orbwalking.GetRealAutoAttackRange(null) + 65, Marksman.Utils.Utils.MobTypes.All);
                if (jQ != null)
                {
                    switch (GetValue<StringList>("UseQJ").SelectedIndex)
                    {
                        case 1:
                            {
                                Q.Cast(jQ);
                                break;
                            }
                        case 2:
                            {
                                jQ = Utils.Utils.GetMobs(Orbwalking.GetRealAutoAttackRange(null) + 65, Utils.Utils.MobTypes.BigBoys);
                                if (jQ != null)
                                {
                                    Q.Cast(jQ);
                                }
                                break;
                            }
                    }
                }
            }


            if (E.IsReady())
            {
                var jungleMobs = Marksman.Utils.Utils.GetMobs(E.Range, Marksman.Utils.Utils.MobTypes.All);

                if (jungleMobs != null)
                {
                    switch (GetValue<StringList>("UseEJ").SelectedIndex)
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
            }
        }
        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false,
                    Color.FromArgb(100, 255, 255, 255))));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("Lane.Non", ObjectManager.Player.ChampionName + " Doesn't Support Lane Clear"));
            return true;
        }
        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQJ" + Id, "Use Q").SetValue(new StringList(new[] { "Off", "On", "Just for big Monsters" }, 1)));
            config.AddItem(new MenuItem("UseEJ" + Id, "Use E").SetValue(new StringList(new[] { "Off", "On", "Just for big Monsters" }, 1)));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("Misc.AntiGapCloser" + Id, "E Anti Gap Closer").SetValue(true));
            return true;
        }
    }
}
