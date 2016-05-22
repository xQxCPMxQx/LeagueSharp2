#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 CPrediction.cs is part of SFXChallenger.

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
using Spell = SFXChallenger.Wrappers.Spell;

#endregion

namespace SFXChallenger.Helpers
{
    internal class CPrediction
    {
        private static float _boundingRadiusMultiplicator = 0.65f;

        public static float BoundingRadiusMultiplicator
        {
            get { return _boundingRadiusMultiplicator; }
            set { _boundingRadiusMultiplicator = value; }
        }

        public static Result Circle(Spell spell,
            Obj_AI_Hero target,
            HitChance hitChance,
            bool boundingRadius = true,
            bool extended = true)
        {
            try
            {
                if (spell == null || target == null)
                {
                    return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
                }
                var hits = new List<Obj_AI_Hero>();
                var center = Vector3.Zero;
                var radius = float.MaxValue;
                var range = spell.Range + (extended ? spell.Width * 0.85f : 0) +
                            (boundingRadius ? target.BoundingRadius * BoundingRadiusMultiplicator : 0);
                var positions = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(range * 1.5f, true, spell.RangeCheckFrom)
                    let prediction = spell.GetPrediction(t)
                    where prediction.Hitchance >= hitChance
                    select new Position(t, prediction.UnitPosition)).ToList();
                var spellWidth = spell.Width;
                if (positions.Any())
                {
                    var mainTarget = positions.FirstOrDefault(p => p.Hero.NetworkId == target.NetworkId);
                    var possibilities =
                        ListExtensions.ProduceEnumeration(
                            positions.Where(
                                p => p.UnitPosition.Distance(mainTarget.UnitPosition) <= spell.Width * 0.85f).ToList())
                            .Where(p => p.Count > 0 && p.Any(t => t.Hero.NetworkId == mainTarget.Hero.NetworkId))
                            .ToList();
                    foreach (var possibility in possibilities)
                    {
                        var mec = MEC.GetMec(possibility.Select(p => p.UnitPosition.To2D()).ToList());
                        var distance = spell.From.Distance(mec.Center.To3D());
                        if (mec.Radius < spellWidth && distance < range)
                        {
                            var lHits = new List<Obj_AI_Hero>();
                            var circle =
                                new Geometry.Polygon.Circle(
                                    spell.From.Extend(
                                        mec.Center.To3D(), spell.Range > distance ? distance : spell.Range), spell.Width);

                            if (boundingRadius)
                            {
                                lHits.AddRange(
                                    from position in positions
                                    where
                                        new Geometry.Polygon.Circle(
                                            position.UnitPosition,
                                            position.Hero.BoundingRadius * BoundingRadiusMultiplicator).Points.Any(
                                                p => circle.IsInside(p))
                                    select position.Hero);
                            }
                            else
                            {
                                lHits.AddRange(
                                    from position in positions
                                    where circle.IsInside(position.UnitPosition)
                                    select position.Hero);
                            }

                            if ((lHits.Count > hits.Count || lHits.Count == hits.Count && mec.Radius < radius ||
                                 lHits.Count == hits.Count &&
                                 spell.From.Distance(circle.Center.To3D()) < spell.From.Distance(center)) &&
                                lHits.Any(p => p.NetworkId == target.NetworkId))
                            {
                                center = circle.Center.To3D2();
                                radius = mec.Radius;
                                hits.Clear();
                                hits.AddRange(lHits);
                            }
                        }
                    }
                    if (!center.Equals(Vector3.Zero))
                    {
                        return new Result(center, hits);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
        }

        public static Result Line(Spell spell,
            Obj_AI_Hero target,
            HitChance hitChance,
            bool boundingRadius = true,
            bool maxRange = true)
        {
            try
            {
                if (spell == null || target == null)
                {
                    return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
                }
                var range = (spell.IsChargedSpell && maxRange ? spell.ChargedMaxRange : spell.Range) +
                            spell.Width * 0.9f +
                            (boundingRadius ? target.BoundingRadius * BoundingRadiusMultiplicator : 0);
                var positions = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(range, true, spell.RangeCheckFrom)
                    let prediction = spell.GetPrediction(t)
                    where prediction.Hitchance >= hitChance
                    select new Position(t, prediction.UnitPosition)).ToList();
                if (positions.Any())
                {
                    var hits = new List<Obj_AI_Hero>();
                    var pred = spell.GetPrediction(target);
                    if (pred.Hitchance >= hitChance)
                    {
                        hits.Add(target);
                        var rect = new Geometry.Polygon.Rectangle(
                            spell.From, spell.From.Extend(pred.CastPosition, range), spell.Width);
                        if (boundingRadius)
                        {
                            hits.AddRange(
                                from point in positions.Where(p => p.Hero.NetworkId != target.NetworkId)
                                let circle =
                                    new Geometry.Polygon.Circle(
                                        point.UnitPosition, point.Hero.BoundingRadius * BoundingRadiusMultiplicator)
                                where circle.Points.Any(p => rect.IsInside(p))
                                select point.Hero);
                        }
                        else
                        {
                            hits.AddRange(
                                from position in positions
                                where rect.IsInside(position.UnitPosition)
                                select position.Hero);
                        }
                        return new Result(pred.CastPosition, hits);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
        }

        internal struct Position
        {
            public readonly Obj_AI_Hero Hero;
            public readonly Obj_AI_Base Base;
            public readonly Vector3 UnitPosition;

            public Position(Obj_AI_Hero hero, Vector3 unitPosition)
            {
                Hero = hero;
                Base = null;
                UnitPosition = unitPosition;
            }

            public Position(Obj_AI_Base unit, Vector3 unitPosition)
            {
                Base = unit;
                Hero = null;
                UnitPosition = unitPosition;
            }
        }

        internal struct BasePosition
        {
            public readonly Obj_AI_Base Unit;
            public readonly Vector3 UnitPosition;

            public BasePosition(Obj_AI_Base unit, Vector3 unitPosition)
            {
                Unit = unit;
                UnitPosition = unitPosition;
            }
        }

        internal struct Result
        {
            public readonly Vector3 CastPosition;
            public readonly List<Obj_AI_Hero> Hits;
            public readonly int TotalHits;

            public Result(Vector3 castPosition, List<Obj_AI_Hero> hits)
            {
                CastPosition = castPosition;
                Hits = hits;
                TotalHits = hits.Count;
            }
        }
    }
}