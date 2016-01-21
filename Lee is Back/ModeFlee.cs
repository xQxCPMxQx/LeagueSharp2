using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace LeeSin
{
    internal static class ModeFlee
    {
        public static Spell Q => Program.Q;
        public static Spell W => Program.W;

        public static Program.QCastStage QState => Program.QStage;
        public static Program.WCastStage WStage => Program.WStage;

        public static Menu LocalMenu => Program.MenuFlee;

        private static Geometry.Polygon toPolygon;

        private static Obj_AI_Base wardJumpObjectforFlee = null;
        private static float wardRange = 625;
        private static bool canJumpWithW;
        private static bool jumpingWithQ;

        public static void Initialize()
        {
            LocalMenu.AddItem(new MenuItem("Flee.UseQ", "Q:")).SetValue(true);
            LocalMenu.AddItem(new MenuItem("Flee.UseW", "W:")).SetValue(true);
            LocalMenu.AddItem(new MenuItem("Flee.Range", "Object Search Range")).SetValue(new Slider(250, 100, 350));
            LocalMenu.AddItem(new MenuItem("Flee.Draw", "Draw Object Search Range (Cursor)"))
                .SetValue(new Circle(true, Color.Aqua));

            Game.OnUpdate += args =>
            {
                if (wardJumpObjectforFlee != null &&
                    (wardJumpObjectforFlee.Distance(ObjectManager.Player.Position) > W.Range*4 ||
                     wardJumpObjectforFlee.IsDead))
                {
                    wardJumpObjectforFlee = null;
                }


                if (Program.MenuKeys.Item("Flee.Active.Ward").GetValue<KeyBind>().Active)
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    jumpingWithQ = false;
                    ExecuteW();
                }

                if (Program.MenuKeys.Item("Flee.Active.QW").GetValue<KeyBind>().Active)
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                    ExecuteW();
                    if (!canJumpWithW)
                        ExecuteQ();
                }
            };
            Game.OnWndProc += OnWndProc_Flee;
            Drawing.OnDraw += Drawing_OnDraw;
        }


        private static void OnWndProc_Flee(WndEventArgs args)
        {
            if (args.Msg != (uint) WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            wardJumpObjectforFlee =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(
                        hero =>
                            hero.Distance(Game.CursorPos, true) < W.Range*4 && hero.IsAlly && !hero.IsMe && !hero.IsDead &&
                            !(hero.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0))
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //if (WStage != Program.WCastStage.IsReady)
            //{
            //    return;
            //}

            if (!Program.MenuKeys.Item("Flee.Active").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (LocalMenu.Item("Flee.Draw").GetValue<Circle>().Active)
            {
                var fleeDraw = LocalMenu.Item("Flee.Draw").GetValue<Circle>();
                Render.Circle.DrawCircle(Game.CursorPos, LocalMenu.Item("Flee.Range").GetValue<Slider>().Value,
                    fleeDraw.Color);
            }


            if (wardJumpObjectforFlee != null)
            {
                Render.Circle.DrawCircle(wardJumpObjectforFlee.Position, 105f, Color.Aquamarine);

                var x = new LeagueSharp.Common.Geometry.Polygon.Line(ObjectManager.Player.Position,
                    wardJumpObjectforFlee.Position);
                x.Draw(Color.Blue, 2);

                Vector3[] vCent = new[] {ObjectManager.Player.Position, wardJumpObjectforFlee.Position};
                var aX =
                    Drawing.WorldToScreen(new Vector3(DrawUtils.CenterOfVectors(vCent).X,
                        DrawUtils.CenterOfVectors(vCent).Y,
                        DrawUtils.CenterOfVectors(vCent).Z));
                Drawing.DrawText(aX.X, aX.Y, Color.White, "Jump to Selected Object");
            }
        }

        public static void ExecuteQ()
        {
            if (!LocalMenu.Item("Flee.UseQ").GetValue<bool>())
            {
                return;
            }

            if (QState == Program.QCastStage.IsReady)
            {
                foreach (
                    var minions in
                        ObjectManager.Get<Obj_AI_Base>()
                            .Where(
                                o =>
                                    !o.IsAlly && !o.IsDead && o.Health > Q.GetDamage(o, 0) + Q.GetDamage(o, 1) &&
                                    o.IsValidTarget(Q.Range) &&
                                    o.Distance(Game.CursorPos) < ObjectManager.Player.Distance(o.Position) &&
                                    o.Distance(ObjectManager.Player.Position) > Orbwalking.GetRealAutoAttackRange(null) + 200)
                            .OrderByDescending(o => o.Distance(ObjectManager.Player.Position)))
                {
                    Q.Cast(minions);
                }
            }

            if (QState == Program.QCastStage.IsCasted)
            {
                jumpingWithQ = true;
                var minion = ObjectManager.Get<Obj_AI_Base>()
                    .Find(
                        o =>
                            o.IsEnemy && !o.IsDead && o.HasBlindMonkBuff() &&
                            o.Distance(ObjectManager.Player.Position) < Q.Range + 400 && o.IsValidTarget(Q.Range + 400));
                if (minion != null)
                {
                    Q.Cast();
                    return;
                }
            }
            jumpingWithQ = false;
        }

        public static void ExecuteW()
        {
            if (!LocalMenu.Item("Flee.UseW").GetValue<bool>())
            {
                return;
            }


            var pos = Game.CursorPos;
            if (jumpingWithQ)
            {
                return;
            }
            if (WStage != Program.WCastStage.IsReady)
            {
                canJumpWithW = false;
                return;
            }

            toPolygon =
                new Geometry.Rectangle(ObjectManager.Player.Position.To2D(),
                    ObjectManager.Player.Position.To2D()
                        .Extend(pos.To2D(),
                            ObjectManager.Player.Position.Distance(pos) < W.Range
                                ? W.Range
                                : +ObjectManager.Player.Position.Distance(pos)),
                    LocalMenu.Item("Flee.Range").GetValue<Slider>().Value).ToPolygon();


            if (wardJumpObjectforFlee != null && WStage == Program.WCastStage.IsReady)
            {
                Render.Circle.DrawCircle(wardJumpObjectforFlee.Position, 85f, Color.Coral);
                canJumpWithW = true;
                W.CastOnUnit(wardJumpObjectforFlee);

                return;
            }

            var jObjects = ObjectManager.Get<Obj_AI_Base>()
                .OrderByDescending(obj => obj.Distance(ObjectManager.Player.ServerPosition))
                .FirstOrDefault(obj => obj.IsAlly && !obj.IsMe && !obj.IsDead &&
                                       !(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                                       obj.Distance(ObjectManager.Player.Position) <= W.Range &&
                                       obj.Distance(ObjectManager.Player.Position) >
                                       Orbwalking.GetRealAutoAttackRange(null) + 100 &&
                                       toPolygon.IsInside(obj.Position));

            if (jObjects != null)
            {
                Render.Circle.DrawCircle(jObjects.Position, 85f, Color.Coral);
                Program.WardJump(jObjects.Position);
                canJumpWithW = true;
                return;
            }

            if (Items.GetWardSlot() != null && Items.GetWardSlot().Stacks > 0)
            {
                canJumpWithW = true;
                Program.PutWard(ObjectManager.Player.Position.Extend(pos, wardRange));
                return;
            }
            canJumpWithW = false;
        }
    }
}