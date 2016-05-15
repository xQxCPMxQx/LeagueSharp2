using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Geometry = Leblanc.Common.CommonGeometry;

namespace Leblanc.Champion
{
    internal static class PlayerObjects
    {
        internal class Slide
        {
            public GameObject Object { get; set; }
            public float NetworkId { get; set; }
            public Vector3 Position { get; set; }
            public float StartTime { get; set; }
            public float EndTime { get; set; }
            public int Type { get; set; }
        }

        internal class LeblancSoulShackle
        {
            public static Obj_AI_Base Object { get; set; }
            public static float StartTime { get; set; }
            public static float EndTime { get; set; }

        }
        internal class LeblancSoulShackleM
        {
            public static Obj_AI_Base Object { get; set; }
            public static float StartTime { get; set; }
            public static float EndTime { get; set; }
        }

        enum LeblancSoulShackleMType
        {
            FromE,
            FromR

        }
        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        private static Spell W => Program.W;
        private static Spell E => Program.E;
        public static void Init()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += DrawingOnOnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void UpdateBeamStatus(LeblancSoulShackleMType beamType, Obj_AI_Base t, BuffInstance buffInstance, float starTime, float EndTime)
        {
            if (beamType == LeblancSoulShackleMType.FromE)
            {
                if (LeblancSoulShackle.EndTime < Game.Time || buffInstance.EndTime > LeblancSoulShackle.EndTime)
                {
                    LeblancSoulShackle.Object = t;
                    LeblancSoulShackle.StartTime = buffInstance.StartTime;
                    LeblancSoulShackle.EndTime = (float)(buffInstance.EndTime + 0.3);
                }
            }

            if (beamType == LeblancSoulShackleMType.FromR)
            {
                if (LeblancSoulShackleM.EndTime < Game.Time || buffInstance.EndTime > LeblancSoulShackleM.EndTime)
                {
                    LeblancSoulShackleM.Object = t;
                    LeblancSoulShackleM.StartTime = buffInstance.StartTime;
                    LeblancSoulShackleM.EndTime = (float)(buffInstance.EndTime + 0.3);
                }
            }
        }


