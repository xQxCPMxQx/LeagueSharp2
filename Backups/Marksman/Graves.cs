#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using ClipperLib;
using System.Collections.Generic;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
#endregion

namespace Marksman
{
    public class Polygon
    {
        public List<Vector2> Points = new List<Vector2>();

        public void Add(Vector2 point)
        {
            Points.Add(point);
        }

        public Path ToClipperPath()
        {
            var result = new Path(Points.Count);

            foreach (var point in Points)
            {
                result.Add(new IntPoint(point.X, point.Y));
            }

            return result;
        }

        public bool IsOutside(Vector2 point)
        {
            var p = new IntPoint(point.X, point.Y);
            return Clipper.PointInPolygon(p, ToClipperPath()) != 1;
        }

        public void Draw(System.Drawing.Color color, int width = 1)
        {
            for (var i = 0; i <= Points.Count - 1; i++)
            {
                var nextIndex = (Points.Count - 1 == i) ? 0 : (i + 1);
                DrawLineInWorld(Points[i].To3D(), Points[nextIndex].To3D(), width, color);
            }
        }
        public static void DrawLineInWorld(Vector3 start, Vector3 end, int width, System.Drawing.Color color)
        {
            var from = Drawing.WorldToScreen(start);
            var to = Drawing.WorldToScreen(end);
            Drawing.DrawLine(from[0], from[1], to[0], to[1], width, color);
            //Drawing.DrawLine(from.X, from.Y, to.X, to.Y, width, color);
        }
    }
    public class Rectangle
    {
        public Vector2 Direction;
        public Vector2 Perpendicular;
        public Vector2 REnd;
        public Vector2 RStart;
        public float Width;

        public Rectangle(Vector2 start, Vector2 end, float width)
        {
            RStart = start;
            REnd = end;
            Width = width;
            Direction = (end - start).Normalized();
            Perpendicular = Direction.Perpendicular();
        }

        public Polygon ToPolygon(int offset = 0, float overrideWidth = -1)
        {
            var result = new Polygon();

            result.Add(
                RStart + (overrideWidth > 0 ? overrideWidth : Width + offset) * Perpendicular - offset * Direction);
            result.Add(
                RStart - (overrideWidth > 0 ? overrideWidth : Width + offset) * Perpendicular - offset * Direction);
            result.Add(
                REnd - (overrideWidth > 0 ? overrideWidth : Width + offset) * Perpendicular + offset * Direction);
            result.Add(
                REnd + (overrideWidth > 0 ? overrideWidth : Width + offset) * Perpendicular + offset * Direction);

            return result;
        }
    }
    internal class Graves : Champion
    {
        public Spell Q;
        public Spell R;
        public Spell W;

        public Graves()
        {
            Utils.PrintMessage("Graves loaded.");

            Q = new Spell(SpellSlot.Q, 900f); // Q likes to shoot a bit too far away, so moving the range inward.
            Q.SetSkillshot(0.25f, 15f * 1.5f * (float)Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);

            W = new Spell(SpellSlot.W, 1100f);
            W.SetSkillshot(0.25f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(0.25f, 100f, 2100f, true, SkillshotType.SkillshotLine);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;
        }

        private float GetComboDamage(Obj_AI_Hero t)
        {
            float fComboDamage = 0f;

            if (Q.IsReady())
                fComboDamage += (float)ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady())
                fComboDamage += (float)ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

            if (R.IsReady())
                fComboDamage += (float)ObjectManager.Player.GetSpellDamage(t, SpellSlot.R);

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


        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active)
            {
                if(ObjectManager.Player.HasBuff("Recall"))
                    return;
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Q.Cast(t, false, true);
            }
            
            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100)) return;
            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));

            if (Q.IsReady() && useQ)
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Q.Cast(t, false, true);
            }

            if (W.IsReady() && useW)
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    W.Cast(t, false, true);
            }

            if (R.IsReady() && useR)
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(R.Range) &&
                                    ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R, 1) - 20 > hero.Health))
                    R.Cast(hero, false, true);
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                    Q.Cast(t);

                if (W.IsReady() && useW)
                    W.Cast(t);
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            var t = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var xWidth = 95;

                new Rectangle(ObjectManager.Player.ServerPosition.To2D(), t.ServerPosition.To2D(), 75).ToPolygon();
                //Drawing.DrawLine(Drawing.WorldToScreen(new Vector3(ObjectManager.Player.Position.X, ObjectManager.Player.Position.Y - xWidth / 2, ObjectManager.Player.Position.Z)), Drawing.WorldToScreen(new Vector3(t.Position.X, t.Position.Y - xWidth / 2, t.Position.Z)), 1, System.Drawing.Color.Crimson);
                //Drawing.DrawLine(Drawing.WorldToScreen(new Vector3(ObjectManager.Player.Position.X, ObjectManager.Player.Position.Y + xWidth / 2, ObjectManager.Player.Position.Z)), Drawing.WorldToScreen(new Vector3(t.Position.X, t.Position.Y + xWidth / 2, t.Position.Z)), 1, System.Drawing.Color.Crimson);
            }

            Spell[] spellList = { Q };
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
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle)));           
            
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    System.Drawing.Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {

            return true;
        }
        public override bool LaneClearMenu(Menu config)
        {

             return true;
        }

   
    }
}
