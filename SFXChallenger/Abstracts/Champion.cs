#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Champion.cs is part of SFXChallenger.

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
using SFXChallenger.Enumerations;
using SFXChallenger.Interfaces;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SFXChallenger.Menus;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.SFXTargetSelector.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;

#endregion

namespace SFXChallenger.Abstracts
{
    internal abstract class Champion : IChampion
    {
        private static float _minionSearchRange;
        protected readonly Obj_AI_Hero Player;
        private Obj_AI_Base _nearestMinion;
        private List<Spell> _spells;
        private bool _useMuramana;
        protected Spell E;
        protected Spell Q;
        protected Spell R;
        protected UltimateManager Ultimate;
        protected Spell W;

        protected Champion()
        {
            Player = ObjectManager.Player;
            Core.OnBoot += OnCoreBoot;
        }

        protected abstract ItemFlags ItemFlags { get; }
        protected abstract ItemUsageType ItemUsage { get; }
        public Menu SFXMenu { get; private set; }

        public List<Spell> Spells
        {
            get { return _spells ?? (_spells = new List<Spell> { Q, W, E, R }); }
        }

        public Menu Menu { get; private set; }
        public Orbwalking.Orbwalker Orbwalker { get; private set; }

        void IChampion.Combo()
        {
            try
            {
                Combo();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.Harass()
        {
            try
            {
                Harass();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.LaneClear()
        {
            try
            {
                if (_nearestMinion == null || !_nearestMinion.IsValid || _nearestMinion.Team != Player.Team)
                {
                    LaneClear();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.JungleClear()
        {
            try
            {
                if (_nearestMinion == null || !_nearestMinion.IsValid || _nearestMinion.Team == GameObjectTeam.Neutral)
                {
                    JungleClear();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.Flee()
        {
            try
            {
                Orbwalker.SetAttack(false);
                Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                Flee();
                Utility.DelayAction.Add(
                    750, delegate
                    {
                        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee)
                        {
                            ItemManager.UseFleeItems();
                        }
                    });
                Utility.DelayAction.Add(
                    125, delegate
                    {
                        if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Flee)
                        {
                            Orbwalker.SetAttack(true);
                        }
                    });
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.Killsteal()
        {
            try
            {
                Killsteal();
                KillstealManager.Killsteal();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected virtual void OnCorePreUpdate(EventArgs args)
        {
            try
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    _nearestMinion =
                        MinionManager.GetMinions(
                            _minionSearchRange, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.None)
                            .OrderBy(m => m.Distance(Player))
                            .FirstOrDefault();
                }
                if (!_useMuramana)
                {
                    ItemManager.Muramana(null, false);
                }
                if (_useMuramana && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    ItemManager.Muramana(null, true, float.MaxValue);
                    Utility.DelayAction.Add(1000 + Game.Ping, () => _useMuramana = false);
                }
                OnPreUpdate();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected virtual void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                OnPostUpdate();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected void ItemsSummonersLogic(Obj_AI_Hero ultimateTarget, bool single = true)
        {
            try
            {
                var range = Math.Max(
                    600,
                    Math.Max(
                        SummonerManager.SummonerSpells.Where(s => s.CastType == CastType.Target).Max(s => s.Range),
                        ItemManager.Items.Where(
                            i => i.EffectFlags.HasFlag(EffectFlags.Damage) && i.Flags.HasFlag(ItemFlags.Offensive))
                            .Max(i => i.Range)));
                if (ultimateTarget == null || Ultimate == null || !ultimateTarget.IsValidTarget(range))
                {
                    var target = TargetSelector.GetTarget(range);
                    if (target != null)
                    {
                        if (ItemManager.CalculateComboDamage(target) + SummonerManager.CalculateComboDamage(target) >
                            target.Health)
                        {
                            ItemManager.UseComboItems(target);
                            SummonerManager.UseComboSummoners(target);
                        }
                    }
                }
                else
                {
                    if (Ultimate.GetDamage(ultimateTarget, UltimateModeType.Combo, single ? 1 : 5) >
                        ultimateTarget.Health)
                    {
                        ItemManager.UseComboItems(ultimateTarget);
                        SummonerManager.UseComboSummoners(ultimateTarget);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected abstract void OnLoad();
        protected abstract void SetupSpells();
        protected abstract void AddToMenu();
        protected abstract void OnPreUpdate();
        protected abstract void OnPostUpdate();
        protected abstract void Combo();
        protected abstract void Harass();
        protected abstract void LaneClear();
        protected abstract void JungleClear();
        protected abstract void Flee();
        protected abstract void Killsteal();

        private void OnCoreBoot(EventArgs args)
        {
            try
            {
                OnLoad();
                SetupSpells();
                SetupMenu();

                _minionSearchRange = Math.Min(
                    2000,
                    Math.Max(
                        ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2,
                        Spells.Select(spell => spell.IsChargedSpell ? spell.ChargedMaxRange : spell.Range)
                            .Concat(new[] { _minionSearchRange })
                            .Max()));

                TargetSelector.Weights.Range = Math.Max(
                    TargetSelector.Weights.Range,
                    Spells.Select(e => e.Range).DefaultIfEmpty(Orbwalking.GetRealAutoAttackRange(null) * 1.2f).Max());

                Core.OnPreUpdate += OnCorePreUpdate;
                Core.OnPostUpdate += OnCorePostUpdate;

                Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
                Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;

                Drawing.OnDraw += OnDrawingDraw;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                DrawingManager.Draw();
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
                if (unit.IsMe)
                {
                    if (ItemUsage == ItemUsageType.AfterAttack)
                    {
                        Orbwalker.ForceTarget(null);
                        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                        {
                            var enemy = target as Obj_AI_Hero;
                            if (enemy != null &&
                                (Ultimate == null || Ultimate.GetDamage(enemy, UltimateModeType.Combo, 1) > enemy.Health))
                            {
                                ItemManager.UseComboItems(enemy);
                                SummonerManager.UseComboSummoners(enemy);
                            }
                        }
                    }
                    if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                    {
                        ItemManager.Muramana(null, false);
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
                if (args.Unit.IsMe)
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        var enemy = args.Target as Obj_AI_Hero;
                        if (enemy != null)
                        {
                            ItemManager.Muramana(enemy, true);
                        }
                    }
                    else
                    {
                        ItemManager.Muramana(null, false);
                    }
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
                if (sender.IsMe && !args.SData.IsAutoAttack())
                {
                    var slot = Player.GetSpellSlot(args.SData.Name);
                    if (args.Target is Obj_AI_Hero || slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E ||
                        slot == SpellSlot.R)
                    {
                        _useMuramana = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void SetupMenu()
        {
            try
            {
                SFXMenu = new Menu(Global.Name, "sfx", true);

                Menu = new Menu(Global.Prefix + Player.ChampionName, SFXMenu.Name + "." + Player.ChampionName, true);

                DrawingManager.AddToMenu(Menu.AddSubMenu(new Menu("Drawings", Menu.Name + ".drawing")), this);

                TargetSelector.AddToMenu(SFXMenu.AddSubMenu(new Menu("Target Selector", SFXMenu.Name + ".ts")));

                Orbwalker = new Orbwalking.Orbwalker(SFXMenu.AddSubMenu(new Menu("Orbwalker", SFXMenu.Name + ".orb")));

                KillstealManager.AddToMenu(SFXMenu.AddSubMenu(new Menu("Killsteal", SFXMenu.Name + ".killsteal")));

                var itemMenu = SFXMenu.AddSubMenu(new Menu("Items", SFXMenu.Name + ".items"));
                TearStackManager.AddToMenu(
                    itemMenu.AddSubMenu(new Menu("Tear Stacking", SFXMenu.Name + ".tear-stack." + Player.ChampionName)),
                    Spells);
                ItemManager.AddToMenu(itemMenu, ItemFlags);
                SummonerManager.AddToMenu(SFXMenu.AddSubMenu(new Menu("Summoners", SFXMenu.Name + ".summoners")));

                InfoMenu.AddToMenu(SFXMenu.AddSubMenu(new Menu("Info", SFXMenu.Name + ".info")));
                DebugMenu.AddToMenu(SFXMenu, Spells);

                Menu.AddToMainMenu();
                SFXMenu.AddToMainMenu();

                try
                {
                    AddToMenu();
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}