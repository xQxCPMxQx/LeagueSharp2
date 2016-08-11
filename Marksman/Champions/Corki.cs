#region

using System;
using System.Collections.Generic;
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
    internal class Corki : Champion
    {
        public Spell Q, W, E;
        public Spell R1, R2;

        public Corki()
        {
            Utils.Utils.PrintMessage("Corki loaded");

            Q = new Spell(SpellSlot.Q, 825f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 600f, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 700f);
            R1 = new Spell(SpellSlot.R, 1300f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            R2 = new Spell(SpellSlot.R, 1500f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.VeryHigh };

            Q.SetSkillshot(0.35f, 240f, 1300f, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.35f, 140f, 1500f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0f, 45 * (float)Math.PI / 180, 1500, false, SkillshotType.SkillshotCone);

            R1.SetSkillshot(0.2f, 40f, 2000f, true, SkillshotType.SkillshotLine);
            R2.SetSkillshot(0.2f, 40f, 2000f, true, SkillshotType.SkillshotLine);
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Circle drawKillableMinions = GetValue<Circle>("Lane.UseQ.DrawKM");
            if (drawKillableMinions.Active && Q.IsReady() && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                foreach (Obj_AI_Base m in
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range)
                        .Where(x => E.CanCast(x) && x.Health < Q.GetDamage(x)))
                {
                    Render.Circle.DrawCircle(m.Position, (float)(m.BoundingRadius * 2), drawKillableMinions.Color, 5);
                }
            }
            //if (JungleClearActive || LaneClearActive)
            //{
            //    IEnumerable<Obj_AI_Base> list = ObjectManager.Get<Obj_AI_Minion>().Where(w => w.IsValidTarget(Q.Range));
            //    IEnumerable<Obj_AI_Base> mobs;
            //    if (JungleClearActive)
            //    {
            //        mobs = list.Where(w => w.Team == GameObjectTeam.Neutral);
            //        if (GetValue<StringList>("Jungle.UseQ").SelectedIndex == 1)
            //        {

            //            IEnumerable<Obj_AI_Base> oMob = (from fMobs in mobs
            //                from fBigBoys in
            //                    new[]
            //                    {
            //                        "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red",
            //                        "SRU_Krug", "SRU_Dragon", "SRU_Baron", "Sru_Crab"
            //                    }
            //                where fBigBoys == fMobs.SkinName
            //                select fMobs).AsEnumerable();


            //            mobs = oMob;
            //        }
            //    }
            //    else
            //    {
            //        mobs = list;
            //    }


            //    var objAiBases = mobs as IList<Obj_AI_Base> ?? mobs.ToList();
            //    List<Obj_AI_Base> m1 = objAiBases.ToList();

            //    foreach (var m in objAiBases)
            //    {
            //        var locLine = W.GetLineFarmLocation(m1);
            //        if (locLine.MinionsHit >= 3 && W.IsInRange(locLine.Position.To3D()))
            //        {
            //            Render.Circle.DrawCircle(m.Position, 105f, Color.Red);
            //            W.Cast(locLine.Position);
            //            return;
            //        }

            //        var locCircular = Q.GetCircularFarmLocation(m1, Q.Width);
            //        if (locCircular.MinionsHit >= 3 && Q.IsInRange(locCircular.Position.To3D()))
            //        {
            //            Render.Circle.DrawCircle(locCircular.Position.To3D(), 105f, Color.Red);
            //            Q.Cast(locCircular.Position);
            //            return;
            //        }
            //    }
            //}
            //return;

            Spell[] spellList = { Q, E };
            foreach (Spell spell in spellList)
            {
                Circle menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            var drawR = GetValue<Circle>("DrawR1");
            if (drawR.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R1.Range, drawR.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (R1.IsReady() && GetValue<bool>("UseRM"))
            {
                bool bigRocket = HasBigRocket();
                foreach (
                    Obj_AI_Hero hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(bigRocket ? R2.Range : R1.Range) &&
                                    R1.GetDamage(hero) * (bigRocket ? 1.5f : 1f) > hero.Health))
                {
                    if (bigRocket)
                    {
                        R2.Cast(hero, false, true);
                    }
                    else
                    {
                        R1.Cast(hero, false, true);
                    }
                }
            }

            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100)) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));
            var rLim = GetValue<Slider>("Rlim" + (ComboActive ? "C" : "H")).Value;

            if (useQ && Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    if (Q.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                        return;
            }

            if (useE && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                    if (E.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                        return;
            }

            if (useR && R1.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo > rLim)
            {
                bool bigRocket = HasBigRocket();
                Obj_AI_Hero t = TargetSelector.GetTarget(bigRocket ? R2.Range : R1.Range, TargetSelector.DamageType.Magical);

                if (t.IsValidTarget())
                {
                    if (bigRocket)
                    {
                        R2.Cast(t, false, true);
                    }
                    else
                    {
                        R1.Cast(t, false, true);
                    }
                }
            }
        }

        public override void ExecuteLaneClear()
        {
            int laneQValue = GetValue<StringList>("Lane.UseQ").SelectedIndex;
            if (laneQValue != 0 && Q.IsReady())
            {
                Vector2 minions = Q.GetCircularFarmMinions(laneQValue);
                if (minions != Vector2.Zero)
                {
                    Q.Cast(minions);
                }
            }

            int laneEValue = GetValue<StringList>("Lane.UseE").SelectedIndex;
            if (laneEValue != 0 && E.IsReady())
            {
                int minCount = E.GetMinionCountsInRange();
                if (minCount >= laneEValue)
                {
                    E.Cast();
                }
            }

            int laneRValue = GetValue<StringList>("Lane.UseR").SelectedIndex;
            if (laneRValue != 0 && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo >= GetValue<Slider>("Lane.UseR.Lim").Value)
            {
                int rocketType = GetValue<StringList>("Lane.UseR.Bomb").SelectedIndex;
                if (R1.IsReady() && (rocketType == 0 || rocketType == 2) && !HasBigRocket())
                {
                    Vector2 minions = R1.GetCircularFarmMinions(laneRValue);
                    if (minions != Vector2.Zero)
                    {
                        R1.Cast(minions);
                    }
                }
                if (R2.IsReady() && (rocketType == 1 || rocketType == 2) && HasBigRocket())
                {
                    Vector2 minions = R2.GetCircularFarmMinions(laneRValue);
                    if (minions != Vector2.Zero)
                    {
                        R2.Cast(minions);
                    }
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            int jungleQValue = GetValue<StringList>("Jungle.UseQ").SelectedIndex;
            if (jungleQValue != 0 && W.IsReady())
            {
                Obj_AI_Base jungleMobs = Utils.Utils.GetMobs(Q.Range,
                    jungleQValue != 1 ? Utils.Utils.MobTypes.All : Utils.Utils.MobTypes.BigBoys,
                    jungleQValue != 1 ? jungleQValue : 1);
                if (jungleMobs != null)
                {
                    Q.Cast(jungleMobs);
                }
            }

            int jungleEValue = GetValue<StringList>("Jungle.UseE").SelectedIndex;
            if (W.IsReady() && jungleEValue != 0)
            {
                Obj_AI_Base jungleMobs = Utils.Utils.GetMobs(E.Range,
                    jungleEValue != 1 ? Utils.Utils.MobTypes.All : Utils.Utils.MobTypes.BigBoys,
                    jungleEValue != 1 ? jungleEValue : 1);

                if (jungleMobs != null)
                {
                    E.Cast();
                }
            }

            int jungleRValue = GetValue<StringList>("Jungle.UseR").SelectedIndex;
            if (jungleRValue != 0 && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo > GetValue<Slider>("Jungle.UseR.Lim").Value)
            {
                Obj_AI_Base jungleMobs = Utils.Utils.GetMobs(R1.Range, jungleRValue != 1 ? Utils.Utils.MobTypes.All : Utils.Utils.MobTypes.BigBoys, jungleRValue != 1 ? jungleRValue : 1);
                if (jungleMobs != null)
                {
                    var rocketType = GetValue<StringList>("Jungle.UseR.Bomb").SelectedIndex;
                    if (R1.IsReady() && (rocketType == 0 || rocketType == 1) && !HasBigRocket())
                    {
                        R1.Cast(jungleMobs);
                    }

                    if (R2.IsReady() && (rocketType == 1 || rocketType == 2) && HasBigRocket())
                    {
                        R2.Cast(jungleMobs);
                    }
                }
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t == null || (!ComboActive && !HarassActive) || !unit.IsMe)
                return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useE = GetValue<bool>("UseE" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));
            var rLim = GetValue<Slider>("Rlim" + (ComboActive ? "C" : "H")).Value;

            if (useQ && Q.IsReady())
                if (Q.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                    return;

            if (useE && E.IsReady())
                if (E.Cast(t, false, true) == Spell.CastStates.SuccessfullyCasted)
                    return;

            if (useR && R1.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Ammo > rLim)
            {
                if (HasBigRocket())
                {
                    R2.Cast(t, false, true);
                }
                else
                {
                    R1.Cast(t, false, true);
                }
            }
        }

        public bool HasBigRocket()
        {
            return ObjectManager.Player.Buffs.Any(buff => buff.DisplayName.ToLower() == "corkimissilebarragecounterbig");
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RlimC" + Id, "Keep R Stacks").SetValue(new Slider(0, 0, 7)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEH" + Id, "Use E").SetValue(false));
            config.AddItem(new MenuItem("UseRH" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("RlimH" + Id, "Keep R Stacks").SetValue(new Slider(3, 0, 7)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, System.Drawing.Color.Aqua, 1)));
            config.AddItem(new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, System.Drawing.Color.Wheat, 1)));
            config.AddItem(new MenuItem("DrawR1" + Id, "R range").SetValue(new Circle(false, System.Drawing.Color.DarkOrange, 1)));
            config.AddItem(new MenuItem("Draw.Packet" + Id, "Show Turbo-Packet Remaining Time").SetValue(new StringList(new[] { "Off", "Everytime", "Show 20 secs Left" }, 2)));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("ShowPosition" + Id, "Show Position").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("UseRM" + Id, "Use R To Killsteal").SetValue(true));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            string[] strQ = new string[4];
            {
                strQ[0] = "Off";
                for (var i = 1; i < 4; i++)
                {
                    strQ[i] = "Mobs Count >= " + i;
                }
                config.AddItem(new MenuItem("Lane.UseQ" + Id, "Q: Use").SetValue(new StringList(strQ, 2))).SetFontStyle(FontStyle.Regular, Q.MenuColor());
            }

            config.AddItem(new MenuItem("Lane.UseQ.Prepare" + Id, "Q: Prepare Minions for multi farm").SetValue(new StringList(new[] { "Off", "On", "Just Under Ally Turret" }, 2))).SetFontStyle(FontStyle.Regular, Q.MenuColor());
            config.AddItem(new MenuItem("Lane.UseQ.DrawKM" + Id, "Q: Draw Killable Minions").SetValue(new Circle(true, Color.Wheat, 85f))).SetFontStyle(FontStyle.Regular, Q.MenuColor());


            string[] strW = new string[6];
            {
                strW[0] = "Off";
                for (var i = 1; i < 6; i++)
                {
                    strW[i] = "Mobs Count >= " + i;
                }
                //TODO: Add W Lane Clear for Corki
                //                config.AddItem(new MenuItem("Lane.UseW" + Id, "W: Use").SetValue(new StringList(strW, 2))).SetFontStyle(FontStyle.Regular, E.MenuColor());
                //                config.AddItem(new MenuItem("Lane.UseW.Mode" + Id, "W: Mode").SetValue(new StringList(new []{"Just Under Ally Turret", "If I'm Alone/in Safe", "Use Everytime"}))).SetFontStyle(FontStyle.Regular, E.MenuColor());
            }

            string[] strE = new string[7];
            {
                strE[0] = "Off";
                for (var i = 1; i < 7; i++)
                {
                    strE[i] = "Mobs Count >= " + i;
                }

                config.AddItem(new MenuItem("Lane.UseE" + Id, "E: Use").SetValue(new StringList(strE, 2))).SetFontStyle(FontStyle.Regular, E.MenuColor());
            }

            string[] strR = new string[4];
            {
                strR[0] = "Off";
                for (var i = 1; i < 4; i++)
                {
                    strR[i] = "Minion Count >= " + i;
                }

                config.AddItem(new MenuItem("Lane.UseR" + Id, "R:").SetValue(new StringList(strR, 3))).SetFontStyle(FontStyle.Regular, R1.MenuColor());
                config.AddItem(new MenuItem("Lane.UseR.Lim" + Id, "R: Keep Stacks").SetValue(new Slider(0, 0, 7))).SetFontStyle(FontStyle.Regular, R1.MenuColor());
                config.AddItem(new MenuItem("Lane.UseR.Bomb" + Id, "R: Rocket Type").SetValue(new StringList(new[] { "Small-Rocked", "Big-Rocked", "Both" }, 0))).SetFontStyle(FontStyle.Regular, R1.MenuColor());
            }
            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            string[] strQ = new string[4];
            {
                strQ[0] = "Off";
                strQ[1] = "Just for big Monsters";

                for (var i = 2; i < 4; i++)
                {
                    strQ[i] = "Mobs Count >= " + i;
                }

                config.AddItem(new MenuItem("Jungle.UseQ" + Id, "Q: Use").SetValue(new StringList(strQ, 1))).SetFontStyle(FontStyle.Regular, Q.MenuColor());
            }

            string[] strE = new string[4];
            {
                strE[0] = "Off";
                strE[1] = "Just for big Monsters";

                for (var i = 2; i < 4; i++)
                {
                    strE[i] = "Mobs Count >= " + i;
                }

                config.AddItem(new MenuItem("Jungle.UseE" + Id, "E: Use").SetValue(new StringList(strE, 1))).SetFontStyle(FontStyle.Regular, E.MenuColor());
                //config.AddItem(new MenuItem("Jungle.UseE.Lock" + Id, "E: Lock Position").SetValue(new StringList(new[] { "Off", "On", "On: if enemy so far" }, 1))).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
            }

            string[] strR = new string[4];
            {
                strR[0] = "Off";
                strR[1] = "Just big Monsters";
                for (var i = 2; i < 4; i++)
                {
                    strR[i] = "Mob Count >= " + i;
                }

                config.AddItem(new MenuItem("Jungle.UseR" + Id, "R:").SetValue(new StringList(strR, 3))).SetFontStyle(FontStyle.Regular, R1.MenuColor());
                config.AddItem(new MenuItem("Jungle.UseR.Lim" + Id, "R: Keep Stacks").SetValue(new Slider(0, 0, 7))).SetFontStyle(FontStyle.Regular, R1.MenuColor());
                config.AddItem(new MenuItem("Jungle.UseR.Bomb" + Id, "R: Rocked Type").SetValue(new StringList(new[] { "Small-Rocked", "Big-Rocked", "Both" }, 0))).SetFontStyle(FontStyle.Regular, R1.MenuColor());
            }

            return true;
        }
    }
}
