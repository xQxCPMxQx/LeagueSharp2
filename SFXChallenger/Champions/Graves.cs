#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Graves.cs is part of SFXChallenger.

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
using SFXChallenger.Abstracts;
using SFXChallenger.Args;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SharpDX;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.SFXTargetSelector.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Graves : Champion
    {
        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.AfterAttack; }
        }

        public Spell R2 { get; private set; }

        protected override void OnLoad()
        {
            GapcloserManager.OnGapcloser += OnEnemyGapcloser;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.25f, 60f, 2000f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 900f, DamageType.Magical);
            W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 425f);

            R = new Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(0.25f, 110f, 2100f, false, SkillshotType.SkillshotLine);

            R2 = new Spell(SpellSlot.R, 700f);
            R2.SetSkillshot(0f, 110f, 1500f, false, SkillshotType.SkillshotCone);

            Ultimate = new UltimateManager
            {
                Combo = true,
                Assisted = true,
                Auto = true,
                Flash = false,
                Required = true,
                Force = true,
                Gapcloser = false,
                GapcloserDelay = false,
                Interrupt = false,
                InterruptDelay = false,
                Spells = Spells,
                DamageCalculation =
                    (hero, resMulti, rangeCheck) =>
                        CalcComboDamage(
                            hero, resMulti, rangeCheck, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>(), true)
            };
        }

        protected override void AddToMenu()
        {
            Ultimate.AddToMenu(Menu);

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.VeryHigh },
                    { "W", HitChance.VeryHigh },
                    { "R", HitChance.High }
                });
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-mode", "E Mode").SetValue(new StringList(new[] { "Auto", "Cursor" })));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-safety", "E Safety Distance").SetValue(new Slider(320, 0, 500)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    DefaultValue = 30
                });
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));

            var laneClearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ResourceManager.AddToMenu(
                laneClearMenu,
                new ResourceManagerArgs(
                    "lane-clear", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".q", "Use Q").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".q-min", "Q Min.").SetValue(new Slider(3, 1, 5)));

            var jungleClearMenu = Menu.AddSubMenu(new Menu("Jungle Clear", Menu.Name + ".jungle-clear"));
            ResourceManager.AddToMenu(
                jungleClearMenu,
                new ResourceManagerArgs(
                    "jungle-clear", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 30, 10, 10 }
                });
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".q", "Use Q").SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", "Use E").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", "Use Q").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));

            var wGapcloserMenu = miscMenu.AddSubMenu(new Menu("W Gapcloser", miscMenu.Name + "w-gapcloser"));
            GapcloserManager.AddToMenu(
                wGapcloserMenu,
                new HeroListManagerArgs("w-gapcloser")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false,
                    Enabled = false
                }, true);
            BestTargetOnlyManager.AddToMenu(wGapcloserMenu, "w-gapcloser");

            var eGapcloserMenu = miscMenu.AddSubMenu(new Menu("E Gapcloser", miscMenu.Name + "e-gapcloser"));
            GapcloserManager.AddToMenu(
                eGapcloserMenu,
                new HeroListManagerArgs("e-gapcloser")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                }, true);
            BestTargetOnlyManager.AddToMenu(eGapcloserMenu, "e-gapcloser");

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        protected override void OnPreUpdate()
        {
            Orbwalker.SetAttack(!IsReloading());
        }

        protected override void OnPostUpdate()
        {
            if (Ultimate.IsActive(UltimateModeType.Assisted) && R.IsReady())
            {
                if (Ultimate.ShouldMove(UltimateModeType.Assisted))
                {
                    Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                }

                if (!RLogic(UltimateModeType.Assisted, TargetSelector.GetTarget(R)))
                {
                    RLogicSingle(UltimateModeType.Assisted);
                }
            }

            if (Ultimate.IsActive(UltimateModeType.Auto) && R.IsReady())
            {
                if (!RLogic(UltimateModeType.Auto, TargetSelector.GetTarget(R)))
                {
                    RLogicSingle(UltimateModeType.Auto);
                }
            }
        }

        private void OnEnemyGapcloser(object sender, GapcloserManagerArgs args)
        {
            try
            {
                if (args.UniqueId.Equals("w-gapcloser") && W.IsReady() &&
                    BestTargetOnlyManager.Check("w-gapcloser", W, args.Hero))
                {
                    if (args.End.Distance(Player.Position) <= W.Range)
                    {
                        W.Cast(args.End);
                    }
                }
                if (args.UniqueId.Equals("e-gapcloser") && E.IsReady() &&
                    BestTargetOnlyManager.Check("e-gapcloser", E, args.Hero))
                {
                    E.Cast(args.End.Extend(Player.Position, args.End.Distance(Player.Position) + E.Range));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool IsReloading()
        {
            return !Player.HasBuff("gravesbasicattackammo1");
        }

        private int GetAmmoCount()
        {
            return Player.HasBuff("gravesbasicattackammo2") ? 2 : (!IsReloading() ? 1 : 0);
        }

        protected override void Combo()
        {
            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var useR = Ultimate.IsActive(UltimateModeType.Combo) && R.IsReady();

            if (useR)
            {
                var target = TargetSelector.GetTarget(R.Range, R.DamageType);
                if (target != null)
                {
                    if (!RLogic(UltimateModeType.Combo, target))
                    {
                        RLogicSingle(UltimateModeType.Combo);
                    }
                }
            }
            if (useE && !Player.IsWindingUp && !IsReloading())
            {
                var target = TargetSelector.GetTarget(
                    E.Range + Player.AttackRange + Player.BoundingRadius, E.DamageType);
                if (target != null)
                {
                    var pos = Menu.Item(Menu.Name + ".combo.e-mode").GetValue<StringList>().SelectedIndex == 0
                        ? Utils.GetDashPosition(
                            E, target, Menu.Item(Menu.Name + ".combo.e-safety").GetValue<Slider>().Value)
                        : Player.Position.Extend(
                            Game.CursorPos, Math.Min(E.Range, Player.Position.Distance(Game.CursorPos)));

                    if (!pos.Equals(Vector3.Zero))
                    {
                        if (GetAmmoCount() == 1 && !pos.IsUnderTurret(false) ||
                            (!GameObjects.EnemyHeroes.Any(e => e.IsValidTarget() && Orbwalking.InAutoAttackRange(e)) &&
                             GameObjects.EnemyHeroes.Any(
                                 e =>
                                     e.IsValidTarget() &&
                                     pos.Distance(e.Position) < Orbwalking.GetRealAutoAttackRange(e)) &&
                             target.Health < Player.GetAutoAttackDamage(target)*2))
                        {
                            E.Cast(pos);
                        }
                    }
                }
            }

            if (useQ)
            {
                QLogic(Q.GetHitChance("combo"));
            }

            if (useW)
            {
                var target = TargetSelector.GetTarget(W);
                if (target != null)
                {
                    var best = CPrediction.Circle(W, target, W.GetHitChance("combo"));
                    if (best.TotalHits > 0 && !best.CastPosition.Equals(Vector3.Zero))
                    {
                        W.Cast(best.CastPosition);
                    }
                }
            }
        }

        private void QLogic(HitChance hitChance)
        {
            var target = TargetSelector.GetTarget(Q);
            if (target != null)
            {
                var prediction = CPrediction.Line(Q, target, hitChance, false);
                if (prediction.TotalHits >= 1)
                {
                    var firstHit = prediction.Hits.OrderBy(h => h.Distance(Player)).FirstOrDefault();
                    if (firstHit != null && !Utils.IsWallBetween(Player.Position, firstHit.Position))
                    {
                        if (!GameObjects.EnemyHeroes.Any(e => e.IsValidTarget() && Orbwalking.InAutoAttackRange(e)) ||
                            IsReloading() || Q.IsKillable(target) || prediction.TotalHits >= 2)
                        {
                            Q.Cast(prediction.CastPosition);
                        }
                    }
                }
            }
        }

        private bool RLogic(UltimateModeType mode, Obj_AI_Hero target)
        {
            try
            {
                if (Ultimate.IsActive(mode))
                {
                    var hits = GetRHits(target);
                    if (Ultimate.Check(mode, hits.Item2) &&
                        (hits.Item2.Any(h => R.GetDamage(h) * 0.95f > h.Health) ||
                         hits.Item2.Any(h => h.Distance(Player) + 300 < Orbwalking.GetRealAutoAttackRange(h) * 0.9f)))
                    {
                        R.Cast(hits.Item3);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void RLogicSingle(UltimateModeType mode)
        {
            try
            {
                if (Ultimate.ShouldSingle(mode))
                {
                    foreach (var target in
                        GameObjects.EnemyHeroes.Where(
                            t =>
                                Ultimate.CheckSingle(mode, t) &&
                                (R.GetDamage(t) * 0.95f > t.Health ||
                                 t.Distance(Player) + 300 < Orbwalking.GetRealAutoAttackRange(t) * 0.8f)))
                    {
                        var hits = GetRHits(target);
                        if (hits.Item1 > 0)
                        {
                            R.Cast(hits.Item3);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private float CalcComboDamage(Obj_AI_Hero target, float resMulti, bool rangeCheck, bool q, bool r)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }

                var damage = 0f;
                var totalMana = 0f;
                var didR = false;

                if (r && R.IsReady() && (!rangeCheck || R.IsInRange(target, R.Range + R2.Range)))
                {
                    var rMana = R.ManaCost * resMulti;
                    if (totalMana + rMana <= Player.Mana)
                    {
                        totalMana += rMana;
                        damage += R.GetDamage(target);
                        didR = true;
                    }
                }
                if (q && Q.IsReady() && (!rangeCheck || target.Distance(Player) < Q.Range - (didR ? 200 : 0)))
                {
                    var qMana = Q.ManaCost * resMulti;
                    if (totalMana + qMana <= Player.Mana)
                    {
                        damage += Q.GetDamage(target);
                    }
                }
                if (!rangeCheck ||
                    target.Distance(Player) <= Orbwalking.GetRealAutoAttackRange(target) * (didR ? 0.65 : 0.85f))
                {
                    damage += 2 * (float) Player.GetAutoAttackDamage(target, true);
                }
                damage += ItemManager.CalculateComboDamage(target, rangeCheck);
                damage += SummonerManager.CalculateComboDamage(target, rangeCheck);
                return damage;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private Tuple<int, List<Obj_AI_Hero>, Vector3> GetRHits(Obj_AI_Hero target)
        {
            var hits = new List<Obj_AI_Hero>();
            var castPos = Vector3.Zero;
            try
            {
                var pred = R.GetPrediction(target);
                if (pred.Hitchance >= R.GetHitChance("combo"))
                {
                    castPos = pred.CastPosition;
                    hits.Add(target);
                    var pos = Player.Position.Extend(castPos, Math.Min(Player.Distance(pred.UnitPosition), R.Range));
                    var pos2 = Player.Position.Extend(pos, Player.Distance(pos) + R2.Range);

                    var input = new PredictionInput
                    {
                        Range = R2.Range,
                        Delay = Player.Position.Distance(pred.UnitPosition) / R.Speed + 0.1f,
                        From = pos,
                        RangeCheckFrom = pos,
                        Radius = R2.Width,
                        Type = SkillshotType.SkillshotLine,
                        Speed = R2.Speed
                    };

                    var rect = new Geometry.Polygon.Rectangle(pos, pos2, R2.Width);

                    foreach (var enemy in
                        GameObjects.EnemyHeroes.Where(e => e.IsValidTarget() && e.NetworkId != target.NetworkId))
                    {
                        input.Unit = enemy;
                        var pred2 = Prediction.GetPrediction(input);
                        if (!pred2.UnitPosition.Equals(Vector3.Zero))
                        {
                            if (
                                new Geometry.Polygon.Circle(enemy.Position, enemy.BoundingRadius).Points.Any(
                                    p => rect.IsInside(p)))
                            {
                                hits.Add(enemy);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<int, List<Obj_AI_Hero>, Vector3>(hits.Count, hits, castPos);
        }

        protected override void Harass()
        {
            if (!ResourceManager.Check("harass"))
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                QLogic(Q.GetHitChance("harass"));
            }
        }

        protected override void LaneClear()
        {
            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady() &&
                       ResourceManager.Check("lane-clear");
            if (useQ)
            {
                QFarmLogic(
                    MinionManager.GetMinions(Q.Range),
                    Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value);
            }
        }

        protected override void JungleClear()
        {
            var useQ = Menu.Item(Menu.Name + ".jungle-clear.q").GetValue<bool>() && Q.IsReady() &&
                       ResourceManager.Check("jungle-clear");
            if (useQ)
            {
                QFarmLogic(
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth),
                    1);
            }
        }

        private void QFarmLogic(List<Obj_AI_Base> minions, int min)
        {
            try
            {
                if (!Q.IsReady() || minions.Count == 0)
                {
                    return;
                }
                var totalHits = 0;
                var castPos = Vector3.Zero;

                var positions = (from minion in minions
                    let pred = Q.GetPrediction(minion)
                    where pred.Hitchance >= HitChance.Medium
                    where !Utils.IsWallBetween(Player.Position, pred.UnitPosition)
                    select new Tuple<Obj_AI_Base, Vector3>(minion, pred.UnitPosition)).ToList();

                if (positions.Any())
                {
                    foreach (var position in positions)
                    {
                        var rect = new Geometry.Polygon.Rectangle(
                            ObjectManager.Player.Position, ObjectManager.Player.Position.Extend(position.Item2, Q.Range),
                            Q.Width);
                        var count =
                            positions.Select(
                                position2 =>
                                    new Geometry.Polygon.Circle(position2.Item2, position2.Item1.BoundingRadius * 0.9f))
                                .Count(circle => circle.Points.Any(p => rect.IsInside(p)));
                        if (count > totalHits)
                        {
                            totalHits = count;
                            castPos = position.Item2;
                        }
                        if (totalHits == minions.Count)
                        {
                            break;
                        }
                    }
                    if (!castPos.Equals(Vector3.Zero) && totalHits >= min)
                    {
                        Q.Cast(castPos);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
            {
                E.Cast(Player.Position.Extend(Game.CursorPos, E.Range));
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
            {
                var fPredEnemy =
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range * 1.2f) && Q.IsKillable(e))
                        .Select(enemy => Q.GetPrediction(enemy, true))
                        .FirstOrDefault(pred => pred.Hitchance >= HitChance.High);
                if (fPredEnemy != null && !Utils.IsWallBetween(Player.Position, fPredEnemy.CastPosition))
                {
                    Q.Cast(fPredEnemy.CastPosition);
                }
            }
        }
    }
}