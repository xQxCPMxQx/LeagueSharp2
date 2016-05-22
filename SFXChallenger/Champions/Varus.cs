#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Varus.cs is part of SFXChallenger.

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
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;
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
    internal class Varus : Champion
    {
        private float _lastLaneClearQStart;
        private float _rSpreadRadius = 450f;
        private MenuItem _wStacks;

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
            GapcloserManager.OnGapcloser += OnEnemyGapcloser;
            Drawing.OnDraw += OnDrawingDraw;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 925f);
            Q.SetSkillshot(0.25f, 70f, 1800f, false, SkillshotType.SkillshotLine);
            Q.SetCharged("VarusQ", "VarusQ", 925, 1700, 1.5f);

            W = new Spell(SpellSlot.W, 0f, DamageType.Magical);

            E = new Spell(SpellSlot.E, 950f);
            E.SetSkillshot(0.25f, 250f, 1500f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 1075f);
            R.SetSkillshot(0.25f, 120f, 1950f, false, SkillshotType.SkillshotLine);

            Ultimate = new UltimateManager
            {
                Combo = true,
                Assisted = true,
                Auto = true,
                Flash = false,
                Required = true,
                Force = true,
                Gapcloser = true,
                GapcloserDelay = false,
                Interrupt = false,
                InterruptDelay = false,
                Spells = Spells,
                DamageCalculation =
                    (hero, resMulti, rangeCheck) =>
                        CalcComboDamage(
                            hero, resMulti, rangeCheck, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>(), true)
            };
        }

        protected override void AddToMenu()
        {
            var ultimateMenu = Ultimate.AddToMenu(Menu);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".range", "Range").SetValue(new Slider((int) R.Range, 500, 1200)))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { R.Range = args.GetNewValue<Slider>().Value; };

            R.Range = Menu.Item(ultimateMenu.Name + ".range").GetValue<Slider>().Value;

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".radius", "Spread Radius").SetValue(new Slider(450, 100, 600)))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    _rSpreadRadius = args.GetNewValue<Slider>().Value;
                };

            _rSpreadRadius = Menu.Item(Menu.Name + ".ultimate.radius").GetValue<Slider>().Value;

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.VeryHigh },
                    { "E", HitChance.High },
                    { "R", HitChance.VeryHigh }
                });

            var comboQMenu = comboMenu.AddSubMenu(new Menu("Q Settings", comboMenu.Name + ".q-settings"));
            comboQMenu.AddItem(new MenuItem(comboQMenu.Name + ".always", "Cast Always").SetValue(true));
            comboQMenu.AddItem(
                new MenuItem(comboQMenu.Name + ".fast-cast-min", "Fast Cast Health <= %").SetValue(new Slider(20)));
            comboQMenu.AddItem(new MenuItem(comboQMenu.Name + ".separator", string.Empty));
            comboQMenu.AddItem(new MenuItem(comboQMenu.Name + ".stacks", "Min. Stacks")).SetValue(new Slider(3, 1, 3));
            comboQMenu.AddItem(new MenuItem(comboQMenu.Name + ".or", "OR"));
            comboQMenu.AddItem(new MenuItem(comboQMenu.Name + ".min", "Min. Hits").SetValue(new Slider(2, 1, 5)));

            var comboEMenu = comboMenu.AddSubMenu(new Menu("E Settings", comboMenu.Name + ".e-settings"));
            comboEMenu.AddItem(new MenuItem(comboEMenu.Name + ".always", "Cast Always").SetValue(false));
            comboEMenu.AddItem(new MenuItem(comboEMenu.Name + ".separator", string.Empty));
            comboEMenu.AddItem(new MenuItem(comboEMenu.Name + ".stacks", "Min. Stacks")).SetValue(new Slider(3, 1, 3));
            comboEMenu.AddItem(new MenuItem(comboEMenu.Name + ".or", "OR"));
            comboEMenu.AddItem(new MenuItem(comboEMenu.Name + ".min", "Min. Hits").SetValue(new Slider(3, 1, 5)));

            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High }, { "E", HitChance.High } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    DefaultValue = 30
                });

            var harassQMenu = harassMenu.AddSubMenu(new Menu("Q Settings", harassMenu.Name + ".q-settings"));
            harassQMenu.AddItem(new MenuItem(harassQMenu.Name + ".always", "Cast Always").SetValue(true));
            harassQMenu.AddItem(
                new MenuItem(harassQMenu.Name + ".fast-cast-min", "Fast Cast Health <= %").SetValue(new Slider(25)));
            harassQMenu.AddItem(new MenuItem(harassQMenu.Name + ".separator", string.Empty));
            harassQMenu.AddItem(new MenuItem(harassQMenu.Name + ".stacks", "Min. Stacks")).SetValue(new Slider(3, 1, 3));
            harassQMenu.AddItem(new MenuItem(harassQMenu.Name + ".or", "OR"));
            harassQMenu.AddItem(new MenuItem(harassQMenu.Name + ".min", "Min. Hits").SetValue(new Slider(2, 1, 5)));

            var harassEMenu = harassMenu.AddSubMenu(new Menu("E Settings", harassMenu.Name + ".e-settings"));
            harassEMenu.AddItem(new MenuItem(harassEMenu.Name + ".always", "Cast Always").SetValue(true));
            harassEMenu.AddItem(new MenuItem(harassEMenu.Name + ".separator", string.Empty));
            harassEMenu.AddItem(new MenuItem(harassEMenu.Name + ".stacks", "Min. Stacks")).SetValue(new Slider(2, 1, 3));
            harassEMenu.AddItem(new MenuItem(harassEMenu.Name + ".or", "OR"));
            harassEMenu.AddItem(new MenuItem(harassEMenu.Name + ".min", "Min. Hits").SetValue(new Slider(3, 1, 5)));

            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", "Use E").SetValue(true));

            var laneClearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ResourceManager.AddToMenu(
                laneClearMenu,
                new ResourceManagerArgs(
                    "lane-clear-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "Q",
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });
            ResourceManager.AddToMenu(
                laneClearMenu,
                new ResourceManagerArgs(
                    "lane-clear-e", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "E",
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".q", "Use Q").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".e", "Use E").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".min", "Min. Hits").SetValue(new Slider(3, 1, 5)));

            var jungleClearMenu = Menu.AddSubMenu(new Menu("Jungle Clear", Menu.Name + ".jungle-clear"));
            ResourceManager.AddToMenu(
                jungleClearMenu,
                new ResourceManagerArgs(
                    "jungle-clear-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "Q",
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 30, 10, 10 }
                });
            ResourceManager.AddToMenu(
                jungleClearMenu,
                new ResourceManagerArgs(
                    "jungle-clear-e", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "E",
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 30, 10, 10 }
                });
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".q", "Use Q").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".e", "Use E").SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", "Use E").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", "Use Q").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));

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

            TargetSelector.Weights.Register(
                new TargetSelector.Weights.Item(
                    "w-stacks", "W Stacks", 5, false, t => GetWStacks(t), "High amount of W Stacks = Higher Weight"));

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add("Q", hero => Q.IsReady() ? Q.GetDamage(hero, 1) : 0);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();

            _wStacks = DrawingManager.Add("W Stacks", true);
        }

        protected override void OnPreUpdate() {}

        protected override void OnPostUpdate()
        {
            Orbwalker.SetAttack(!Q.IsCharging);
            if (Ultimate.IsActive(UltimateModeType.Assisted) && R.IsReady())
            {
                if (Ultimate.ShouldMove(UltimateModeType.Assisted))
                {
                    Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                }

                if (!RLogic(UltimateModeType.Assisted, R.GetHitChance("combo"), TargetSelector.GetTarget(R)))
                {
                    RLogicSingle(UltimateModeType.Assisted, R.GetHitChance("combo"));
                }
            }

            if (Ultimate.IsActive(UltimateModeType.Auto) && R.IsReady())
            {
                if (!RLogic(UltimateModeType.Auto, R.GetHitChance("combo"), TargetSelector.GetTarget(R)))
                {
                    RLogicSingle(UltimateModeType.Auto, R.GetHitChance("combo"));
                }
            }
        }

        private void OnEnemyGapcloser(object sender, GapcloserManagerArgs args)
        {
            try
            {
                if (args.UniqueId.Equals("e-gapcloser") && E.IsReady() &&
                    BestTargetOnlyManager.Check("e-gapcloser", E, args.Hero))
                {
                    if (args.End.Distance(Player.Position) <= E.Range)
                    {
                        E.Cast(args.End);
                    }
                }
                if (string.IsNullOrEmpty(args.UniqueId))
                {
                    if (Ultimate.IsActive(UltimateModeType.Gapcloser, args.Hero) &&
                        BestTargetOnlyManager.Check("r-gapcloser", R, args.Hero))
                    {
                        RLogic(UltimateModeType.Gapcloser, HitChance.High, args.Hero);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>();
            var r = Ultimate.IsActive(UltimateModeType.Combo);

            if (e && !Q.IsCharging && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E);
                if (target != null)
                {
                    var stacks = W.Level == 0 &&
                                 Menu.Item(Menu.Name + ".combo.e-settings.stacks").GetValue<Slider>().Value > 0;
                    if (Menu.Item(Menu.Name + ".combo.e-settings.always").GetValue<bool>() || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.e-settings.stacks").GetValue<Slider>().Value ||
                        E.IsKillable(target) ||
                        CPrediction.Circle(E, target, E.GetHitChance("combo")).TotalHits >=
                        Menu.Item(Menu.Name + ".combo.e-settings.min").GetValue<Slider>().Value)
                    {
                        ELogic(target, E.GetHitChance("combo"));
                    }
                }
            }
            if (q && Q.IsReady())
            {
                var target = TargetSelector.GetTarget((Q.ChargedMaxRange + Q.Width) * 1.1f, Q.DamageType);
                if (target != null)
                {
                    var stacks = W.Level == 0 &&
                                 Menu.Item(Menu.Name + ".combo.q-settings.stacks").GetValue<Slider>().Value > 0;
                    if (Q.IsCharging || Menu.Item(Menu.Name + ".combo.q-settings.always").GetValue<bool>() ||
                        target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(target) * 1.2f || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.q-settings.stacks").GetValue<Slider>().Value ||
                        CPrediction.Line(Q, target, Q.GetHitChance("combo")).TotalHits >=
                        Menu.Item(Menu.Name + ".combo.q-settings.min").GetValue<Slider>().Value || Q.IsKillable(target))
                    {
                        QLogic(
                            target, Q.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".combo.q-settings.fast-cast-min").GetValue<Slider>().Value);
                    }
                }
            }
            if (r && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R);
                if (target != null)
                {
                    if (!RLogic(UltimateModeType.Combo, R.GetHitChance("combo"), target))
                    {
                        RLogicSingle(UltimateModeType.Combo, R.GetHitChance("combo"));
                    }
                }
            }
        }

        protected override void Harass()
        {
            if (!ResourceManager.Check("harass") && !Q.IsCharging)
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && !Q.IsCharging && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E);
                if (target != null)
                {
                    var stacks = W.Level == 0 &&
                                 Menu.Item(Menu.Name + ".harass.e-settings.stacks").GetValue<Slider>().Value > 0;
                    if (Menu.Item(Menu.Name + ".harass.e-settings.always").GetValue<bool>() || stacks ||
                        GetWStacks(target) >=
                        Menu.Item(Menu.Name + ".harass.e-settings.stacks").GetValue<Slider>().Value ||
                        E.IsKillable(target) ||
                        CPrediction.Circle(E, target, E.GetHitChance("harass")).TotalHits >=
                        Menu.Item(Menu.Name + ".combo.e-settings.min").GetValue<Slider>().Value)
                    {
                        ELogic(target, E.GetHitChance("harass"));
                    }
                }
            }
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                var target = TargetSelector.GetTarget((Q.ChargedMaxRange + Q.Width) * 1.1f, Q.DamageType);
                if (target != null)
                {
                    var stacks = W.Level == 0 &&
                                 Menu.Item(Menu.Name + ".harass.q-settings.stacks").GetValue<Slider>().Value > 0;
                    if (Q.IsCharging || Menu.Item(Menu.Name + ".harass.q-settings.always").GetValue<bool>() ||
                        target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(target) * 1.2f || stacks ||
                        GetWStacks(target) >=
                        Menu.Item(Menu.Name + ".harass.q-settings.stacks").GetValue<Slider>().Value ||
                        Q.IsKillable(target) ||
                        CPrediction.Line(Q, target, Q.GetHitChance("harass")).TotalHits >=
                        Menu.Item(Menu.Name + ".harass.q-settings.min").GetValue<Slider>().Value)
                    {
                        QLogic(
                            target, Q.GetHitChance("harass"),
                            Menu.Item(Menu.Name + ".harass.q-settings.fast-cast-min").GetValue<Slider>().Value);
                    }
                }
            }
        }

        private bool QMaxRangeHit(Obj_AI_Hero target)
        {
            var delay = Q.ChargeDuration / 1000f *
                        ((Q.Range - Q.ChargedMinRange) / (Q.ChargedMaxRange - Q.ChargedMinRange));
            return
                Utils.PositionAfter(
                    target,
                    delay + (Player.Distance(target) - Q.Width - target.BoundingRadius * 0.75f) / Q.Speed +
                    Game.Ping / 2000f, target.MoveSpeed).Distance(Player) < Q.ChargedMaxRange;
        }

        private bool QIsKillable(Obj_AI_Hero target, int collisions)
        {
            return target.Health + target.HPRegenRate / 2f < GetQDamage(target, collisions);
        }

        private bool IsFullyCharged()
        {
            return Q.ChargedMaxRange - Q.Range < 200;
        }

        private float GetQDamage(Obj_AI_Hero target, int collisions)
        {
            if (Q.Level == 0)
            {
                return 0;
            }
            var chargePercentage = Q.Range / Q.ChargedMaxRange;
            var damage =
                (float)
                    (new float[] { 10, 47, 83, 120, 157 }[Q.Level - 1] +
                     new float[] { 5, 23, 42, 60, 78 }[Q.Level - 1] * chargePercentage +
                     chargePercentage * (Player.TotalAttackDamage + Player.TotalAttackDamage * .6));
            var minimum = damage / 100f * 33f;
            for (var i = 0; i < collisions; i++)
            {
                var reduce = damage / 100f * 15f;
                if (damage - reduce < minimum)
                {
                    damage = minimum;
                    break;
                }
                damage -= reduce;
            }
            return (float) Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        private int GetQCollisionsCount(Obj_AI_Hero target, Vector3 castPos)
        {
            try
            {
                var input = new PredictionInput
                {
                    Unit = target,
                    Radius = Q.Width,
                    Delay = Q.Delay,
                    Speed = Q.Speed,
                    CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.Minions }
                };
                return
                    Collision.GetCollision(
                        new List<Vector3> { Player.Position.Extend(castPos, Q.Range + Q.Width) }, input).Count;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private void QLogic(Obj_AI_Hero target, HitChance hitChance, int minHealthPercent)
        {
            try
            {
                if (target == null)
                {
                    return;
                }

                var pred = CPrediction.Line(Q, target, hitChance);
                if (pred.TotalHits > 0 &&
                    (QIsKillable(target, GetQCollisionsCount(target, pred.CastPosition)) ||
                     Player.HealthPercent <= minHealthPercent))
                {
                    Q.Cast(pred.CastPosition);
                }

                if (!Q.IsCharging)
                {
                    if (QMaxRangeHit(target))
                    {
                        Q.StartCharging();
                    }
                }
                if (Q.IsCharging && (!QMaxRangeHit(target) || IsFullyCharged()))
                {
                    var pred2 = CPrediction.Line(Q, target, hitChance);
                    if (pred2.TotalHits > 0)
                    {
                        Q.Cast(pred2.CastPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void ELogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (Q.IsCharging || target == null)
                {
                    return;
                }

                var best = CPrediction.Circle(E, target, hitChance);
                if (best.TotalHits > 0 && !best.CastPosition.Equals(Vector3.Zero))
                {
                    E.Cast(best.CastPosition);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool RLogic(UltimateModeType mode, HitChance hitChance, Obj_AI_Hero target)
        {
            try
            {
                if (Q.IsCharging || target == null || !Ultimate.IsActive(mode))
                {
                    return false;
                }
                var pred = R.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    var hits = GameObjects.EnemyHeroes.Where(x => x.Distance(target) <= _rSpreadRadius).ToList();
                    if (Ultimate.Check(mode, hits))
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

        private void RLogicSingle(UltimateModeType mode, HitChance hitChance)
        {
            try
            {
                if (Q.IsCharging || !Ultimate.ShouldSingle(mode))
                {
                    return;
                }
                foreach (var t in GameObjects.EnemyHeroes.Where(t => Ultimate.CheckSingle(mode, t)))
                {
                    var pred = R.GetPrediction(t);
                    if (pred.Hitchance >= hitChance)
                    {
                        R.Cast(pred.CastPosition);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private float CalcComboDamage(Obj_AI_Hero target, float resMulti, bool rangeCheck, bool q, bool e, bool r)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }

                float damage = 0;
                var totalMana = 0f;

                if (r && R.IsReady() && (!rangeCheck || R.IsInRange(target)))
                {
                    var rMana = R.ManaCost * resMulti;
                    if (totalMana + rMana <= Player.Mana)
                    {
                        totalMana += rMana;
                        damage += R.GetDamage(target);
                    }
                }
                if (q && Q.IsReady() && (!rangeCheck || Q.IsInRange(target, Q.ChargedMaxRange)))
                {
                    var qMana = Q.ManaCost * resMulti;
                    if (totalMana + qMana <= Player.Mana)
                    {
                        totalMana += qMana;
                        damage += GetQDamage(target, 1);
                    }
                }
                if (e && E.IsReady() && (!rangeCheck || E.IsInRange(target, E.Range + E.Width)))
                {
                    var eMana = E.ManaCost * resMulti;
                    if (totalMana + eMana <= Player.Mana)
                    {
                        damage += E.GetDamage(target);
                    }
                }
                if (!rangeCheck || Orbwalking.InAutoAttackRange(target))
                {
                    damage += 5f * (float) Player.GetAutoAttackDamage(target);
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

        protected override void LaneClear()
        {
            var min = Menu.Item(Menu.Name + ".lane-clear.min").GetValue<Slider>().Value;
            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady() &&
                       (ResourceManager.Check("lane-clear-q") || Q.IsCharging);
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady() &&
                       ResourceManager.Check("lane-clear-e");
            if (useQ)
            {
                var minions = MinionManager.GetMinions(Q.ChargedMaxRange);
                if (Q.IsCharging || minions.Count >= min)
                {
                    if (!Q.IsCharging)
                    {
                        Q.StartCharging();
                        _lastLaneClearQStart = Game.Time;
                    }
                    if (Q.IsCharging && IsFullyCharged())
                    {
                        Casting.Farm(
                            Q, minions,
                            Game.Time - _lastLaneClearQStart > 3 ? 1 : (minions.Count < min ? minions.Count : min));
                    }
                }
            }

            if (useE)
            {
                Casting.Farm(E, MinionManager.GetMinions(E.Range + E.Width), min);
            }
        }

        protected override void JungleClear()
        {
            var useQ = Menu.Item(Menu.Name + ".jungle-clear.q").GetValue<bool>() && Q.IsReady() &&
                       (ResourceManager.Check("jungle-clear-q") || Q.IsCharging);
            var useE = Menu.Item(Menu.Name + ".jungle-clear.e").GetValue<bool>() && E.IsReady() &&
                       ResourceManager.Check("jungle-clear-e");
            if (useQ)
            {
                var minions = MinionManager.GetMinions(
                    Q.ChargedMaxRange, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (Q.IsCharging || minions.Count >= 1)
                {
                    if (!Q.IsCharging)
                    {
                        Q.StartCharging();
                        _lastLaneClearQStart = Game.Time;
                    }
                    if (Q.IsCharging && IsFullyCharged())
                    {
                        Casting.Farm(Q, minions, 1);
                    }
                }
            }

            if (useE)
            {
                Casting.Farm(
                    E,
                    MinionManager.GetMinions(
                        E.Range + E.Width, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth), 1);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && !Q.IsCharging && E.IsReady())
            {
                ELogic(
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(E.Range))
                        .OrderBy(e => e.Position.Distance(Player.Position))
                        .FirstOrDefault(), HitChance.High);
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
            {
                var killable =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e =>
                            e.IsValidTarget(Q.Range) && !Orbwalking.InAutoAttackRange(e) &&
                            (QIsKillable(e, 1) || QMaxRangeHit(e) && QIsKillable(e, 2)));
                if (killable != null)
                {
                    QLogic(killable, HitChance.High, 100);
                }
            }
        }

        private int GetWStacks(Obj_AI_Base target)
        {
            return target.GetBuffCount("varuswdebuff");
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (!Utils.ShouldDraw())
                {
                    return;
                }
                if (W.Level > 0 && _wStacks != null && _wStacks.GetValue<bool>())
                {
                    foreach (var enemy in
                        GameObjects.EnemyHeroes.Where(
                            e => e.IsHPBarRendered && e.Position.IsOnScreen() && e.IsValidTarget()))
                    {
                        var stacks = GetWStacks(enemy) - 1;
                        if (stacks > -1)
                        {
                            var x = enemy.HPBarPosition.X + 45;
                            var y = enemy.HPBarPosition.Y - 25;
                            for (var i = 0; 3 > i; i++)
                            {
                                Drawing.DrawLine(
                                    x + i * 20, y, x + i * 20 + 10, y, 10, i > stacks ? Color.DarkGray : Color.Orange);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}