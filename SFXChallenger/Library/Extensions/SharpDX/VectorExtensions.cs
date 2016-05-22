#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 VectorExtensions.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace SFXChallenger.Library.Extensions.SharpDX
{
    public static class VectorExtensions
    {
        public static bool IsOnScreen(this Vector3 position, float radius)
        {
            var pos = Drawing.WorldToScreen(position);
            return !(pos.X + radius < 0) && !(pos.X - radius > Drawing.Width) && !(pos.Y + radius < 0) &&
                   !(pos.Y - radius > Drawing.Height);
        }

        public static bool IsOnScreen(this Vector2 position, float radius)
        {
            return position.To3D().IsOnScreen(radius);
        }

        public static bool IsOnScreen(this Vector2 start, Vector2 end)
        {
            if (start.X > 0 && start.X < Drawing.Width && start.Y > 0 && start.Y < Drawing.Height && end.X > 0 &&
                end.X < Drawing.Width && end.Y > 0 && end.Y < Drawing.Height)
            {
                return true;
            }
            if (start.Intersection(end, new Vector2(0, 0), new Vector2(Drawing.Width, 0)).Intersects)
            {
                return true;
            }
            if (start.Intersection(end, new Vector2(0, 0), new Vector2(0, Drawing.Height)).Intersects)
            {
                return true;
            }
            if (
                start.Intersection(end, new Vector2(0, Drawing.Height), new Vector2(Drawing.Width, Drawing.Height))
                    .Intersects)
            {
                return true;
            }
            return
                start.Intersection(end, new Vector2(Drawing.Width, 0), new Vector2(Drawing.Width, Drawing.Height))
                    .Intersects;
        }

        public static Vector2 FindNearestLineCircleIntersections(this Vector2 start,
            Vector2 end,
            Vector2 circlePos,
            float radius)
        {
            float t;
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;

            var a = dx * dx + dy * dy;
            var b = 2 * (dx * (start.X - circlePos.X) + dy * (start.Y - circlePos.Y));
            var c = (start.X - circlePos.X) * (start.X - circlePos.X) +
                    (start.Y - circlePos.Y) * (start.Y - circlePos.Y) - radius * radius;

            var det = b * b - 4 * a * c;
            if ((a <= 0.0000001) || (det < 0))
            {
                return Vector2.Zero;
            }
            if (det.Equals(0f))
            {
                t = -b / (2 * a);
                return new Vector2(start.X + t * dx, start.Y + t * dy);
            }

            t = (float) ((-b + Math.Sqrt(det)) / (2 * a));
            var intersection1 = new Vector2(start.X + t * dx, start.Y + t * dy);
            t = (float) ((-b - Math.Sqrt(det)) / (2 * a));
            var intersection2 = new Vector2(start.X + t * dx, start.Y + t * dy);

            return Vector2.Distance(intersection1, ObjectManager.Player.Position.To2D()) >
                   Vector2.Distance(intersection2, ObjectManager.Player.Position.To2D())
                ? intersection2
                : intersection1;
        }

        public static bool IsInsideCircle(this Vector2 point, Vector2 circlePos, float circleRad)
        {
            return Math.Sqrt(Math.Pow(circlePos.X - point.X, 2) + Math.Pow(circlePos.Y - point.Y, 2)) < circleRad;
        }

        public static bool IsIntersecting(this Vector2 lineStart, Vector2 lineEnd, Vector2 circlePos, float circleRadius)
        {
            return IsInsideCircle(lineStart, circlePos, circleRadius) ^ IsInsideCircle(lineEnd, circlePos, circleRadius);
        }

        public static Obj_AI_Minion GetNearestMinionByNames(this Vector3 position, string[] names)
        {
            var nearest = float.MaxValue;
            Obj_AI_Minion sMinion = null;
            foreach (var minion in
                GameObjects.Jungle.Where(
                    minion => names.Any(name => minion.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))))
            {
                var distance = Vector3.Distance(position, minion.ServerPosition);
                if (nearest > distance || nearest.Equals(float.MaxValue))
                {
                    nearest = distance;
                    sMinion = minion;
                }
            }
            return sMinion;
        }

        public static Obj_AI_Minion GetMinionFastByNames(this Vector3 position, float range, string[] names)
        {
            return GameObjects.Jungle.FirstOrDefault(m => names.Any(n => m.Name.Equals(n)) && m.IsValidTarget(range));
        }

        public static Obj_AI_Minion GetNearestMinionByNames(this Vector2 position, string[] names)
        {
            return GetNearestMinionByNames(position.To3D(), names);
        }
    }
}