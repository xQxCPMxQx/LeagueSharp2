#region
using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Utils;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Marksman.Champions
{
    internal class Lucian : Champion
    {
        public static Spell Q, Q2;

        public static Spell W;

        public static Spell E;

        public static Spell R;

        public static bool DoubleHit = false;

        private static int xAttackLeft;

        private static float xPassiveUsedTime;

        public Lucian()
        {
            Utils.Utils.PrintMessage("Lucian loaded.");

            Q = new Spell(SpellSlot.Q, 760);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 1000);

            Q.SetSkillshot(0.45f, 60f, 1100f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1400);

            xPassiveUsedTime = Game.Time;

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
        }

        public static Obj_AI_Base QMinion(Obj_AI_Hero t)
        {
            var m = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.NotAlly, MinionOrderTypes.None);

            return (from vM
                        in m.Where(vM => vM.IsValidTarget(Q.Range))
                    let endPoint = vM.ServerPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -Q2.Range).To3D()
                    where
                        vM.Distance(t) <= t.Distance(ObjectManager.Player) &&
                        Intersection(ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(), t.ServerPosition.To2D(), t.BoundingRadius + vM.BoundingRadius)
                    //Intersection(ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(), t.ServerPosition.To2D(), t.BoundingRadius + Q.Width/4)
                    select vM).FirstOrDefault();
            //get
            //{
            //    var vTarget = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
            //    var vMinions = MinionManager.GetMinions(
            //        ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly,
            //        MinionOrderTypes.None);

            //    return (from vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(Q.Range))
            //            let endPoint =
            //                vMinion.ServerPosition.To2D()
            //                    .Extend(ObjectManager.Player.ServerPosition.To2D(), -Q2.Range)
            //                    .To3D()
            //            where
            //                vMinion.Distance(vTarget) <= vTarget.Distance(ObjectManager.Player) &&
            //                Intersection(ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(),
            //                    vTarget.ServerPosition.To2D(), vTarget.BoundingRadius + vMinion.BoundingRadius)
            //            select vMinion).FirstOrDefault();
            //}
        }
        public static bool IsPositionSafeForE(Obj_AI_Hero target, Spell spell)
        {
            var predPos = spell.GetPrediction(target).UnitPosition.To2D();
            var myPos = ObjectManager.Player.Position.To2D();
            var newPos = (target.Position.To2D() - myPos);
            newPos.Normalize();

            var checkPos = predPos + newPos * (spell.Range - Vector2.Distance(predPos, myPos));
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

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, Q2, W, E, R };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (!menuItem.Active || spell.Level < 0 && spell.IsReady()) return;

                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            return;
            var t = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null) * 2, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var targetBehind = ObjectManager.Player.Position + Vector3.Normalize(ObjectManager.Player.Position - t.ServerPosition) * (Orbwalking.GetRealAutoAttackRange(null) - ObjectManager.Player.Distance(t.Position));
                if (ObjectManager.Player.Distance(targetBehind) > ObjectManager.Player.BoundingRadius)
                {
                    Orbwalker.SetOrbwalkingPoint(targetBehind);
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, targetBehind);
                }
                Render.Circle.DrawCircle(t.Position, 85f, Color.DarkRed);
                Render.Circle.DrawCircle(targetBehind, 85f, Color.DarkRed);
            }
            return;
            var heropos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            Drawing.DrawText(heropos.X, heropos.Y, Color.GreenYellow, "[AL]: " + xAttackLeft);



        }

        public static bool Intersection(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        {
            var p3 = new Vector2(pC.X + radius, pC.Y + radius);

            var m = ((p2.Y - p1.Y) / (p2.X - p1.X));
            var constant = (m * p1.X) - p1.Y;
            var b = -(2f * ((m * constant) + p3.X + (m * p3.Y)));
            var a = (1 + (m * m));
            var c = ((p3.X * p3.X) + (p3.Y * p3.Y) - (radius * radius) + (2f * constant * p3.Y) + (constant * constant));
            var d = ((b * b) - (4f * a * c));

            return d > 0;
        }

        public void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe) return;
            if (spell.SData.Name.Contains("summoner")) return;
            if (!Config.Item("Passive" + Id).GetValue<bool>()) return;

            //if (spell.Slot == SpellSlot.E || spell.Slot == SpellSlot.W || spell.Slot == SpellSlot.E || spell.Slot == SpellSlot.R)
            if (spell.SData.Name.ToLower().Contains("lucianq") || spell.SData.Name.ToLower().Contains("lucianw") ||
                spell.SData.Name.ToLower().Contains("luciane") || spell.SData.Name.ToLower().Contains("lucianr"))
            {
                xAttackLeft = 1;
                xPassiveUsedTime = Game.Time;
            }

            if (spell.SData.Name.ToLower().Contains("lucianpassiveattack"))
            {
                Utility.DelayAction.Add(500, () => { xAttackLeft -= 1; });
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                xAttackLeft = 0;
                return;
            }

            if (Game.Time > xPassiveUsedTime + 3 && xAttackLeft == 1)
            {
                xAttackLeft = 0;
            }

            if (Config.Item("Passive" + Id).GetValue<bool>() && xAttackLeft > 0)
            {
                return;
            }
            
            Obj_AI_Hero t;

            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active && ToggleActive)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                    return;

                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Q.CastOnUnit(t);
            }
            

            if (Q.IsReady() && GetValue<KeyBind>("UseQExtendedTH").Active && ToggleActive)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                    return;

                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && QMinion(t).IsValidTarget())
                {
                    if (ObjectManager.Player.Distance(t) > Q.Range)
                        Q.CastOnUnit(QMinion(t));
                }
            }

            
            if ((!ComboActive && !HarassActive)) return;
            var useQExtended = GetValue<StringList>("UseQExtendedC").SelectedIndex;
            if (useQExtended != 0)
            {
                switch (useQExtended)
                {
                    case 1:
                    {
                        t = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
                        var tx = QMinion(t);
                        if (tx.IsValidTarget())
                        {
                            if (!Orbwalking.InAutoAttackRange(t))
                                Q.CastOnUnit(tx);
                        }
                        break;
                    }

                    case 2:
                    {
                        var enemy = HeroManager.Enemies.Find(e => e.IsValidTarget(Q2.Range) && !e.IsZombie);
                        if (enemy != null)
                        {
                            var tx = QMinion(enemy);
                            if (tx.IsValidTarget())
                            {
                                Q.CastOnUnit(tx);
                            }
                        }
                        break;
                    }
                }
            }

            // Auto turn off Ghostblade Item if Ultimate active
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level > 0)
            {
                Config.Item("GHOSTBLADE")
                    .SetValue(ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LucianR");
            }

            //if (useQExtended && Q.IsReady())
            //{
            //    var t = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
            //    if (t.IsValidTarget() && QMinion.IsValidTarget())
            //    {
            //        if (!Orbwalking.InAutoAttackRange(t))
            //            Q.CastOnUnit(QMinion);
            //    }
            //}

            t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
            {
                return;
            }

            var useQ = GetValue<bool>("UseQC");
            if (useQ && Q.IsReady())
            {
                if (t.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(t);
                }
            }

            var useW = GetValue<bool>("UseWC");
            if (useW && W.IsReady())
            {
                if (t.IsValidTarget(W.Range))
                {
                    W.Cast(t);
                }
            }

            var useE = GetValue<StringList>("UseEC").SelectedIndex;
            if (useE != 0 && E.IsReady())
            {
                if (t.IsValidTarget(Q.Range))
                {
                    E.Cast(Game.CursorPos);
                }
            }
        }

        public override void ExecuteLaneClear()
        {
            int laneQValue = GetValue<StringList>("Lane.UseQ").SelectedIndex;
            if (laneQValue != 0)
            {
                var minion = Q.GetLineCollisionMinions(laneQValue);
                if (minion != null)
                {
                    Q.CastOnUnit(minion);
                }
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                minion = allMinions.FirstOrDefault(minionn => minionn.Distance(ObjectManager.Player.Position) <= Q.Range && HealthPrediction.LaneClearHealthPrediction(minionn, (int)Q.Delay * 2) > 0);
                if (minion != null)
                {
                    Q.CastOnUnit(minion);
                }
            }

            int laneWValue = GetValue<StringList>("Lane.UseW").SelectedIndex;
            if (laneWValue != 0 && E.IsReady())
            {
                Vector2 minions = W.GetLineFarmMinions(laneWValue);
                if (minions != Vector2.Zero)
                {
                    W.Cast(minions);
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            var jungleQValue = GetValue<StringList>("Jungle.UseQ").SelectedIndex;
            if (jungleQValue != 0 && Q.IsReady())
            {
                var bigMobsQ = Utils.Utils.GetMobs(Q.Range, jungleQValue == 2 ? Utils.Utils.MobTypes.BigBoys : Utils.Utils.MobTypes.All);
                if (bigMobsQ != null && bigMobsQ.Health > ObjectManager.Player.TotalAttackDamage * 2)
                {
                    Q.CastOnUnit(bigMobsQ);
                }
            }

            var jungleWValue = GetValue<StringList>("Jungle.UseQ").SelectedIndex;
            if (jungleWValue != 0 && W.IsReady())
            {
                var bigMobsQ = Utils.Utils.GetMobs(W.Range, jungleWValue == 2 ? Utils.Utils.MobTypes.BigBoys : Utils.Utils.MobTypes.All);
                if (bigMobsQ != null && bigMobsQ.Health > ObjectManager.Player.TotalAttackDamage * 2)
                {
                    W.Cast(bigMobsQ);
                }
            }

            var jungleEValue = GetValue<StringList>("Jungle.UseE").SelectedIndex;
            if (jungleEValue != 0 && E.IsReady())
            {
                var jungleMobs =
                    Marksman.Utils.Utils.GetMobs(Q.Range + Orbwalking.GetRealAutoAttackRange(null) + 65,
                        Marksman.Utils.Utils.MobTypes.All);

                if (jungleMobs != null)
                {
                    switch (GetValue<StringList>("Jungle.UseE").SelectedIndex)
                    {
                        case 1:
                            {
                                if (!jungleMobs.SkinName.ToLower().Contains("baron") ||
                                    !jungleMobs.SkinName.ToLower().Contains("dragon"))
                                {
                                    if (jungleMobs.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                                        E.Cast(
                                            jungleMobs.IsValidTarget(
                                                Orbwalking.GetRealAutoAttackRange(null) + 65)
                                                ? Game.CursorPos
                                                : jungleMobs.Position);
                                }
                                break;
                            }

                        case 2:
                            {
                                if (!jungleMobs.SkinName.ToLower().Contains("baron") ||
                                    !jungleMobs.SkinName.ToLower().Contains("dragon"))
                                {
                                    jungleMobs =
                                        Marksman.Utils.Utils.GetMobs(
                                            E.Range + Orbwalking.GetRealAutoAttackRange(null) + 65,
                                            Marksman.Utils.Utils.MobTypes.BigBoys);
                                    if (jungleMobs != null)
                                    {
                                        E.Cast(
                                            jungleMobs.IsValidTarget(
                                                Orbwalking.GetRealAutoAttackRange(null) + 65)
                                                ? Game.CursorPos
                                                : jungleMobs.Position);
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }

        private static float GetRTotalDamage(Obj_AI_Hero t)
        {
            var baseAttackSpeed = 0.638;
            var wCdTime = 3;
            var passiveDamage = 0;

            var attackSpeed = (float)Math.Round(Math.Floor(1 / ObjectManager.Player.AttackDelay * 100) / 100, 2, MidpointRounding.ToEven);

            var RLevel = new[] { 7.5, 9, 10.5 };
            var shoots = 7.5 + RLevel[R.Level - 1];
            var shoots2 = shoots * attackSpeed;

            var aDmg = Math.Round(Math.Floor(ObjectManager.Player.GetAutoAttackDamage(t) * 100) / 100, 2, MidpointRounding.ToEven);
            aDmg = Math.Floor(aDmg);

            var totalAttackSpeedWithWActive = (float)Math.Round((attackSpeed + baseAttackSpeed / 100) * 100 / 100, 2, MidpointRounding.ToEven);

            var totalPossibleDamage = (float)Math.Round((totalAttackSpeedWithWActive * wCdTime * aDmg) * 100 / 100, 2, MidpointRounding.ToEven);

            return totalPossibleDamage + (float)passiveDamage;
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Q:").SetValue(true));
            config.AddItem(new MenuItem("UseQExtendedC" + Id, "Q Extended:").SetValue(new StringList(new[] { "Off", "Use for Selected Target", "Use for Any Target" }, 1)));
            config.AddItem(new MenuItem("UseWC" + Id, "W:").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "E:").SetValue(new StringList(new []{ "Off", "On", "On: Protect AA Range" }, 2)));
            //config.AddItem(new MenuItem("UseRC" + Id, "E:").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            config.AddItem(new MenuItem("UseQExtendedTH" + Id, "Use Ext. Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("Passive" + Id, "Check Passive").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.Gray)));
            config.AddItem(new MenuItem("DrawQ2" + Id, "Ext. Q range").SetValue(new Circle(true, Color.Gray)));
            config.AddItem(new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, Color.Gray)));
            config.AddItem(new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, Color.Gray)));
            config.AddItem(new MenuItem("DrawR" + Id, "R range").SetValue(new Circle(false, Color.Chocolate)));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
            config.AddItem(dmgAfterComboItem);
            
            //Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };
            
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

            config.AddItem(new MenuItem("Lane.UseQ" + Id, "Q:").SetValue(new StringList(strQ, 3))).SetFontStyle(FontStyle.Regular, Q.MenuColor());
            config.AddItem(new MenuItem("Lane.UseQ2" + Id, "Q Extended:").SetValue(new StringList(new[] { "Off", "Out of AA Range" }, 1))).SetFontStyle(FontStyle.Regular, Q.MenuColor());

            string[] strW = new string[5];
            strW[0] = "Off";

            for (var i = 1; i < 5; i++)
            {
                strW[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("Lane.UseW" + Id, "W:").SetValue(new StringList(strW, 3))).SetFontStyle(FontStyle.Regular, W.MenuColor());

            config.AddItem(new MenuItem("Lane.UseE" + Id, "E:").SetValue(new StringList(new[] { "Off", "Under Ally Turrent Farm", "Out of AA Range", "Both" }, 1))).SetFontStyle(FontStyle.Regular, E.MenuColor());


            string[] strR = new string[4];
            strR[0] = "Off";

            for (var i = 1; i < 4; i++)
            {
                strR[i] = "Minion Count >= Ulti Attack Count x " + i.ToString();
            }
            config.AddItem(new MenuItem("Lane.UseR" + Id, "R:").SetValue(new StringList(strR, 2))).SetFontStyle(FontStyle.Regular, R.MenuColor());


            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("Jungle.UseQ" + Id, "Q:").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 2)));
            config.AddItem(new MenuItem("Jungle.UseW" + Id, "W:").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 2)));
            config.AddItem(new MenuItem("Jungle.UseE" + Id, "E:").SetValue(new StringList(new[] { "Off", "On", "Just big Monsters" }, 2)));

            return true;
        }

        public override void PermaActive()
        {
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            var enemy = HeroManager.Enemies.Find(e => e.IsValidTarget(E.Range + (Q.IsReady() ? Q.Range : Orbwalking.GetRealAutoAttackRange(null) + 65)) && !e.IsZombie);
            if (enemy != null)
            {
                if (enemy.Health < ObjectManager.Player.TotalAttackDamage*2)
                {
                    if (enemy.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                    {
                        if (!Q.IsReady())
                        {
                            if (W.IsReady() && GetValue<bool>("UseWC"))
                                W.Cast();
                        }
                    }
                    else
                    {
                        if (E.IsReady() && GetValue<StringList>("UseEC").SelectedIndex != 0)
                            E.Cast(enemy.Position);
                    }
                }

                var xPossibleComboDamage = 0f;
                xPossibleComboDamage += Q.IsReady() ? Q.GetDamage(enemy) + ObjectManager.Player.TotalAttackDamage * 2 : 0;
                xPossibleComboDamage += E.IsReady() ? ObjectManager.Player.TotalAttackDamage * 2 : 0;

                if (enemy.Health < xPossibleComboDamage)
                {
//                    if (enemy.Distance(ObjectManager.Player) > Orbwalking.GetRealAutoAttackRange(null) + 65))
                }

                if (E.IsReady() && Q.IsReady() && GetValue<StringList>("UseEC").SelectedIndex != 0)
                {
                    E.Cast(enemy.Position);
                }
            }
        }
    }
}
