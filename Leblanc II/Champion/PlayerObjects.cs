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
        private static Spell W => PlayerSpells.W;
        private static Spell E => PlayerSpells.E;

        private static bool DrawWObject => Modes.ModeDraw.MenuLocal.Item(Modes.ModeDraw.GetPcModeStringValue + "Draw.W.BuffTime").GetValue<StringList>().SelectedIndex == 1;
        private static bool DrawRObject => Modes.ModeDraw.MenuLocal.Item(Modes.ModeDraw.GetPcModeStringValue + "Draw.R.BuffTime").GetValue<StringList>().SelectedIndex == 1;
        public static void Init()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += DrawingOnOnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void UpdateBeamStatus(LeblancSoulShackleMType beamType, Obj_AI_Base t, BuffInstance buffInstance, float starTime, float EndTime)
        {
            if (!DrawRObject && !DrawRObject)
            {
                return;
            }

            if (DrawWObject)
            {
                if (beamType == LeblancSoulShackleMType.FromE)
                {
                    if (LeblancSoulShackle.EndTime < Game.Time || buffInstance.EndTime > LeblancSoulShackle.EndTime)
                    {
                        LeblancSoulShackle.Object = t;
                        LeblancSoulShackle.StartTime = buffInstance.StartTime;
                        LeblancSoulShackle.EndTime = (float) (buffInstance.EndTime + 0.3);
                    }
                }
            }

            if (DrawRObject)
            {
                if (beamType == LeblancSoulShackleMType.FromR)
                {
                    if (LeblancSoulShackleM.EndTime < Game.Time || buffInstance.EndTime > LeblancSoulShackleM.EndTime)
                    {
                        LeblancSoulShackleM.Object = t;
                        LeblancSoulShackleM.StartTime = buffInstance.StartTime;
                        LeblancSoulShackleM.EndTime = (float) (buffInstance.EndTime + 0.3);
                    }
                }
            }
        }


        private static void GameOnOnUpdate(EventArgs args)
        {
            if (!DrawRObject && !DrawRObject)
            {
                return;
            }

            foreach (var eObjects in ObjectManager.Get<Obj_AI_Base>().Where(e => e.IsEnemy && !e.IsDead && e.IsValidTarget(1500)))
            {
                BuffInstance beam = null;
                if (DrawWObject)
                {

                    beam = eObjects.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeam", StringComparison.InvariantCultureIgnoreCase));
                    if (beam != null)
                    {
                        UpdateBeamStatus(LeblancSoulShackleMType.FromE, eObjects, beam, beam.StartTime, beam.EndTime);
                    }
                }

                if (DrawRObject)
                { 
                    beam = eObjects.Buffs.Find(buff => buff.DisplayName.Equals("Leblancshacklebeamm", StringComparison.InvariantCultureIgnoreCase));
                    if (beam != null)
                    {
                        UpdateBeamStatus(LeblancSoulShackleMType.FromR, eObjects, beam, beam.StartTime, beam.EndTime);
                    }
                }
            }
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            if (!Modes.ModeDraw.MenuLocal.Item("Draw.Enable").GetValue<bool>())
            {
                return;
            }

            if (!DrawRObject && !DrawRObject)
            {
                return;
            }

            foreach (var eObjects in ObjectManager.Get<Obj_AI_Base>().Where(e => e.IsEnemy && !e.IsDead && e.IsValidTarget(1500)))
            {
                if (DrawWObject)
                { 
                    if (LeblancSoulShackle.EndTime >= Game.Time && eObjects.NetworkId == LeblancSoulShackle.Object.NetworkId)
                    {
                        var circle = new Geometry.Circle2(LeblancSoulShackle.Object.Position.To2D(), (LeblancSoulShackle.Object.BoundingRadius * 2) - 20, Game.Time * 300 - LeblancSoulShackle.StartTime * 300, LeblancSoulShackle.EndTime * 300 - LeblancSoulShackle.StartTime * 300).ToPolygon();
                        circle.Draw(Color.GreenYellow, 5);
                    }
                }

                if (DrawWObject)
                {
                    if (LeblancSoulShackleM.EndTime >= Game.Time && eObjects.NetworkId == LeblancSoulShackleM.Object.NetworkId)
                    {
                        var circle = new Geometry.Circle2(LeblancSoulShackleM.Object.Position.To2D(), LeblancSoulShackleM.Object.BoundingRadius * 2, Game.Time * 300 - LeblancSoulShackleM.StartTime * 300, LeblancSoulShackleM.EndTime * 300 - LeblancSoulShackleM.StartTime * 300).ToPolygon();
                        circle.Draw(Color.DarkRed, 5);
                    }
                }
            }

            if (DrawWObject)
            {
                var wSlide = ExistingSlide.FirstOrDefault(s => s.Type == 0 && s.EndTime >= Game.Time);
                if (wSlide != null)
                {
                    DrawArrow(wSlide);
                }
            }

            if (DrawRObject)
            {
                var rSlide = ExistingSlide.FirstOrDefault(s => s.Type == 1 && s.EndTime >= Game.Time);
                if (rSlide != null)
                {
                    DrawArrow(rSlide);
                }
            }
        }

        static void DrawArrow(Slide slide)
        {
            if (!DrawRObject && !DrawRObject)
            {
                return;
            }

            var color = slide.Type == 1 ? Color.DarkRed : Color.DeepPink;
            var width = 4;
            
            var circle = new Geometry.Circle2(slide.Position.To2D(), 150f, Game.Time * 300 - slide.StartTime * 300, slide.EndTime * 300 - slide.StartTime * 300).ToPolygon();
            circle.Draw(color, width);

            var startpos = ObjectManager.Player.Position;
            var endpos = slide.Position;
            if (startpos.Distance(endpos) > 100)
            {
                var endpos1 = slide.Position + (startpos - endpos).To2D().Normalized().Rotated(25 * (float)Math.PI / 180).To3D() * 75;
                var endpos2 = slide.Position + (startpos - endpos).To2D().Normalized().Rotated(-25 * (float)Math.PI / 180).To3D() * 75;

                var x1 = new LeagueSharp.Common.Geometry.Polygon.Line(startpos, endpos);
                x1.Draw(color, width - 2);
                var y1 = new LeagueSharp.Common.Geometry.Polygon.Line(endpos, endpos1);
                y1.Draw(color, width - 2);
                var z1 = new LeagueSharp.Common.Geometry.Polygon.Line(endpos, endpos2);
                z1.Draw(color, width - 2);
            }
        }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (
                sender.Name.Equals("Leblanc_base_w_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase) ||
                sender.Name.Equals("Leblanc_base_rw_return_indicator.troy", StringComparison.InvariantCultureIgnoreCase))
            {
               // Console.WriteLine(sender.Name);
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
            if (!DrawRObject && !DrawRObject)
            {
                return;
            }

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