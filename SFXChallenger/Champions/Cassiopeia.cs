#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Cassiopeia.cs is part of SFXChallenger.

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
    internal class Cassiopeia : TChampion
    {
        private Obj_AI_Minion _lastAaMinion;
        private float _lastAaMinionEndTime;
        private int _lastECast;
        private float _lastEEndTime;
        private float _lastPoisonClearDelay;
        private Vector2 _lastPoisonClearPosition;
        private float _lastQPoisonDelay;
        private Obj_AI_Base _lastQPoisonT;
        public Cassiopeia() : base(1500f) {}

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.Custom; }
        }

        protected override void OnLoad()
        {
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            GapcloserManager.OnGapcloser += OnEnemyGapcloser;
            BuffManager.OnBuff += OnBuffManagerBuff;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f, DamageType.Magical);
            Q.SetSkillshot(0.3f, 50f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 850f, DamageType.Magical);
            W.SetSkillshot(0.5f, 125f, 2500f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700f, DamageType.Magical);
            E.SetTargetted(0.2f, 1700f);
            E.Collision = true;

            R = new Spell(SpellSlot.R, 825f, DamageType.Magical);
            R.SetSkillshot(0.8f, (float) (80 * Math.PI / 180), float.MaxValue, false, SkillshotType.SkillshotCone);

            Ultimate = new UltimateManager
            {
                Combo = true,
                Assisted = true,
                Auto = true,
                Flash = true,
                Required = true,
                Force = true,
                Gapcloser = true,
                GapcloserDelay = false,
                Interrupt = true,
                InterruptDelay = false,
                Spells = Spells,
                DamageCalculation =
                    (hero, resMulti, rangeCheck) =>
                        CalcComboDamage(
                            hero, resMulti, rangeCheck, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>(),
                            Menu.Item(Menu.Name + ".combo.w").GetValue<bool>(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>(), true)
            };
        }

        protected override void AddToMenu()
        {
            DrawingManager.Add("R Flash", R.Range + SummonerManager.Flash.Range);

            var ultimateMenu = Ultimate.AddToMenu(Menu);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".range", "Range").SetValue(new Slider(700, 400, 825))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    R.Range = args.GetNewValue<Slider>().Value;
                    DrawingManager.Update("R Flash", args.GetNewValue<Slider>().Value + SummonerManager.Flash.Range);
                };
            ultimateMenu.AddItem(new MenuItem(ultimateMenu.Name + ".backwards", "Backwards Flash").SetValue(true));

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.High },
                    { "W", HitChance.High },
                    { "R", HitChance.High }
                });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".aa", "Use AutoAttacks").SetValue(false));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh }, { "W", HitChance.High } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    DefaultValue = 30
                });
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".aa", "Use AutoAttacks").SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", "Use E").SetValue(true));

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
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".aa", "Use AutoAttacks").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".q", "Use Q").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".w", "Use W").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(laneClearMenu.Name + ".e", "Use E").SetValue(true));

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
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".aa", "Use AutoAttacks").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".q", "Use Q").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".w", "Use W").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".e", "Use E").SetValue(true));

            var lasthitMenu = Menu.AddSubMenu(new Menu("Last Hit", Menu.Name + ".lasthit"));
            ResourceManager.AddToMenu(
                lasthitMenu,
                new ResourceManagerArgs(
                    "lasthit", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Maximum)
                {
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 90, 70, 70 }
                });
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e", "Use E").SetValue(true));
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e-poison", "Use E Poison").SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", "Use W").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e", "Use E").SetValue(true));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e-poison", "Use E Poison Only").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));
            DelayManager.AddToMenu(miscMenu, "e-delay", "E", 420, 0, 1000);

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

            var qFleeingMenu = miscMenu.AddSubMenu(new Menu("Q Fleeing", miscMenu.Name + "q-fleeing"));
            HeroListManager.AddToMenu(
                qFleeingMenu,
                new HeroListManagerArgs("q-fleeing")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                });
            BestTargetOnlyManager.AddToMenu(qFleeingMenu, "q-fleeing", true);

            var wGapcloserMenu = miscMenu.AddSubMenu(new Menu("W Gapcloser", miscMenu.Name + "w-gapcloser"));
            GapcloserManager.AddToMenu(
                wGapcloserMenu,
                new HeroListManagerArgs("w-gapcloser")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                }, true);
            BestTargetOnlyManager.AddToMenu(wGapcloserMenu, "w-gapcloser");

            var wImmobileMenu = miscMenu.AddSubMenu(new Menu("W Immobile", miscMenu.Name + "w-immobile"));
            BuffManager.AddToMenu(
                wImmobileMenu, BuffManager.ImmobileBuffs,
                new HeroListManagerArgs("w-immobile")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                }, true);
            BestTargetOnlyManager.AddToMenu(wImmobileMenu, "w-immobile");

            var wFleeingMenu = miscMenu.AddSubMenu(new Menu("W Fleeing", miscMenu.Name + "w-fleeing"));
            HeroListManager.AddToMenu(
                wFleeingMenu,
                new HeroListManagerArgs("w-fleeing")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                });
            BestTargetOnlyManager.AddToMenu(wFleeingMenu, "w-fleeing", true);

            R.Range = Menu.Item(Menu.Name + ".ultimate.range").GetValue<Slider>().Value;
            DrawingManager.Update(
                "R Flash",
                Menu.Item(Menu.Name + ".ultimate.range").GetValue<Slider>().Value + SummonerManager.Flash.Range);

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add("E", hero => E.GetDamage(hero) * 3);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();

            TargetSelector.Weights.Register(
                new TargetSelector.Weights.Item(
                    "poison-time", "Poison Time", 5, false, GetPoisonBuffEndTime,
                    "Long time until poison ends = Higher Weight"));
        }

        private void OnBuffManagerBuff(object sender, BuffManagerArgs args)
        {
            try
            {
                if (W.IsReady())
                {
                    if (args.UniqueId.Equals("w-immobile") && BestTargetOnlyManager.Check("w-immobile", W, args.Hero) &&
                        W.IsInRange(args.Position))
                    {
                        W.Cast(args.Position);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnPreUpdate()
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit && ResourceManager.Check("lasthit") &&
                E.IsReady())
            {
                var ePoison = Menu.Item(Menu.Name + ".lasthit.e-poison").GetValue<bool>();
                var eHit = Menu.Item(Menu.Name + ".lasthit.e").GetValue<bool>();
                if (eHit || ePoison)
                {
                    var m =
                        MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly)
                            .FirstOrDefault(
                                e =>
                                    (_lastAaMinion == null || e.NetworkId != _lastAaMinion.NetworkId ||
                                     Game.Time > _lastAaMinionEndTime) && e.Health < E.GetDamage(e) - 5 &&
                                    (ePoison && GetPoisonBuffEndTime(e) > E.ArrivalTime(e) || eHit));
                    if (m != null)
                    {
                        Casting.TargetSkill(m, E);
                    }
                }
            }
        }

        protected override void OnPostUpdate()
        {
            if (Ultimate.IsActive(UltimateModeType.Flash) && R.IsReady() && SummonerManager.Flash.IsReady())
            {
                if (Ultimate.ShouldMove(UltimateModeType.Flash))
                {
                    Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                }
                var targets =
                    Targets.Where(
                        t =>
                            t.Distance(Player) < (R.Range + R.Width + SummonerManager.Flash.Range) * 1.5f &&
                            !t.IsDashing() &&
                            (t.IsFacing(Player)
                                ? t.Distance(Player)
                                : R.GetPrediction(t).UnitPosition.Distance(Player.Position)) > R.Range);
                var backwards = Menu.Item(Menu.Name + ".ultimate.backwards").GetValue<bool>();
                foreach (var target in targets)
                {
                    var flashPos = Player.Position.Extend(target.Position, SummonerManager.Flash.Range);
                    var maxHits = GetMaxRHits(HitChance.High, flashPos);
                    if (maxHits.Item1.Count > 0)
                    {
                        var castPos = backwards
                            ? Player.Position.Extend(maxHits.Item2, -(Player.Position.Distance(maxHits.Item2) * 2))
                            : Player.Position.Extend(maxHits.Item2, Player.Position.Distance(maxHits.Item2));
                        if (Ultimate.Check(UltimateModeType.Flash, maxHits.Item1))
                        {
                            if (R.Cast(castPos))
                            {
                                Utility.DelayAction.Add(300 + Game.Ping / 2, () => SummonerManager.Flash.Cast(flashPos));
                            }
                        }
                        else if (Ultimate.ShouldSingle(UltimateModeType.Flash))
                        {
                            if (
                                maxHits.Item1.Where(hit => Ultimate.CheckSingle(UltimateModeType.Flash, hit))
                                    .Any(hit => R.Cast(castPos)))
                            {
                                Utility.DelayAction.Add(300 + Game.Ping / 2, () => SummonerManager.Flash.Cast(flashPos));
                            }
                        }
                    }
                }
            }

            if (Ultimate.IsActive(UltimateModeType.Assisted) && R.IsReady())
            {
                if (Ultimate.ShouldMove(UltimateModeType.Assisted))
                {
                    Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                }

                if (!RLogic(UltimateModeType.Assisted, R.GetHitChance("combo")))
                {
                    RLogicSingle(UltimateModeType.Assisted, R.GetHitChance("combo"), false);
                }
            }

            if (Ultimate.IsActive(UltimateModeType.Auto) && R.IsReady())
            {
                if (!RLogic(UltimateModeType.Auto, R.GetHitChance("combo")))
                {
                    RLogicSingle(UltimateModeType.Auto, R.GetHitChance("combo"));
                }
            }

            if (HeroListManager.Enabled("w-immobile") && W.IsReady())
            {
                var target =
                    Targets.FirstOrDefault(
                        t =>
                            HeroListManager.Check("w-immobile", t) && BestTargetOnlyManager.Check("w-immobile", W, t) &&
                            Utils.IsImmobile(t));
                if (target != null)
                {
                    Casting.SkillShot(target, W, HitChance.High);
                }
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
            {
                if (unit.IsMe)
                {
                    var minion = target as Obj_AI_Minion;
                    if (minion != null)
                    {
                        _lastAaMinion = minion;
                        _lastAaMinionEndTime = Game.Time + minion.Distance(Player) / Orbwalking.GetMyProjectileSpeed() +
                                               0.25f;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (!args.Unit.IsMe)
                {
                    return;
                }
                var t = args.Target as Obj_AI_Hero;
                if (t != null &&
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                     Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed))
                {
                    args.Process =
                        Menu.Item(
                            Menu.Name + "." +
                            (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ? "combo" : "harass") + ".aa")
                            .GetValue<bool>();
                    if (!args.Process)
                    {
                        var poison = GetPoisonBuffEndTime(t);
                        args.Process = (!Q.IsReady() || Q.Instance.ManaCost > Player.Mana) &&
                                       ((!E.IsReady() && Game.Time - _lastECast > 3) ||
                                        E.Instance.ManaCost > Player.Mana || poison <= 0 || poison < E.ArrivalTime(t));
                    }
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    var mode = args.Target.Team == GameObjectTeam.Neutral ? "jungle-clear" : "lane-clear";
                    args.Process = Menu.Item(Menu.Name + "." + mode + ".aa").GetValue<bool>();
                    if (!args.Process)
                    {
                        var m = args.Target as Obj_AI_Minion;
                        if (m != null && (_lastEEndTime < Game.Time || E.IsReady()) ||
                            GetPoisonBuffEndTime(m) < E.ArrivalTime(m) || E.Instance.ManaCost > Player.Mana ||
                            !ResourceManager.Check(mode))
                        {
                            args.Process = true;
                        }
                    }
                }

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                {
                    var m = args.Target as Obj_AI_Minion;
                    if (m != null && E.IsReady() && E.CanCast(m) && E.Instance.ManaCost < Player.Mana)
                    {
                        var useE = Menu.Item(Menu.Name + ".lasthit.e").GetValue<bool>();
                        var useEPoison = Menu.Item(Menu.Name + ".lasthit.e-poison").GetValue<bool>();
                        if ((useE || useEPoison && GetPoisonBuffEndTime(m) > E.ArrivalTime(m)) &&
                            ResourceManager.Check("lasthit"))
                        {
                            args.Process = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            try
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter2.DangerLevel.High &&
                    Ultimate.IsActive(UltimateModeType.Interrupt, sender) && sender.IsFacing(Player))
                {
                    Casting.SkillShot(sender, R, HitChance.High);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnEnemyGapcloser(object sender, GapcloserManagerArgs args)
        {
            try
            {
                if (args.UniqueId.Equals("q-gapcloser") && Q.IsReady() &&
                    BestTargetOnlyManager.Check("q-gapcloser", Q, args.Hero))
                {
                    if (args.End.Distance(Player.Position) <= Q.Range)
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
                if (args.UniqueId.Equals("w-gapcloser") && W.IsReady() &&
                    BestTargetOnlyManager.Check("w-gapcloser", W, args.Hero))
                {
                    if (args.End.Distance(Player.Position) <= W.Range)
                    {
                        var delay = (int) (args.EndTime - Game.Time - W.Delay - 0.1f);
                        if (delay > 0)
                        {
                            Utility.DelayAction.Add(delay * 1000, () => W.Cast(args.End));
                        }
                        else
                        {
                            W.Cast(args.End);
                        }
                    }
                }
                if (string.IsNullOrEmpty(args.UniqueId))
                {
                    if (Ultimate.IsActive(UltimateModeType.Gapcloser, args.Hero) &&
                        BestTargetOnlyManager.Check("r-gapcloser", R, args.Hero))
                    {
                        if (args.End.Distance(Player.Position) <= R.Range)
                        {
                            if (args.EndTime - Game.Time > R.Delay)
                            {
                                Utility.DelayAction.Add(
                                    (int) ((args.EndTime - Game.Time - R.Delay) * 1000), () => R.Cast(args.End));
                            }
                            else
                            {
                                R.Cast(args.End);
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

        protected override void Combo()
        {
            var single = false;
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var r = Ultimate.IsActive(UltimateModeType.Combo) && R.IsReady();

            if (q)
            {
                QLogic(Q.GetHitChance("combo"));
            }
            if (w)
            {
                WLogic(W.GetHitChance("combo"));
            }
            if (e)
            {
                ELogic();
            }
            if (r)
            {
                if (!RLogic(UltimateModeType.Combo, R.GetHitChance("combo")))
                {
                    RLogicSingle(UltimateModeType.Combo, R.GetHitChance("combo"));
                    single = true;
                }
            }

            ItemsSummonersLogic(null, single);
        }

        private float CalcComboDamage(Obj_AI_Hero target,
            float resMulti,
            bool rangeCheck,
            bool q,
            bool w,
            bool e,
            bool r)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }

                var damage = 0f;
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

                if (q && Q.IsReady() && (!rangeCheck || Q.IsInRange(target)))
                {
                    var qMana = Q.ManaCost * resMulti;
                    if (totalMana + qMana <= Player.Mana)
                    {
                        totalMana += qMana;
                        damage += Q.GetDamage(target);
                    }
                }
                else if (w && W.IsReady() && (!rangeCheck || W.IsInRange(target)))
                {
                    var wMana = W.ManaCost * resMulti;
                    if (totalMana + wMana <= Player.Mana)
                    {
                        totalMana += wMana;
                        damage += W.GetDamage(target);
                    }
                }
                if (e && E.IsReady(3000) && (!rangeCheck || E.IsInRange(target)))
                {
                    var eMana = E.ManaCost * resMulti;
                    var eDamage = E.GetDamage(target);
                    var count = target.IsNearTurret() && !target.IsFacing(Player) ||
                                target.IsNearTurret() && Player.HealthPercent <= 35 || !R.IsReady()
                        ? 5
                        : 8;
                    for (var i = 0; i < count; i++)
                    {
                        if (totalMana + eMana > Player.Mana)
                        {
                            break;
                        }
                        totalMana += eMana;
                        damage += eDamage;
                    }
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

        private Tuple<List<Obj_AI_Hero>, Vector3> GetMaxRHits(HitChance hitChance, Vector3 fromCheck = default(Vector3))
        {
            if (fromCheck.Equals(default(Vector3)))
            {
                fromCheck = ObjectManager.Player.Position;
            }

            var input = new PredictionInput
            {
                Collision = true,
                CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                From = fromCheck,
                RangeCheckFrom = fromCheck,
                Type = R.Type,
                Radius = R.Width,
                Delay = R.Delay,
                Speed = R.Speed,
                Range = R.Range,
                Aoe = true
            };

            var castPosition = Vector3.Zero;
            var totalHits = new List<Obj_AI_Hero>();
            try
            {
                var positions = new List<CPrediction.Position>();
                foreach (var t in GameObjects.EnemyHeroes)
                {
                    if (t.IsValidTarget(R.Range * 1.5f, true, fromCheck))
                    {
                        input.Unit = t;
                        var prediction = Prediction.GetPrediction(input);
                        if (prediction.Hitchance >= hitChance)
                        {
                            positions.Add(new CPrediction.Position(t, prediction.UnitPosition));
                        }
                    }
                }
                var circle = new Geometry.Polygon.Circle(fromCheck, R.Range).Points;
                foreach (var point in circle)
                {
                    var hits = new List<Obj_AI_Hero>();
                    foreach (var position in positions)
                    {
                        R.UpdateSourcePosition(fromCheck, fromCheck);
                        if (R.WillHit(position.UnitPosition, point.To3D()))
                        {
                            hits.Add(position.Hero);
                        }
                        R.UpdateSourcePosition();
                    }
                    if (hits.Count > totalHits.Count)
                    {
                        castPosition = point.To3D();
                        totalHits = hits;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<List<Obj_AI_Hero>, Vector3>(totalHits, castPosition);
        }

        private void RLogicSingle(UltimateModeType mode, HitChance hitChance, bool face = true)
        {
            try
            {
                if (Ultimate.ShouldSingle(mode))
                {
                    foreach (var target in
                        Targets.Where(
                            t => (!face || t.IsFacing(Player)) && R.CanCast(t) && Ultimate.CheckSingle(mode, t)))
                    {
                        var pred = R.GetPrediction(target, true);
                        if (pred.Hitchance >= hitChance)
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

        private bool RLogic(UltimateModeType mode, HitChance hitChance)
        {
            try
            {
                if (Ultimate.IsActive(mode))
                {
                    var maxHits = GetMaxRHits(hitChance);
                    if (maxHits.Item1.Count > 0 && !maxHits.Item2.Equals(Vector3.Zero))
                    {
                        if (Ultimate.Check(mode, maxHits.Item1))
                        {
                            R.Cast(maxHits.Item2);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void QLogic(HitChance hitChance)
        {
            try
            {
                var ts =
                    Targets.FirstOrDefault(
                        t =>
                            Q.CanCast(t) &&
                            (GetPoisonBuffEndTime(t) < Q.Delay * 1.2f ||
                             (HeroListManager.Check("q-fleeing", t) && BestTargetOnlyManager.Check("q-fleeing", Q, t) &&
                              !t.IsFacing(Player) && t.IsMoving && t.Distance(Player) > 150)));
                if (ts != null)
                {
                    _lastQPoisonDelay = Game.Time + Q.Delay;
                    _lastQPoisonT = ts;
                    Casting.SkillShot(ts, Q, hitChance);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void WLogic(HitChance hitChance)
        {
            try
            {
                var ts =
                    Targets.FirstOrDefault(
                        t =>
                            W.CanCast(t) &&
                            (_lastQPoisonDelay < Game.Time && GetPoisonBuffEndTime(t) < W.Delay * 1.2 ||
                             _lastQPoisonT == null || _lastQPoisonT.NetworkId != t.NetworkId ||
                             (HeroListManager.Check("w-fleeing", t) && BestTargetOnlyManager.Check("w-fleeing", W, t) &&
                              !t.IsFacing(Player) && t.IsMoving && t.Distance(Player) > 150)));
                if (ts != null)
                {
                    Casting.SkillShot(ts, W, hitChance);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void ELogic()
        {
            try
            {
                if (!DelayManager.Check("e-delay", _lastECast))
                {
                    return;
                }
                var ts = Targets.FirstOrDefault(t => E.CanCast(t) && GetPoisonBuffEndTime(t) > E.ArrivalTime(t));
                if (ts != null)
                {
                    var pred = E.GetPrediction(ts, false, -1f, new[] { CollisionableObjects.YasuoWall });
                    if (pred.Hitchance != HitChance.Collision)
                    {
                        _lastECast = Environment.TickCount;
                        E.Cast(ts);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Harass()
        {
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>())
            {
                QLogic(Q.GetHitChance("harass"));
            }
            if (Menu.Item(Menu.Name + ".harass.w").GetValue<bool>())
            {
                WLogic(W.GetHitChance("harass"));
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && ResourceManager.Check("harass"))
            {
                ELogic();
            }
        }

        private float GetPoisonBuffEndTime(Obj_AI_Base target)
        {
            try
            {
                var buffEndTime =
                    target.Buffs.Where(buff => buff.Type == BuffType.Poison)
                        .OrderByDescending(buff => buff.EndTime - Game.Time)
                        .Select(buff => buff.EndTime)
                        .DefaultIfEmpty(0)
                        .Max();
                return buffEndTime;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        protected override void LaneClear()
        {
            if (!ResourceManager.Check("lane-clear"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var useW = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady() &&
                       DelayManager.Check("e-delay", _lastECast);

            if (useE)
            {
                var minion =
                    MinionManager.GetMinions(Player.ServerPosition, E.Range)
                        .FirstOrDefault(
                            e =>
                                GetPoisonBuffEndTime(e) > E.ArrivalTime(e) &&
                                (e.Health > E.GetDamage(e) * 2 || e.Health < E.GetDamage(e) - 5));
                if (minion != null)
                {
                    _lastEEndTime = Game.Time + E.ArrivalTime(minion) + 0.1f;
                    _lastECast = Environment.TickCount;
                    Casting.TargetSkill(minion, E);
                }
            }

            if (useQ || useW)
            {
                var minions =
                    MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width)
                        .Where(e => GetPoisonBuffEndTime(e) < Q.Delay * 1.1)
                        .ToList();
                if (minions.Any())
                {
                    if (useQ)
                    {
                        var prediction = Q.GetCircularFarmLocation(minions, Q.Width + 30);
                        if (prediction.MinionsHit > 1 && Game.Time > _lastPoisonClearDelay ||
                            _lastPoisonClearPosition.Distance(prediction.Position) > W.Width * 1.1f)
                        {
                            var mP =
                                minions.Count(
                                    p =>
                                        p.Distance(prediction.Position) < Q.Width + 30 &&
                                        GetPoisonBuffEndTime(p) >= 0.5f);
                            if (prediction.MinionsHit - mP > 1)
                            {
                                _lastPoisonClearDelay = Game.Time + Q.Delay + 1;
                                _lastPoisonClearPosition = prediction.Position;
                                Q.Cast(prediction.Position);
                            }
                        }
                    }
                    if (useW)
                    {
                        var prediction = W.GetCircularFarmLocation(minions, W.Width + 50);
                        if (prediction.MinionsHit > 2 &&
                            (Game.Time > _lastPoisonClearDelay ||
                             _lastPoisonClearPosition.Distance(prediction.Position) > Q.Width * 1.1f))
                        {
                            var mP =
                                minions.Count(
                                    p =>
                                        p.Distance(prediction.Position) < W.Width + 50 &&
                                        GetPoisonBuffEndTime(p) >= 0.5f);
                            if (prediction.MinionsHit - mP > 1)
                            {
                                _lastPoisonClearDelay = Game.Time + W.Delay + 2;
                                _lastPoisonClearPosition = prediction.Position;
                                W.Cast(prediction.Position);
                            }
                        }
                    }
                }
            }
        }

        protected override void JungleClear()
        {
            if (!ResourceManager.Check("jungle-clear"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".jungle-clear.q").GetValue<bool>() && Q.IsReady();
            var useW = Menu.Item(Menu.Name + ".jungle-clear.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".jungle-clear.e").GetValue<bool>() && E.IsReady() &&
                       DelayManager.Check("e-delay", _lastECast);

            if (useE)
            {
                var minion =
                    MinionManager.GetMinions(
                        Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(e => GetPoisonBuffEndTime(e) > E.ArrivalTime(e));
                if (minion != null)
                {
                    _lastEEndTime = Game.Time + E.ArrivalTime(minion) + 0.1f;
                    _lastECast = Environment.TickCount;
                    Casting.TargetSkill(minion, E);
                }
            }

            if (useQ || useW)
            {
                var minions =
                    MinionManager.GetMinions(
                        Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth).Where(e => GetPoisonBuffEndTime(e) < Q.Delay * 1.1).ToList();
                if (minions.Any())
                {
                    if (useQ)
                    {
                        var prediction = Q.GetCircularFarmLocation(minions, Q.Width + 30);
                        if (prediction.MinionsHit >= 1 && Game.Time > _lastPoisonClearDelay ||
                            _lastPoisonClearPosition.Distance(prediction.Position) > W.Width * 1.1f)
                        {
                            var mP =
                                minions.Count(
                                    p =>
                                        p.Distance(prediction.Position) < Q.Width + 30 &&
                                        GetPoisonBuffEndTime(p) >= 0.5f);
                            if (prediction.MinionsHit - mP > 1)
                            {
                                _lastPoisonClearDelay = Game.Time + Q.Delay + 1;
                                _lastPoisonClearPosition = prediction.Position;
                                Q.Cast(prediction.Position);
                            }
                        }
                    }
                    if (useW)
                    {
                        var prediction = W.GetCircularFarmLocation(minions, W.Width + 50);
                        if (prediction.MinionsHit >= 2 &&
                            (Game.Time > _lastPoisonClearDelay ||
                             _lastPoisonClearPosition.Distance(prediction.Position) > Q.Width * 1.1f))
                        {
                            var mP =
                                minions.Count(
                                    p =>
                                        p.Distance(prediction.Position) < W.Width + 50 &&
                                        GetPoisonBuffEndTime(p) >= 0.5f);
                            if (prediction.MinionsHit - mP > 1)
                            {
                                _lastPoisonClearDelay = Game.Time + W.Delay + 2;
                                _lastPoisonClearPosition = prediction.Position;
                                W.Cast(prediction.Position);
                            }
                        }
                    }
                }
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() && W.IsReady())
            {
                var near = GameObjects.EnemyHeroes.OrderBy(e => e.Distance(Player.Position)).FirstOrDefault();
                if (near != null)
                {
                    var pred = W.GetPrediction(near, true);
                    if (pred.Hitchance >= W.GetHitChance("harass"))
                    {
                        W.Cast(
                            Player.Position.Extend(
                                pred.CastPosition, Player.Position.Distance(pred.CastPosition) * 0.8f));
                    }
                }
            }
        }

        protected override void Killsteal()
        {
            var ePoison = Menu.Item(Menu.Name + ".killsteal.e-poison").GetValue<bool>();
            var eHit = Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>();
            if (ePoison || eHit && E.IsReady())
            {
                var m =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e =>
                            E.CanCast(e) && e.Health < E.GetDamage(e) - 5 &&
                            (ePoison && GetPoisonBuffEndTime(e) > E.ArrivalTime(e) || eHit));
                if (m != null)
                {
                    Casting.TargetSkill(m, E);
                }
            }
        }
    }
}