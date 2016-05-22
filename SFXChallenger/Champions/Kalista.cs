#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Kalista.cs is part of SFXChallenger.

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
using SFXChallenger.Library.Extensions.SharpDX;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SFXChallenger.SFXTargetSelector.Others;
using SharpDX;
using SharpDX.Direct3D9;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.SFXTargetSelector.Orbwalking;
using ResourceManager = SFXChallenger.Managers.ResourceManager;
using ResourceType = SFXChallenger.Enumerations.ResourceType;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Kalista : Champion
    {
        private MenuItem _ePercent;
        private Font _font;
        private float _lastECast;
        private Obj_AI_Hero _soulbound;

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
            _font = MDrawing.GetFont(23);

            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            Orbwalking.OnNonKillableMinion += OnOrbwalkingNonKillableMinion;
            Drawing.OnDraw += OnDrawingDraw;

            IncomingDamageManager.RemoveDelay = 500;
            IncomingDamageManager.Skillshots = true;
            IncomingDamageManager.AddChampion(Player);

            CheckSoulbound();
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.25f, 40f, 1200f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 5000f);

            E = new Spell(SpellSlot.E, 950f);

            R = new Spell(SpellSlot.R, 1200f);
        }

        protected override void AddToMenu()
        {
            var ultimateMenu = Menu.AddSubMenu(new Menu("Ultimate", Menu.Name + ".ultimate"));

            var blitzMenu = ultimateMenu.AddSubMenu(new Menu("Blitzcrank", ultimateMenu.Name + ".blitzcrank"));
            HeroListManager.AddToMenu(
                blitzMenu.AddSubMenu(new Menu("Blacklist", blitzMenu.Name + ".blacklist")),
                new HeroListManagerArgs("blitzcrank")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false,
                    EnabledButton = false
                });
            blitzMenu.AddItem(new MenuItem(blitzMenu.Name + ".r", "Enabled").SetValue(true));

            var tahmMenu = ultimateMenu.AddSubMenu(new Menu("Tahm Kench", ultimateMenu.Name + ".tahm-kench"));
            HeroListManager.AddToMenu(
                tahmMenu.AddSubMenu(new Menu("Blacklist", tahmMenu.Name + ".blacklist")),
                new HeroListManagerArgs("tahm-kench")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false,
                    EnabledButton = false
                });
            tahmMenu.AddItem(new MenuItem(tahmMenu.Name + ".r", "Enabled").SetValue(true));

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".save", "Save Mode").SetValue(
                    new StringList(new[] { "None", "Auto", "Min. Health %" }, 1))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    Utils.UpdateVisibleTag(ultimateMenu, 1, args.GetNewValue<StringList>().SelectedIndex == 2);
                };
            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".save-health", "Min. Health %").SetValue(new Slider(10, 1, 50)))
                .SetTag(1);

            Utils.UpdateVisibleTag(
                ultimateMenu, 1, Menu.Item(Menu.Name + ".ultimate.save").GetValue<StringList>().SelectedIndex == 2);

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh } });
            ResourceManager.AddToMenu(
                comboMenu,
                new ResourceManagerArgs(
                    "combo-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "Q",
                    DefaultValue = 10
                });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e-min", "E Fleeing Min.").SetValue(new Slider(8, 1, 20)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".minions", "Attack Minions").SetValue(false));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "Q",
                    DefaultValue = 30
                });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass-e", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "E",
                    DefaultValue = 30
                });
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", "Use E").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-min", "E Min.").SetValue(new Slider(4, 1, 20)));

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
            laneClearMenu.AddItem(
                new MenuItem(laneClearMenu.Name + ".q-min", "Q Min. Hits").SetValue(new Slider(3, 1, 5)));
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
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".q", "Use Q").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem(jungleClearMenu.Name + ".e", "Use E").SetValue(true));

            var lasthitMenu = Menu.AddSubMenu(new Menu("Last Hit", Menu.Name + ".lasthit"));
            ResourceManager.AddToMenu(
                lasthitMenu,
                new ResourceManagerArgs(
                    "lasthit", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e-siege", "E Siege Minion").SetValue(true));
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e-unkillable", "E Unkillable").SetValue(true));
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e-turret", "E Under Turret").SetValue(true));
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".separator", string.Empty));
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e-jungle", "E Jungle").SetValue(true));
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e-big", "E Dragon/Baron").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e", "Use E").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));
            ResourceManager.AddToMenu(
                miscMenu,
                new ResourceManagerArgs("misc", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "E",
                    DefaultValue = 30
                });
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-reset", "E Harass Reset").SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-death", "E Before Death").SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-baron", "Hotkey W Baron").SetValue(new KeyBind('J', KeyBindType.Press)));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-dragon", "Hotkey W Dragon").SetValue(
                    new KeyBind('K', KeyBindType.Press)));

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q, true, false);
            IndicatorManager.Add(W, true, false);
            IndicatorManager.Add("E", Rend.GetDamage);
            IndicatorManager.Finale();

            _ePercent = DrawingManager.Add("E Percent Damage", new Circle(false, Color.DodgerBlue));


            var lowHealthWeight = TargetSelector.Weights.GetItem("low-health");
            if (lowHealthWeight != null)
            {
                lowHealthWeight.ValueFunction = hero => hero.Health - Rend.GetDamage(hero);
                lowHealthWeight.Tooltip = "Low Health (Health - Rend Damage) = Higher Weight";
            }

            TargetSelector.Weights.Register(
                new TargetSelector.Weights.Item(
                    "w-stack", "W Stack", 10, false, hero => hero.HasBuff("kalistacoopstrikemarkally") ? 1 : 0,
                    "Has W Debuff = Higher Weight"));
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Player.IsDashing())
                {
                    args.Process = false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (args.SData.Name == "KalistaExpungeWrapper")
                    {
                        Orbwalking.ResetAutoAttackTimer();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingNonKillableMinion(AttackableUnit unit)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".lasthit.e-unkillable").GetValue<bool>() && E.IsReady() &&
                    ResourceManager.Check("lasthit"))
                {
                    var target = unit as Obj_AI_Base;
                    if (target != null && Rend.IsKillable(target, true))
                    {
                        CastE();
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
            if (E.IsReady())
            {
                if (Menu.Item(Menu.Name + ".miscellaneous.e-death").GetValue<bool>())
                {
                    if (IncomingDamageManager.GetDamage(Player) > Player.Health &&
                        GameObjects.EnemyHeroes.Any(e => e.IsValidTarget(E.Range) && Rend.HasBuff(e)))
                    {
                        CastE();
                    }
                }

                var eBig = Menu.Item(Menu.Name + ".lasthit.e-big").GetValue<bool>();
                var eJungle = Menu.Item(Menu.Name + ".lasthit.e-jungle").GetValue<bool>();
                if (eBig || eJungle)
                {
                    if (eJungle && Player.Level >= 3 || eBig)
                    {
                        var creeps =
                            GameObjects.Jungle.Where(e => e.IsValidTarget(E.Range) && Rend.IsKillable(e, false))
                                .ToList();
                        if (eJungle && creeps.Any() ||
                            eBig &&
                            creeps.Any(
                                m =>
                                    m.CharData.BaseSkinName.StartsWith("SRU_Dragon") ||
                                    m.CharData.BaseSkinName.StartsWith("SRU_Baron")))
                        {
                            CastE();
                            return;
                        }
                    }
                }

                var eSiege = (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                              Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit) &&
                             Menu.Item(Menu.Name + ".lasthit.e-siege").GetValue<bool>();
                var eTurret = (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                               Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit) &&
                              Menu.Item(Menu.Name + ".lasthit.e-turret").GetValue<bool>();
                var eReset = Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None &&
                             Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Flee &&
                             Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo &&
                             Menu.Item(Menu.Name + ".miscellaneous.e-reset").GetValue<bool>();

                IEnumerable<Obj_AI_Minion> minions = new HashSet<Obj_AI_Minion>();
                if (eSiege || eTurret || eReset)
                {
                    minions =
                        GameObjects.EnemyMinions.Where(
                            e => e.IsValidTarget(E.Range) && Rend.IsKillable(e, e.HealthPercent < 25));
                }

                if (ResourceManager.Check("lasthit"))
                {
                    if (eSiege)
                    {
                        if (
                            minions.Any(
                                m =>
                                    m.CharData.BaseSkinName.Contains("MinionSiege") ||
                                    m.CharData.BaseSkinName.Contains("Super")))
                        {
                            CastE();
                            return;
                        }
                    }
                    if (eTurret)
                    {
                        if (minions.Any(m => Utils.UnderAllyTurret(m.Position)))
                        {
                            CastE();
                            return;
                        }
                    }
                }

                if (eReset && minions.Any() && ResourceManager.Check("misc") &&
                    GameObjects.EnemyHeroes.Any(e => Rend.HasBuff(e) && e.IsValidTarget(E.Range)))
                {
                    CastE();
                    return;
                }
            }

            if (ShouldSave())
            {
                R.Cast();
            }
        }

        private void CastE()
        {
            if (Game.Time - _lastECast >= 1f)
            {
                _lastECast = Game.Time;
                E.Cast();
            }
        }

        private bool ShouldSave()
        {
            try
            {
                if (_soulbound != null && R.IsReady() && !_soulbound.InFountain())
                {
                    var mode = Menu.Item(Menu.Name + ".ultimate.save").GetValue<StringList>().SelectedIndex;
                    var enemies = _soulbound.CountEnemiesInRange(600);
                    switch (mode)
                    {
                        case 0:
                            return false;
                        case 1:
                            return enemies >= 1 &&
                                   _soulbound.HealthPercent <=
                                   Menu.Item(Menu.Name + ".ultimate.save-health").GetValue<Slider>().Value;
                        case 2:
                            return IncomingDamageManager.GetDamage(_soulbound) > _soulbound.Health ||
                                   _soulbound.HealthPercent <= 10 && enemies >= 1 ||
                                   _soulbound.HealthPercent <= enemies * 10f - 10f;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        protected override void OnPostUpdate()
        {
            CheckSoulbound();

            if (_soulbound != null && _soulbound.Distance(Player) < R.Range && R.IsReady())
            {
                var blitz = Menu.Item(Menu.Name + ".ultimate.blitzcrank.r").GetValue<bool>();
                var tahm = Menu.Item(Menu.Name + ".ultimate.tahm-kench.r").GetValue<bool>();
                foreach (var enemy in
                    GameObjects.EnemyHeroes.Where(e => (blitz || tahm) && !e.IsDead && e.Distance(Player) < 3000))
                {
                    if (blitz)
                    {
                        var blitzBuff =
                            enemy.Buffs.FirstOrDefault(
                                b =>
                                    b.IsActive && b.Caster.NetworkId.Equals(_soulbound.NetworkId) &&
                                    b.Name.Equals("rocketgrab2", StringComparison.OrdinalIgnoreCase));
                        if (blitzBuff != null)
                        {
                            if (!HeroListManager.Check("blitzcrank", enemy))
                            {
                                if (!_soulbound.UnderTurret(false) && _soulbound.Distance(enemy) > 750f &&
                                    _soulbound.Distance(Player) > R.Range / 3f)
                                {
                                    R.Cast();
                                }
                            }
                            return;
                        }
                    }
                    if (tahm)
                    {
                        var tahmBuff =
                            enemy.Buffs.FirstOrDefault(
                                b =>
                                    b.IsActive && b.Caster.NetworkId.Equals(_soulbound.NetworkId) &&
                                    b.Name.Equals("tahmkenchwdevoured", StringComparison.OrdinalIgnoreCase));
                        if (tahmBuff != null)
                        {
                            if (!HeroListManager.Check("tahm-kench", enemy))
                            {
                                if (!_soulbound.UnderTurret(false) &&
                                    (_soulbound.Distance(enemy) > Player.AttackRange ||
                                     GameObjects.AllyHeroes.Where(
                                         a => a.NetworkId != _soulbound.NetworkId && a.NetworkId != Player.NetworkId)
                                         .Any(t => t.Distance(Player) > 600) ||
                                     GameObjects.AllyTurrets.Any(t => t.Distance(Player) < 600)))
                                {
                                    R.Cast();
                                }
                            }
                            return;
                        }
                    }
                }
            }

            if (Menu.Item(Menu.Name + ".miscellaneous.w-baron").GetValue<KeyBind>().Active && W.IsReady() &&
                !Player.IsWindingUp && !Player.IsDashing() && Player.Distance(SummonersRift.River.Baron) <= W.Range)
            {
                W.Cast(SummonersRift.River.Baron);
            }
            if (Menu.Item(Menu.Name + ".miscellaneous.w-dragon").GetValue<KeyBind>().Active && W.IsReady() &&
                !Player.IsWindingUp && !Player.IsDashing() && Player.Distance(SummonersRift.River.Dragon) <= W.Range)
            {
                W.Cast(SummonersRift.River.Dragon);
            }
        }

        private void CheckSoulbound()
        {
            try
            {
                if (_soulbound == null)
                {
                    _soulbound =
                        GameObjects.AllyHeroes.FirstOrDefault(
                            a =>
                                a.Buffs.Any(
                                    b =>
                                        b.Caster.IsMe &&
                                        b.Name.Equals("kalistacoopstrikeally", StringComparison.OrdinalIgnoreCase)));
                    if (_soulbound != null)
                    {
                        IncomingDamageManager.AddChampion(_soulbound);
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
            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady() &&
                       ResourceManager.Check("combo-q");
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();

            if (useQ && !Player.IsWindingUp && !Player.IsDashing())
            {
                var target = TargetSelector.GetTarget(Q);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.GetHitChance("combo"))
                    {
                        Q.Cast(prediction.CastPosition);
                    }
                    else if (prediction.Hitchance == HitChance.Collision)
                    {
                        QCollisionCheck(target);
                    }
                }
            }

            var dashObjects = new List<Obj_AI_Base>();
            if (useE)
            {
                var target = TargetSelector.GetTarget(E, false);
                if (target != null && Rend.HasBuff(target))
                {
                    if (Rend.IsKillable(target, false))
                    {
                        CastE();
                    }
                    if (target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(target))
                    {
                        if (
                            GameObjects.EnemyMinions.Any(
                                m => m.IsValidTarget(E.Range * 0.95f) && Rend.IsKillable(m, m.HealthPercent < 10)))
                        {
                            CastE();
                        }
                        else
                        {
                            dashObjects =
                                GameObjects.EnemyMinions.Where(
                                    m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(m)))
                                    .Select(e => e as Obj_AI_Base)
                                    .ToList();
                            var minion =
                                dashObjects.FirstOrDefault(
                                    m =>
                                        m.Health > Player.GetAutoAttackDamage(m) * 1.1f &&
                                        m.Health < Player.GetAutoAttackDamage(m) + Rend.GetDamage(m, 1));
                            if (minion != null)
                            {
                                Orbwalker.ForceTarget(minion);
                                if (Orbwalking.CanAttack())
                                {
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
                                }
                            }
                        }
                    }
                    else if (E.IsInRange(target))
                    {
                        if (Rend.IsKillable(target, false))
                        {
                            CastE();
                        }
                        else
                        {
                            var buff = Rend.GetBuff(target);
                            if (buff != null &&
                                buff.Count >= Menu.Item(Menu.Name + ".harass.e-min").GetValue<Slider>().Value)
                            {
                                if (target.Distance(Player) > E.Range * 0.8 && !target.IsFacing(Player) ||
                                    buff.EndTime - Game.Time < 0.3)
                                {
                                    CastE();
                                }
                            }
                        }
                    }
                }
            }

            if (Menu.Item(Menu.Name + ".combo.minions").GetValue<bool>() && !Player.IsWindingUp && !Player.IsDashing() &&
                !GameObjects.EnemyHeroes.Any(
                    e => e.IsValidTarget() && e.Distance(Player) < Orbwalking.GetRealAutoAttackRange(e) * 1.1f))
            {
                if (dashObjects.Count <= 0)
                {
                    dashObjects = GetDashObjects().ToList();
                }
                var minion = dashObjects.FirstOrDefault();
                if (minion != null)
                {
                    Orbwalker.ForceTarget(minion);
                    if (Orbwalking.CanAttack())
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
                    }
                }
            }
            else
            {
                Orbwalker.ForceTarget(null);
            }
        }

        protected override void Harass()
        {
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady() && ResourceManager.Check("harass-q") &&
                !Player.IsWindingUp && !Player.IsDashing())
            {
                var target = TargetSelector.GetTarget(Q);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.GetHitChance("harass"))
                    {
                        Q.Cast(prediction.CastPosition);
                    }
                    else if (prediction.Hitchance == HitChance.Collision)
                    {
                        QCollisionCheck(target);
                    }
                }
            }

            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && E.IsReady() && ResourceManager.Check("harass-e"))
            {
                var target = TargetSelector.GetTarget(E, false);
                if (target != null && Rend.HasBuff(target))
                {
                    if (Rend.IsKillable(target, false))
                    {
                        CastE();
                    }
                    if (target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(target))
                    {
                        if (
                            GameObjects.EnemyMinions.Any(
                                m => m.IsValidTarget(E.Range * 0.95f) && Rend.IsKillable(m, m.HealthPercent < 10)))
                        {
                            CastE();
                        }
                        else
                        {
                            var dashObjects =
                                GameObjects.EnemyMinions.Where(
                                    m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(m))).ToList();
                            var minion =
                                dashObjects.FirstOrDefault(
                                    m =>
                                        m.Health > Player.GetAutoAttackDamage(m) * 1.1f &&
                                        m.Health < Player.GetAutoAttackDamage(m) + Rend.GetDamage(m, 1));
                            if (minion != null)
                            {
                                Orbwalker.ForceTarget(minion);
                                if (Orbwalking.CanAttack())
                                {
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
                                }
                            }
                        }
                    }
                    else if (E.IsInRange(target))
                    {
                        if (Rend.IsKillable(target, false))
                        {
                            CastE();
                        }
                        else
                        {
                            var buff = Rend.GetBuff(target);
                            if (buff != null &&
                                buff.Count >= Menu.Item(Menu.Name + ".harass.e-min").GetValue<Slider>().Value)
                            {
                                if (target.Distance(Player) > E.Range * 0.8 && !target.IsFacing(Player) ||
                                    buff.EndTime - Game.Time < 0.3)
                                {
                                    CastE();
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<Obj_AI_Base> QGetCollisions(Obj_AI_Hero source, Vector3 targetposition)
        {
            try
            {
                var input = new PredictionInput { Unit = source, Radius = Q.Width, Delay = Q.Delay, Speed = Q.Speed };
                input.CollisionObjects[0] = CollisionableObjects.Minions;
                return
                    Collision.GetCollision(new List<Vector3> { targetposition }, input)
                        .OrderBy(obj => obj.Distance(source))
                        .ToList();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Obj_AI_Base>();
        }

        protected override void LaneClear()
        {
            if (!ResourceManager.Check("lane-clear"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady();

            if (!useQ && !useE)
            {
                return;
            }

            var minE = ItemData.Runaans_Hurricane_Ranged_Only.GetItem().IsOwned(Player) ? 3 : 2;
            var minQ = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;
            var minions = MinionManager.GetMinions(Q.Range);
            if (minions.Count == 0)
            {
                return;
            }
            if (useQ && minions.Count >= minQ && !Player.IsWindingUp && !Player.IsDashing())
            {
                foreach (var minion in minions.Where(x => x.Health <= Q.GetDamage(x)))
                {
                    var killcount = 0;
                    foreach (var colminion in
                        QGetCollisions(Player, Player.ServerPosition.Extend(minion.ServerPosition, Q.Range)))
                    {
                        if (colminion.Health <= Q.GetDamage(colminion))
                        {
                            killcount++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (killcount >= minQ)
                    {
                        Q.Cast(minion.ServerPosition);
                        break;
                    }
                }
            }
            if (useE)
            {
                var killable = minions.Where(m => E.IsInRange(m) && Rend.IsKillable(m, false)).ToList();
                if (killable.Count >= minE)
                {
                    CastE();
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
            var useE = Menu.Item(Menu.Name + ".jungle-clear.e").GetValue<bool>() && E.IsReady();

            if (!useQ && !useE)
            {
                return;
            }

            var minions = MinionManager.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
            {
                return;
            }
            if (useQ && minions.Count >= 1 && !Player.IsWindingUp && !Player.IsDashing())
            {
                foreach (var minion in minions.Where(x => x.Health <= Q.GetDamage(x)))
                {
                    var killcount = 0;
                    foreach (var colminion in
                        QGetCollisions(Player, Player.ServerPosition.Extend(minion.ServerPosition, Q.Range)))
                    {
                        if (colminion.Health <= Q.GetDamage(colminion))
                        {
                            killcount++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (killcount >= 1)
                    {
                        Q.Cast(minion.ServerPosition);
                        break;
                    }
                }
            }
            if (useE)
            {
                var killable = minions.Where(m => E.IsInRange(m) && Rend.IsKillable(m, false)).ToList();
                if (killable.Count >= 1)
                {
                    CastE();
                }
            }
        }

        protected override void Flee()
        {
            Orbwalker.SetAttack(true);
            var dashObjects = GetDashObjects();
            if (dashObjects != null && dashObjects.Any())
            {
                Orbwalking.Orbwalk(dashObjects.First(), Game.CursorPos);
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>() && E.IsReady() &&
                GameObjects.EnemyHeroes.Any(h => h.IsValidTarget(E.Range) && Rend.IsKillable(h, false)))
            {
                CastE();
            }
        }

        private void QCollisionCheck(Obj_AI_Hero target)
        {
            var minions = MinionManager.GetMinions(Q.Range);

            if (minions.Count < 1 || Player.IsWindingUp || Player.IsDashing())
            {
                return;
            }

            foreach (var minion in minions)
            {
                var difference = Player.Distance(target) - Player.Distance(minion);
                for (var i = 0; i < difference; i += (int) target.BoundingRadius)
                {
                    var point = minion.ServerPosition.To2D().Extend(Player.ServerPosition.To2D(), -i).To3D();
                    var time = Q.Delay + ObjectManager.Player.Distance(point) / Q.Speed * 1000f;

                    var prediction = Prediction.GetPrediction(target, time);

                    var collision = Q.GetCollision(point.To2D(), new List<Vector2> { prediction.UnitPosition.To2D() });

                    if (collision.Any(x => x.Health > Q.GetDamage(x)))
                    {
                        return;
                    }

                    if (prediction.UnitPosition.Distance(point) <= Q.Width &&
                        !minions.Any(m => m.Distance(point) <= Q.Width))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }

        public IOrderedEnumerable<Obj_AI_Base> GetDashObjects()
        {
            try
            {
                var objects =
                    GameObjects.EnemyMinions.Concat(GameObjects.Jungle)
                        .Where(o => o.IsValidTarget(Orbwalking.GetRealAutoAttackRange(o)))
                        .Select(o => o as Obj_AI_Base)
                        .ToList();
                var apexPoint = Player.ServerPosition.To2D() +
                                (Player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() *
                                Orbwalking.GetRealAutoAttackRange(Player);
                return
                    objects.Where(
                        o =>
                            Utils.IsLyingInCone(
                                o.ServerPosition.To2D(), apexPoint, Player.ServerPosition.To2D(), Math.PI))
                        .OrderBy(o => o.Distance(apexPoint, true));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public List<Obj_AI_Base> GetDashObjects(List<Obj_AI_Base> targets)
        {
            try
            {
                var apexPoint = Player.ServerPosition.To2D() +
                                (Player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() *
                                Orbwalking.GetRealAutoAttackRange(Player);

                return
                    targets.Where(
                        o =>
                            Utils.IsLyingInCone(
                                o.ServerPosition.To2D(), apexPoint, Player.ServerPosition.To2D(), Math.PI))
                        .OrderBy(o => o.Distance(apexPoint, true))
                        .ToList();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        private void OnDrawingDraw(EventArgs args)
        {
            if (!Utils.ShouldDraw() || _ePercent == null)
            {
                return;
            }
            var ePercentCircle = _ePercent.GetValue<Circle>();
            if (ePercentCircle.Active && E.IsReady())
            {
                var sharpColor = new SharpDX.Color(
                    ePercentCircle.Color.R, ePercentCircle.Color.G, ePercentCircle.Color.B);
                var maxRange = E.Range * 1.5f;
                var targets = GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().Concat(GameObjects.Jungle);

                foreach (var enemy in
                    targets.Where(
                        e =>
                            e.IsValidTarget(maxRange) && e.Position.IsOnScreen() &&
                            (e is Obj_AI_Hero || Utils.IsBigJungle(e))))
                {
                    var damage = Rend.GetDamage(enemy);
                    if (damage > 0)
                    {
                        var percent = (int) (damage / enemy.Health * 100);
                        if (percent > 0)
                        {
                            var screen = Drawing.WorldToScreen(enemy.Position);
                            var position = enemy.Team == GameObjectTeam.Neutral
                                ? new Vector2(screen.X, screen.Y + 30)
                                : new Vector2(enemy.HPBarPosition.X + 73, enemy.HPBarPosition.Y - 28);
                            _font.DrawTextCentered(percent + " %", position, sharpColor);
                        }
                    }
                }
            }
        }

        internal class Rend
        {
            private static readonly float[] Damage = { 20, 30, 40, 50, 60 };
            private static readonly float[] DamageMultiplier = { 0.6f, 0.6f, 0.6f, 0.6f, 0.6f };
            private static readonly float[] DamagePerSpear = { 7, 12, 18, 25, 32 };
            private static readonly float[] DamagePerSpearMultiplier = { 0.175f, 0.2125f, 0.245f, 0.275f, 0.3f };

            public static bool IsKillable(Obj_AI_Base target, bool check)
            {
                try
                {
                    if (!target.IsValidTarget(1000))
                    {
                        return false;
                    }
                    if (check)
                    {
                        if (target.Health < 100 && target is Obj_AI_Minion)
                        {
                            if (HealthPrediction.GetHealthPrediction(target, 250 + Game.Ping / 2) <= 0)
                            {
                                return false;
                            }
                        }
                    }
                    var damage = GetDamage(target);
                    var hero = target as Obj_AI_Hero;
                    if (hero != null)
                    {
                        if (Invulnerable.Check(hero, damage, DamageType.Physical, false))
                        {
                            return false;
                        }
                    }
                    return damage > target.Health;
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return false;
            }

            private static float GetRealDamage(Obj_AI_Base target, float damage)
            {
                try
                {
                    if (ObjectManager.Player.HasBuff("summonerexhaust"))
                    {
                        damage *= 0.6f;
                    }
                    if (target is Obj_AI_Minion)
                    {
                        var dragonBuff =
                            ObjectManager.Player.Buffs.FirstOrDefault(
                                b => b.Name.Equals("s5test_dragonslayerbuff", StringComparison.OrdinalIgnoreCase));
                        if (dragonBuff != null)
                        {
                            if (dragonBuff.Count == 4)
                            {
                                damage *= 1.15f;
                            }
                            else if (dragonBuff.Count == 5)
                            {
                                damage *= 1.3f;
                            }
                            if (target.CharData.BaseSkinName.StartsWith("SRU_Dragon"))
                            {
                                damage *= 1f - 0.07f * dragonBuff.Count;
                            }
                        }
                        if (target.CharData.BaseSkinName.StartsWith("SRU_Baron"))
                        {
                            var baronBuff =
                                ObjectManager.Player.Buffs.FirstOrDefault(
                                    b => b.Name.Equals("barontarget", StringComparison.OrdinalIgnoreCase));
                            if (baronBuff != null)
                            {
                                damage *= 0.5f;
                            }
                        }
                        if (target.CharData.BaseSkinName.Contains("Siege"))
                        {
                            damage -= 5;
                        }
                        damage -= ObjectManager.Player.Level;
                    }
                    var hero = target as Obj_AI_Hero;
                    if (hero != null)
                    {
                        if (hero.HasBuff("FerociousHowl"))
                        {
                            damage *= 0.3f;
                        }
                        if (hero.HasBuff("GarenW"))
                        {
                            damage *= 0.7f;
                        }
                        if (hero.HasBuff("Medidate"))
                        {
                            damage *= 0.5f;
                        }
                        if (hero.HasBuff("gragaswself"))
                        {
                            damage *= 0.8f;
                        }
                        if (!hero.HasBuff("BlackShield"))
                        {
                            if (hero.AllShield > 0)
                            {
                                damage -= hero.AllShield;
                            }
                            else if (hero.ChampionName.Equals("Blitzcrank") && !hero.HasBuff("BlitzcrankManaBarrierCD") &&
                                     !hero.HasBuff("ManaBarrier"))
                            {
                                damage -= hero.Mana / 2f;
                            }
                        }
                        damage -= hero.HPRegenRate;
                        damage -= hero.Health / 150f;
                    }
                    return damage;
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return 0;
            }

            public static float GetDamage(Obj_AI_Hero target)
            {
                return GetDamage(target, -1);
            }

            public static float GetDamage(Obj_AI_Base target, int customStacks = -1)
            {
                return GetRealDamage(
                    target,
                    (float)
                        ObjectManager.Player.CalcDamage(
                            target, LeagueSharp.Common.Damage.DamageType.Physical, GetRawDamage(target, customStacks)));
            }

            public static float GetRawDamage(Obj_AI_Base target, int customStacks = -1)
            {
                try
                {
                    var buff = GetBuff(target);
                    var eLevel = ObjectManager.Player.GetSpell(SpellSlot.E).Level;
                    if (buff != null && buff.Count > 0 || customStacks > -1)
                    {
                        return Damage[eLevel - 1] +
                               DamageMultiplier[eLevel - 1] * ObjectManager.Player.TotalAttackDamage +
                               ((customStacks < 0 ? (buff == null ? 0 : buff.Count) : customStacks) - 1) *
                               (DamagePerSpear[eLevel - 1] +
                                DamagePerSpearMultiplier[eLevel - 1] * ObjectManager.Player.TotalAttackDamage);
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return 0f;
            }

            public static bool HasBuff(Obj_AI_Base target)
            {
                return GetBuff(target) != null;
            }

            public static BuffInstance GetBuff(Obj_AI_Base target)
            {
                return
                    target.Buffs.FirstOrDefault(
                        b =>
                            b.Caster.IsMe && b.IsValid &&
                            b.DisplayName.Equals("KalistaExpungeMarker", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}