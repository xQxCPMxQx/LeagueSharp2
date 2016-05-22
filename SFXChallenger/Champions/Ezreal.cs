#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ezreal.cs is part of SFXChallenger.

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
    internal class EzrealTesting : Champion
    {
        private int _lastFarmQKill;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.AfterAttack; }
        }

        protected override void OnLoad()
        {
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            GapcloserManager.OnGapcloser += OnEnemyGapcloser;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 475f);

            R = new Spell(SpellSlot.R, 1750f);
            R.SetSkillshot(1.2f, 160f, 2000f, false, SkillshotType.SkillshotLine);

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
                            hero, resMulti, rangeCheck, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>(),
                            Menu.Item(Menu.Name + ".combo.w").GetValue<bool>(), true)
            };
        }

        protected override void AddToMenu()
        {
            var ultimateMenu = Ultimate.AddToMenu(Menu);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".range", "Range").SetValue(new Slider((int) R.Range, 1000, 3000)))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { R.Range = args.GetNewValue<Slider>().Value; };

            R.Range = Menu.Item(ultimateMenu.Name + ".range").GetValue<Slider>().Value;

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.High },
                    { "W", HitChance.VeryHigh },
                    { "R", HitChance.High }
                });
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-mode", "E Mode").SetValue(new StringList(new[] { "Auto", "Cursor" })));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-safety", "E Safety Distance").SetValue(new Slider(300, 0, 500)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High }, { "W", HitChance.VeryHigh } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    DefaultValue = 30
                });
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", "Use W").SetValue(false));

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

            var jungleClearMenu = Menu.AddSubMenu(new Menu("Jungle Clear", Menu.Name + ".jungle-clear"));
            ResourceManager.AddToMenu(
                jungleClearMenu,
                new ResourceManagerArgs(
                    "jungle-clear", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".q", "Use Q").SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", "Use E").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", "Use Q").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));

            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".w-push", "W Pushing").SetValue(true));

            var qGapcloserMenu = miscMenu.AddSubMenu(new Menu("Q Gapcloser", miscMenu.Name + "q-gapcloser"));
            GapcloserManager.AddToMenu(
                qGapcloserMenu,
                new HeroListManagerArgs("q-gapcloser")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                });
            BestTargetOnlyManager.AddToMenu(qGapcloserMenu, "q-gapcloser", true);

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
            IndicatorManager.Add("R", hero => R.IsReady() ? R.GetDamage(hero) * 0.8f : 0);
            IndicatorManager.Finale();
        }

        private float GetRDamage(Obj_AI_Base target)
        {
            var dmg = R.GetDamage(target);
            var collisions = Math.Min(
                7,
                R.GetCollision(Player.ServerPosition.To2D(), new List<Vector2> { target.ServerPosition.To2D() }).Count);
            return dmg * (1 - collisions / 10);
        }

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (!args.Unit.IsMe)
                {
                    return;
                }

                if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                     Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit) && args.Target is Obj_AI_Minion)
                {
                    args.Process = args.Target.NetworkId != _lastFarmQKill;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
            {
                if (!unit.IsMe)
                {
                    return;
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.w-push").GetValue<bool>() && W.IsReady())
                {
                    if (target is Obj_BarracksDampener || target is Obj_AI_Turret || target is Obj_HQ)
                    {
                        var ally =
                            GameObjects.AllyHeroes.Where(e => !e.IsMe && e.IsValidTarget(W.Range, false))
                                .OrderBy(w => w.TotalAttackDamage)
                                .FirstOrDefault();
                        if (ally != null)
                        {
                            W.Cast(ally);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnPreUpdate() {}

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

        private bool SpellCollision(Spell spell, Obj_AI_Base target)
        {
            return
                spell.GetCollision(Player.ServerPosition.To2D(), new List<Vector2> { target.ServerPosition.To2D() })
                    .Any(c => c.NetworkId != target.NetworkId);
        }

        private void OnEnemyGapcloser(object sender, GapcloserManagerArgs args)
        {
            try
            {
                if (args.UniqueId.Equals("q-gapcloser") && Q.IsReady() &&
                    BestTargetOnlyManager.Check("q-gapcloser", Q, args.Hero))
                {
                    if (args.End.Distance(Player.Position) <= Q.Range && !SpellCollision(Q, args.Hero))
                    {
                        var delay = (int) (args.EndTime - Game.Time - Q.Delay - 0.1f);
                        if (delay > 0)
                        {
                            Utility.DelayAction.Add(delay * 1000, () => Q.Cast(args.End));
                        }
                        else
                        {
                            Q.Cast(args.End);
                        }
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
            if (useE)
            {
                var target = TargetSelector.GetTarget((E.Range + Q.Range) * 0.9f, E.DamageType);
                if (target != null)
                {
                    var safety = Menu.Item(Menu.Name + ".combo.e-safety").GetValue<Slider>().Value;
                    var playerEnemies =
                        GameObjects.EnemyHeroes.Where(e => e.IsValidTarget() && e.Distance(Player) < safety).ToList();
                    if (playerEnemies.Count >= 2 ||
                        playerEnemies.Count == 1 && playerEnemies.First().HealthPercent > Player.HealthPercent)
                    {
                        var pos = Menu.Item(Menu.Name + ".combo.e-mode").GetValue<StringList>().SelectedIndex == 0
                            ? Utils.GetDashPosition(
                                E, target, Menu.Item(Menu.Name + ".combo.e-safety").GetValue<Slider>().Value)
                            : Player.Position.Extend(
                                Game.CursorPos, Math.Min(E.Range, Player.Position.Distance(Game.CursorPos)));

                        if (!pos.Equals(Vector3.Zero))
                        {
                            E.Cast(pos);
                        }
                    }
                    else
                    {
                        var newPosition = Player.Position.Extend(target.Position, E.Range);
                        var enemies =
                            GameObjects.EnemyHeroes.Where(
                                e => e.IsValidTarget() && e.Distance(newPosition) < safety * 1.25f).ToList();
                        var allies =
                            GameObjects.AllyHeroes.Where(
                                e => e.IsValidTarget(float.MaxValue, false) && e.Distance(newPosition) < safety)
                                .ToList();
                        var avgEnemyHealth = enemies.Average(e => e.HealthPercent);
                        if (enemies.Count - allies.Count <= 1 ||
                            enemies.Count - allies.Count <= 2 && allies.Average(e => e.HealthPercent) > avgEnemyHealth &&
                            (Player.HealthPercent >= 50 || avgEnemyHealth < 30))
                        {
                            E.Cast(newPosition);
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
                    var best = CPrediction.Line(W, target, W.GetHitChance("combo"));
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
                var prediction = CPrediction.Line(Q, target, hitChance);
                if (prediction.TotalHits <= 0 || prediction.CastPosition.Equals(Vector3.Zero) ||
                    SpellCollision(Q, target) && !Orbwalking.InAutoAttackRange(target))
                {
                    var newTarget = TargetSelector.GetTarget(
                        Q, true, default(Vector3), new List<Obj_AI_Hero> { target });
                    if (newTarget != null && !SpellCollision(Q, newTarget))
                    {
                        prediction = CPrediction.Line(Q, newTarget, hitChance);
                    }
                }
                if (prediction.TotalHits > 0 && !prediction.CastPosition.Equals(Vector3.Zero))
                {
                    Q.Cast(prediction.CastPosition);
                }
            }
        }

        private bool RLogic(UltimateModeType mode, Obj_AI_Hero target)
        {
            try
            {
                if (Ultimate.IsActive(mode))
                {
                    var pred = CPrediction.Line(R, target, R.GetHitChance("combo"));
                    if (pred.TotalHits > 0 && Ultimate.Check(mode, pred.Hits))
                    {
                        R.Cast(pred.CastPosition);
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
                    foreach (var target in GameObjects.EnemyHeroes.Where(t => Ultimate.CheckSingle(mode, t)))
                    {
                        var pred = CPrediction.Line(R, target, R.GetHitChance("combo"));
                        if (pred.TotalHits > 0)
                        {
                            R.Cast(pred.CastPosition);
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

        private float CalcComboDamage(Obj_AI_Hero target, float resMulti, bool rangeCheck, bool q, bool w, bool r)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }

                var damage = 0f;
                var totalMana = 0f;

                var pred = R.GetPrediction(target);
                var position = target.Position;
                if (!pred.UnitPosition.Equals(Vector3.Zero))
                {
                    position = pred.UnitPosition;
                }

                if (r && R.IsReady() && (!rangeCheck || R.IsInRange(target)))
                {
                    var rMana = R.ManaCost * resMulti;
                    if (totalMana + rMana <= Player.Mana)
                    {
                        totalMana += rMana;
                        damage += GetRDamage(target);
                    }
                }
                var qMana = Q.ManaCost * resMulti;
                if (totalMana + qMana <= Player.Mana && q && (!rangeCheck || Q.IsInRange(position)))
                {
                    totalMana += qMana;
                    damage += Q.GetDamage(target);
                    if (totalMana + qMana <= Player.Mana)
                    {
                        totalMana += qMana;
                        damage += Q.GetDamage(target);
                    }
                }
                if (w && W.IsReady() && (!rangeCheck || W.IsInRange(position)))
                {
                    var wMana = W.ManaCost * resMulti;
                    if (totalMana + wMana <= Player.Mana)
                    {
                        damage += W.GetDamage(target);
                    }
                }
                if (!rangeCheck ||
                    position.Distance(Player.Position) <= Orbwalking.GetRealAutoAttackRange(target) * 0.9f)
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
            if (Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W);
                if (target != null)
                {
                    var best = CPrediction.Line(W, target, W.GetHitChance("harass"));
                    if (best.TotalHits > 0 && !best.CastPosition.Equals(Vector3.Zero))
                    {
                        W.Cast(best.CastPosition);
                    }
                }
            }
        }

        protected override void LaneClear()
        {
            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady() && !Player.IsWindingUp &&
                       ResourceManager.Check("lane-clear");
            if (useQ)
            {
                var minion =
                    MinionManager.GetMinions(Q.Range)
                        .Where(
                            m =>
                                Q.GetDamage(m) > m.Health ||
                                HealthPrediction.LaneClearHealthPrediction(
                                    m, (int) (Player.AttackDelay * 1000), (int) (Q.Delay * 1000)) >
                                Player.GetAutoAttackDamage(m) * 1.5f)
                        .OrderBy(m => !Orbwalking.InAutoAttackRange(m))
                        .ThenBy(m => m.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    Q.Cast(minion);
                    if (Q.GetDamage(minion) > minion.Health)
                    {
                        _lastFarmQKill = minion.NetworkId;
                    }
                }
            }
        }

        protected override void JungleClear()
        {
            var useQ = Menu.Item(Menu.Name + ".jungle-clear.q").GetValue<bool>() && Q.IsReady() && !Player.IsWindingUp &&
                       ResourceManager.Check("jungle-clear");
            if (useQ)
            {
                Casting.Farm(
                    Q,
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth),
                    1);
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
                        .Select(enemy => Q.GetPrediction(enemy))
                        .FirstOrDefault(pred => pred.Hitchance >= HitChance.High);
                if (fPredEnemy != null)
                {
                    Q.Cast(fPredEnemy.CastPosition);
                }
            }
        }
    }
}