﻿using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace JaxQx
{
    internal class Polygon
    {
        public List<Vector2> Points = new List<Vector2>();

        public Polygon()
        {
        }

        public Polygon(List<Vector2> p)
        {
            Points = p;
        }

        public void Add(Vector2 vec)
        {
            Points.Add(vec);
        }

        public int Count()
        {
            return Points.Count;
        }

        public Vector2 GetProjOnPolygon(Vector2 vec)
        {
            var closest = new Vector2(-1000, -1000);
            var start = Points[Count() - 1];
            foreach (var vecPol in Points)
            {
                var proj = ProjOnLine(start, vecPol, vec);
                closest = ClosestVec(proj, closest, vec);
                start = vecPol;
            }

            return closest;
        }

        public Vector2 ClosestVec(Vector2 vec1, Vector2 vec2, Vector2 to)
        {
            var dist1 = Vector2.DistanceSquared(vec1, to);
            var dist2 = Vector2.DistanceSquared(vec2, to);
            return (dist1 > dist2) ? vec2 : vec1;
        }

        public void Draw(Color color, int width = 1)
        {
            for (var i = 0; i <= Points.Count - 1; i++)
            {
                if (!(Points[i].Distance(Jumper.Player.Position) < 1500))
                {
                    continue;
                }

                var nextIndex = (Points.Count - 1 == i) ? 0 : (i + 1);
                var from = Drawing.WorldToScreen(Points[i].To3D());
                var to = Drawing.WorldToScreen(Points[nextIndex].To3D());
                Drawing.DrawLine(@from[0], @from[1], to[0], to[1], width, color);
            }
        }

        private static Vector2 ProjOnLine(Vector2 v, Vector2 w, Vector2 p)
        {
            var nullVec = new Vector2(-1, -1);
            var l2 = Vector2.DistanceSquared(v, w);
            if (l2 == 0.0)
                return nullVec;

            var t = Vector2.Dot(p - v, w - v)/l2;
            if (t < 0.0)
                return nullVec;

            if (t > 1.0)
                return nullVec;

            var projection = v + t*(w - v);
            return projection;
        }
    }
}