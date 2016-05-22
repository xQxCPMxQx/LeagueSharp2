#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Utils.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library;
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.Helpers
{
    public static class Utils
    {
        private static readonly List<string> BigMinionList;

        static Utils()
        {
            BigMinionList = new List<string>
            {
                "SRU_Blue1.1.1",
                "SRU_Blue7.1.1",
                "SRU_Blue7.1.1",
                "SRU_Red4.1.1",
                "SRU_Red10.1.1",
                "SRU_Dragon6.1.1",
                "SRU_RiftHerald",
                "SRU_Baron12.1.1",
                "TT_Spiderboss8.1.1"
            };
        }

        public static bool IsNearTurret(this Obj_AI_Base target, float extraRange = 300f)
        {
            try
            {
                return GameObjects.Turrets.Any(turret => turret.IsValidTarget(900f + extraRange, true, target.Position));
            }

            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static bool IsUnderTurret(this Vector3 position, bool ally)
        {
            try
            {
                if (
                    GameObjects.Turrets.Any(
                        t =>
                            t.Health > 1 && !t.IsDead && (ally && t.IsAlly || !ally && t.IsEnemy) &&
                            position.Distance(t.Position) < 900f))
                {
                    return true;
                }
            }

            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static Vector2 PositionAfter(Obj_AI_Base unit, float t, float speed = float.MaxValue)
        {
            var distance = t * speed;
            var path = unit.GetWaypoints();

            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var d = a.Distance(b);

                if (d < distance)
                {
                    distance -= d;
                }
                else
                {
                    return a + distance * (b - a).Normalized();
                }
            }

            return path[path.Count - 1];
        }

        public static float SpellArrivalTime(Obj_AI_Base sender,
            Obj_AI_Base target,
            float delay,
            float speed,
            bool prediction = false)
        {
            try
            {
                var additional = sender.IsMe ? Game.Ping / 2000f + 0.1f : 0f;
                if (prediction && target is Obj_AI_Hero && target.IsMoving)
                {
                    var predTarget = Prediction.GetPrediction(
                        target,
                        delay + sender.ServerPosition.Distance(target.ServerPosition) * 1.1f / speed + additional);
                    return delay + (sender.ServerPosition.Distance(predTarget.UnitPosition) * 1.1f / speed + additional);
                }
                return delay + (sender.ServerPosition.Distance(target.ServerPosition) / speed + additional);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        public static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, double aperture)
        {
            try
            {
                var halfAperture = aperture / 2;
                var apexToXVector = apexPoint - position;
                var axisVector = apexPoint - circleCenter;
                var isInInfiniteCone = DotProd(apexToXVector, axisVector) / Magn(apexToXVector) / Magn(axisVector) >
                                       Math.Cos(halfAperture);
                return isInInfiniteCone && DotProd(apexToXVector, axisVector) / Magn(axisVector) < Magn(axisVector);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private static float DotProd(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float Magn(Vector2 a)
        {
            return (float) Math.Sqrt(a.X * a.X + a.Y * a.Y);
        }

        public static bool UnderAllyTurret(Vector3 position)
        {
            return
                GameObjects.AllyTurrets.Any(t => t.IsValid && !t.IsDead && t.Health > 1 && t.Distance(position) < 925f);
        }

        public static bool UnderEnemyTurret(Vector3 position)
        {
            return
                GameObjects.EnemyTurrets.Any(t => t.IsValid && !t.IsDead && t.Health > 1 && t.Distance(position) < 925f);
        }

        public static bool IsImmobile(Obj_AI_Base t)
        {
            return t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Charm) || t.HasBuffOfType(BuffType.Snare) ||
                   t.HasBuffOfType(BuffType.Knockup) || t.HasBuffOfType(BuffType.Polymorph) ||
                   t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Taunt) || t.IsStunned;
        }

        public static bool IsSlowed(Obj_AI_Base t)
        {
            return t.HasBuffOfType(BuffType.Slow);
        }

        public static float GetImmobileTime(Obj_AI_Base target)
        {
            var buffs =
                target.Buffs.Where(
                    t =>
                        t.Type == BuffType.Charm || t.Type == BuffType.Snare || t.Type == BuffType.Knockback ||
                        t.Type == BuffType.Polymorph || t.Type == BuffType.Fear || t.Type == BuffType.Taunt ||
                        t.Type == BuffType.Stun).ToList();
            if (buffs.Any())
            {
                return buffs.Max(t => t.EndTime) - Game.Time;
            }
            return 0f;
        }

        public static bool IsFacing(this Obj_AI_Base source, Vector3 position, float angle = 90)
        {
            if (source == null || position.Equals(Vector3.Zero))
            {
                return false;
            }
            return source.Direction.To2D().Perpendicular().AngleBetween((position - source.Position).To2D()) < angle;
        }

        public static bool ShouldDraw(bool checkScreen = false)
        {
            return !ObjectManager.Player.IsDead && !MenuGUI.IsShopOpen &&
                   (!checkScreen || ObjectManager.Player.Position.IsOnScreen());
        }

        public static Vector3 GetDashPosition(Spell spell, Obj_AI_Hero target, float safetyDistance)
        {
            var distance = target.Distance(ObjectManager.Player);
            var dashPoints = new Geometry.Polygon.Circle(ObjectManager.Player.Position, spell.Range).Points;
            if (distance < safetyDistance)
            {
                dashPoints.AddRange(
                    new Geometry.Polygon.Circle(ObjectManager.Player.Position, safetyDistance - distance).Points);
            }
            dashPoints = dashPoints.Where(p => !p.IsWall()).OrderBy(p => p.Distance(Game.CursorPos)).ToList();
            foreach (var point in dashPoints)
            {
                var allies =
                    GameObjects.AllyHeroes.Where(
                        hero => !hero.IsDead && hero.Distance(point.To3D()) < ObjectManager.Player.AttackRange).ToList();
                var enemies =
                    GameObjects.EnemyHeroes.Where(
                        hero => hero.IsValidTarget(ObjectManager.Player.AttackRange, true, point.To3D())).ToList();
                var lowEnemies = enemies.Where(hero => hero.HealthPercent <= 15).ToList();

                if (!point.To3D().IsUnderTurret(false))
                {
                    if (enemies.Count == 1 &&
                        (!target.IsMelee || target.HealthPercent <= ObjectManager.Player.HealthPercent - 25 ||
                         target.Position.Distance(point.To3D()) >= safetyDistance) ||
                        allies.Count >
                        enemies.Count -
                        (ObjectManager.Player.HealthPercent >= 10 * lowEnemies.Count ? lowEnemies.Count : 0))
                    {
                        return point.To3D();
                    }
                }
                else
                {
                    if (enemies.Count == 1 && lowEnemies.Any(t => t.NetworkId.Equals(target.NetworkId)))
                    {
                        return point.To3D();
                    }
                }
            }

            return Vector3.Zero;
        }

        public static void UpdateVisibleTags(Menu menu, int tag)
        {
            foreach (var menuItem in menu.Items)
            {
                if (menuItem.Tag != 0)
                {
                    menuItem.Show(false);
                }

                if (menuItem.Tag == tag)
                {
                    menuItem.Show();
                }
            }
        }

        public static void UpdateVisibleTag(Menu menu, int tag, bool value)
        {
            foreach (var menuItem in menu.Items)
            {
                if (menuItem.Tag == tag)
                {
                    menuItem.Show(value);
                }
            }
        }

        public static bool IsWallBetween(Vector3 start, Vector3 end, int step = 3)
        {
            if (start.IsValid() && end.IsValid() && step > 0)
            {
                var distance = start.Distance(end);
                for (var i = 0; i < distance; i = i + step)
                {
                    if (NavMesh.GetCollisionFlags(start.Extend(end, i)) == CollisionFlags.Wall)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsBigJungle(Obj_AI_Base minion)
        {
            return minion != null && minion.IsValid && minion.Team == GameObjectTeam.Neutral &&
                   BigMinionList.Any(b => minion.Name.Contains(b, StringComparison.OrdinalIgnoreCase));
        }
    }
}