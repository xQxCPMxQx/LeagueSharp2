#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TearStackManager.cs is part of SFXChallenger.

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
using MinionManager = SFXChallenger.Library.MinionManager;
using Spell = SFXChallenger.Wrappers.Spell;

#endregion

namespace SFXChallenger.Managers
{
    public static class TearStackManager
    {
        private static List<Spell> _spells;
        private static readonly Random Random;
        private static Menu _menu;
        private static int _lastTick;
        private static readonly int _interval = 350;

        static TearStackManager()
        {
            try
            {
                Random = new Random();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void AddToMenu(Menu menu, List<Spell> spells)
        {
            try
            {
                _menu = menu;

                spells =
                    spells.DistinctBy(s => s.Slot)
                        .Where(s => s.Slot != SpellSlot.Unknown && (s.IsSkillshot || s.Range > 0f))
                        .ToList();

                foreach (var spell in spells)
                {
                    _menu.AddItem(
                        new MenuItem(_menu.Name + "." + spell.Slot, "Use " + spell.Slot).SetValue(
                            spell.Slot != SpellSlot.R && spell.Instance.Cooldown < 20));
                }

                _menu.AddItem(
                    new MenuItem(_menu.Name + ".min-distance", "Min. Enemy Distance").SetValue(
                        new Slider(1000, 200, 3000)));
                _menu.AddItem(new MenuItem(_menu.Name + ".min-mana", "Min. Mana %").SetValue(new Slider(95, 1)));
                _menu.AddItem(new MenuItem(_menu.Name + ".fountain", "Only Inside Fountain").SetValue(false));
                _menu.AddItem(new MenuItem(_menu.Name + ".enabled", "Enabled").SetValue(false));

                _spells = spells.OrderBy(s => s.ManaCost).ThenBy(s => s.Instance.Cooldown).ToList();
                Core.OnPostUpdate += OnCorePostUpdate;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }

                if (Environment.TickCount - _lastTick >= _interval)
                {
                    _lastTick = Environment.TickCount;

                    if (_menu.Item(_menu.Name + ".fountain").GetValue<bool>() && !ObjectManager.Player.InFountain())
                    {
                        return;
                    }

                    if (ObjectManager.Player.ManaPercent >=
                        _menu.Item(_menu.Name + ".min-mana").GetValue<Slider>().Value)
                    {
                        var tearSlot = ObjectManager.Player.GetSpellSlot("TearsDummySpell");
                        if (tearSlot != SpellSlot.Unknown &&
                            Game.Time > ObjectManager.Player.GetSpell(tearSlot).CooldownExpires &&
                            ObjectManager.Player.CountEnemiesInRange(
                                _menu.Item(_menu.Name + ".min-distance").GetValue<Slider>().Value) <= 0)
                        {
                            var spell =
                                _spells.FirstOrDefault(
                                    s => s.IsReady() && _menu.Item(_menu.Name + "." + s.Slot).GetValue<bool>());
                            if (spell != null)
                            {
                                if (spell.IsSkillshot)
                                {
                                    var target =
                                        GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(spell.Range))
                                            .Concat(MinionManager.GetMinions(spell.Range))
                                            .FirstOrDefault();
                                    if (target != null)
                                    {
                                        spell.Cast(target.Position);
                                    }
                                    else
                                    {
                                        var position = ObjectManager.Player.Position.Extend(
                                            Game.CursorPos, Math.Min(1000, spell.Range * 0.8f + Random.Next(1, 26)));
                                        if (position.IsValid())
                                        {
                                            spell.Cast(position);
                                        }
                                    }
                                }
                                else if (spell.Range > 0f)
                                {
                                    if (spell.Speed.Equals(default(float)))
                                    {
                                        spell.Cast();
                                    }
                                    else
                                    {
                                        var target =
                                            GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(spell.Range))
                                                .Concat(MinionManager.GetMinions(spell.Range))
                                                .FirstOrDefault();
                                        if (target != null)
                                        {
                                            spell.Cast(target);
                                        }
                                    }
                                }
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