        private static void GameOnOnUpdate(EventArgs args)
        {
            //foreach (var t1 in HeroManager.Enemies.Where(t1 => t1.IsValidTarget(E.Range * 2)))
            //{
            //    foreach (var b in t1.Buffs)
            //    {
            //        if (b.DisplayName.ToLower().Contains("Leblanc"))
            //        {
            //            Game.PrintChat(b.DisplayName);
            //        }
            //    }
            //}

            foreach (var eObjects in ObjectManager.Get<Obj_AI_Base>().Where(e => e.IsEnemy && !e.IsDead && e.IsValidTarget(1500)))
            {
                BuffInstance beam = null;
                beam = eObjects.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeam", StringComparison.InvariantCultureIgnoreCase));
                if (beam != null)
                {
                    UpdateBeamStatus(LeblancSoulShackleMType.FromE, eObjects, beam, beam.StartTime, beam.EndTime);
                }

                beam = eObjects.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeamm", StringComparison.InvariantCultureIgnoreCase));
                if (beam != null)
                {
                    UpdateBeamStatus(LeblancSoulShackleMType.FromR, eObjects, beam, beam.StartTime, beam.EndTime);
                    //if (LeblancSoulShackleM.EndTime < Game.Time || beam.EndTime > LeblancSoulShackle.EndTime)
                    //{
                    //    LeblancSoulShackleM.Object = eObjects;
                    //    LeblancSoulShackleM.StartTime = beam.StartTime;
                    //    LeblancSoulShackleM.EndTime = (float)(beam.EndTime + 0.3);
                    //}
                }
            }

            //var t = TargetSelector.GetTarget(E.Range * 2, TargetSelector.DamageType.Physical);
            //if (t.IsValidTarget())
            //{
            //    if (t.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeam", StringComparison.InvariantCultureIgnoreCase)) != null)
            //    {
            //        BuffInstance b = t.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeam", StringComparison.InvariantCultureIgnoreCase));
            //        if (LeblancSoulShackle.EndTime < Game.Time || b.EndTime > LeblancSoulShackle.EndTime)
            //        {
            //            LeblancSoulShackle.Object = t;
            //            LeblancSoulShackle.StartTime = b.StartTime;
            //            LeblancSoulShackle.EndTime = (float) (b.EndTime + 0.3);
            //        }
            //    }

            //    if (t.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeamm", StringComparison.InvariantCultureIgnoreCase)) != null)
            //    {
            //        BuffInstance b = t.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeamm", StringComparison.InvariantCultureIgnoreCase));
            //        if (LeblancSoulShackleM.EndTime < Game.Time || b.EndTime > LeblancSoulShackleM.EndTime)
            //        {
            //            LeblancSoulShackleM.Object = t;
            //            LeblancSoulShackleM.StartTime = b.StartTime;
            //            LeblancSoulShackleM.EndTime = (float)(b.EndTime + 0.3);
            //        }
            //    }
            //}
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {

            if (Program.Config.Item("Objects.EStunStatus").GetValue<bool>())
            {
                foreach (var e in ObjectManager.Get<Obj_AI_Base>())
                {
                    if (e.Buffs.Find(buff =>buff.DisplayName.Equals("Leblancshacklebeam",StringComparison.InvariantCultureIgnoreCase)) != null)
                    {
                        BuffInstance b =e.Buffs.Find(buff =>buff.DisplayName.Equals("Leblancshacklebeam",StringComparison.InvariantCultureIgnoreCase));
                        if (b != null)
                        {
                            var circle1 =
                                new Geometry.Circle2(new Vector2(e.Position.X + 3, e.Position.Y - 3), e.BoundingRadius*2,
                                    Game.Time*300 - b.StartTime*300, b.EndTime*300 - b.StartTime*300).ToPolygon();
                            circle1.Draw(Color.Black, 3);


                            var circle =
                                new Geometry.Circle2(e.Position.To2D(), e.BoundingRadius*2,
                                    Game.Time*300 - b.StartTime*300, b.EndTime*300 - b.StartTime*300).ToPolygon();
                            circle.Draw(Color.DarkRed, 3);

                        }
                    }

                    if (e.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeamm", StringComparison.InvariantCultureIgnoreCase)) != null)
                    {
                        BuffInstance b = e.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeamm", StringComparison.InvariantCultureIgnoreCase));
                        if (b != null)
                        {
                            var circle1 =
                                new Geometry.Circle2(new Vector2(e.Position.X + 3, e.Position.Y - 3), e.BoundingRadius * 2,
                                    Game.Time * 300 - b.StartTime * 300, b.EndTime * 300 - b.StartTime * 300).ToPolygon();
                            circle1.Draw(Color.Black, 3);


                            var circle =
                                new Geometry.Circle2(e.Position.To2D(), e.BoundingRadius * 2,
                                    Game.Time * 300 - b.StartTime * 300, b.EndTime * 300 - b.StartTime * 300).ToPolygon();
                            circle.Draw(Color.DarkRed, 3);

                        }
                    }
                }
            }

            //foreach (var eObjects in ObjectManager.Get<Obj_AI_Base>().Where(e => e.IsEnemy && !e.IsDead && e.IsValidTarget(1500)))
            //{
            //    if (LeblancSoulShackle.EndTime >= Game.Time && eObjects.NetworkId == LeblancSoulShackle.Object.NetworkId)
            //    {
            //        var circle1 = new Geometry.Circle2(new Vector2(LeblancSoulShackle.Object.Position.X + 3, LeblancSoulShackle.Object.Position.X - 3), 170f, Game.Time * 300 - LeblancSoulShackle.StartTime * 300, LeblancSoulShackle.EndTime * 300 - LeblancSoulShackle.StartTime * 300).ToPolygon();
            //        circle1.Draw(Color.Black, 5);

            //        var circle = new Geometry.Circle2(LeblancSoulShackle.Object.Position.To2D(), 170f, Game.Time * 300 - LeblancSoulShackle.StartTime * 300, LeblancSoulShackle.EndTime * 300 - LeblancSoulShackle.StartTime * 300).ToPolygon();
            //        circle.Draw(Color.GreenYellow, 5);
            //    }

            //    if (LeblancSoulShackleM.EndTime >= Game.Time && eObjects.NetworkId == LeblancSoulShackleM.Object.NetworkId)
            //    {
            //        var circle1 = new Geometry.Circle2(new Vector2(LeblancSoulShackleM.Object.Position.X + 3, LeblancSoulShackleM.Object.Position.X - 3), 170f, Game.Time * 300 - LeblancSoulShackleM.StartTime * 300, LeblancSoulShackleM.EndTime * 300 - LeblancSoulShackleM.StartTime * 300).ToPolygon();
            //        circle1.Draw(Color.Black, 5);

            //        var circle = new Geometry.Circle2(LeblancSoulShackleM.Object.Position.To2D(), 190f, Game.Time * 300 - LeblancSoulShackleM.StartTime * 300, LeblancSoulShackleM.EndTime * 300 - LeblancSoulShackleM.StartTime * 300).ToPolygon();
            //        circle.Draw(Color.DarkRed, 5);
            //    }
            //}

            if (Program.Config.Item("Objects.WPosition").GetValue<bool>())
            {
                foreach (var x in ExistingSlide)
                {
                    if (x.EndTime >= Game.Time)
                    {
                        var color = x.Type == 1 ? Color.DarkRed : Color.DeepPink;
                        var width = 4;

                        var circle1 =
                            new Geometry.Circle2(new Vector2(x.Position.X + 3, x.Position.Y - 3), 150f,
                                Game.Time*300 - x.StartTime*300, x.EndTime*300 - x.StartTime*300).ToPolygon();
                        circle1.Draw(Color.Black, 4);

                        var circle =
                            new Geometry.Circle2(x.Position.To2D(), 150f, Game.Time*300 - x.StartTime*300,
                                x.EndTime*300 - x.StartTime*300).ToPolygon();
                        circle.Draw(color, width);

                        var startpos = ObjectManager.Player.Position;
                        var endpos = x.Position;
                        if (startpos.Distance(endpos) > 100)
                        {
                            var endpos1 = x.Position +
                                          (startpos - endpos).To2D().Normalized().Rotated(25*(float) Math.PI/180).To3D()*
                                          75;
                            var endpos2 = x.Position +
                                          (startpos - endpos).To2D()
                                              .Normalized()
                                              .Rotated(-25*(float) Math.PI/180)
                                              .To3D()*75;

                            var x1 = new LeagueSharp.Common.Geometry.Polygon.Line(startpos, endpos);
                            x1.Draw(color, width - 2);
                            var y1 = new LeagueSharp.Common.Geometry.Polygon.Line(endpos, endpos1);
                            y1.Draw(color, width - 2);
                            var z1 = new LeagueSharp.Common.Geometry.Polygon.Line(endpos, endpos2);
                            z1.Draw(color, width - 2);
                        }
                    }
                }
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (
                sender.Name.Equals("Leblanc_base_w_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase) ||
                sender.Name.Equals("Leblanc_base_rw_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine(sender.Name);
                ExistingSlide.Add(
                    new Slide
                    {
                        Object = sender,
                        NetworkId = sender.NetworkId,
                        Position = sender.Position,
                        StartTime = Game.Time,
                        EndTime = Game.Time + 4,
                        Type = sender.Name.Equals("Leblanc_base_rw_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase) ? 1 : 0
                    });
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (
                sender.Name.Equals("Leblanc_base_w_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase) ||
                sender.Name.Equals("Leblanc_base_rw_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase))
            {

                for (var i = 0; i < ExistingSlide.Count; i++)
                {
                    if (Math.Abs(ExistingSlide[i].NetworkId - sender.NetworkId) < 0.00001)
                    {
                        ExistingSlide.RemoveAt(i);
                        return;
                    }
                }
            }
        }
    }
}