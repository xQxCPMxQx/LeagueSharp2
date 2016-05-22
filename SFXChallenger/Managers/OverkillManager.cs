#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 OverkillManager.cs is part of SFXChallenger.

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
using System.Collections.Concurrent;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public class OverkillManager
    {
        public static bool Enabled { get; set; }

        public static void AddToMenu(Menu menu)
        {
            try
            {
                menu.AddItem(new MenuItem(menu.Name + ".enabled", "Enabled").SetValue(false)).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args) { Enabled = args.GetNewValue<bool>(); };

                Enabled = menu.Item(menu.Name + ".enabled").GetValue<bool>();

                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                Spellbook.OnCastSpell += OnSpellbookCastSpell;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                if (sender.Owner.IsMe)
                {
                    var target = args.Target as Obj_AI_Hero;
                    if (target != null)
                    {
                        switch (args.Slot)
                        {
                            case SpellSlot.Q:
                            case SpellSlot.W:
                            case SpellSlot.E:
                            case SpellSlot.R:
                                Damages.Clean();
                                if (Damages.IsDying(target))
                                {
                                    args.Process = false;
                                }
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

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                if (args.Target != null && args.Target.IsEnemy)
                {
                    var target = args.Target as Obj_AI_Hero;
                    if (target != null)
                    {
                        if (!(sender is Obj_AI_Hero) || args.SData.IsAutoAttack())
                        {
                            Damages.Add(
                                target.NetworkId,
                                target.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed +
                                Game.Time, (float) sender.GetAutoAttackDamage(target));
                        }
                        else
                        {
                            var slot = target.GetSpellSlot(args.SData.Name);
                            if (slot != SpellSlot.Unknown && slot == target.GetSpellSlot("SummonerDot"))
                            {
                                Damages.Add(
                                    target.NetworkId, Game.Time + 2,
                                    (float) target.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite));
                            }
                        }
                    }
                }
                var hero = sender as Obj_AI_Hero;
                if (hero != null && hero.IsAlly)
                {
                    var slot = hero.GetSpellSlot(args.SData.Name);
                    if (slot != SpellSlot.Unknown)
                    {
                        if (slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R)
                        {
                            if (args.Target != null && args.Target.IsEnemy)
                            {
                                Damages.Add(
                                    args.Target.NetworkId, Game.Time + 1,
                                    (float) hero.GetSpellDamage(args.Target as Obj_AI_Hero, slot));
                            }
                            else if (args.Target == null)
                            {
                                foreach (var enemy in
                                    GameObjects.EnemyHeroes.Where(
                                        e => e.IsValidTarget() && e.Distance(args.Start) < 300))
                                {
                                    var length = (int) args.Start.Distance(args.End);
                                    for (int i = 0, l = length > 300 ? 300 : length; i < l; i = i + 50)
                                    {
                                        var pos = args.Start.Extend(args.End, i);
                                        if (enemy.Distance(pos) <= 50)
                                        {
                                            Damages.Add(
                                                enemy.NetworkId, Game.Time + 1, (float) hero.GetSpellDamage(enemy, slot));
                                            break;
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

        internal class Damages
        {
            private static readonly ConcurrentDictionary<int, ConcurrentDictionary<float, float>> Units =
                new ConcurrentDictionary<int, ConcurrentDictionary<float, float>>();

            public static float TotalDamage(int networkId)
            {
                try
                {
                    ConcurrentDictionary<float, float> unit;
                    if (Units.TryGetValue(networkId, out unit))
                    {
                        return unit.Where(e => e.Key >= Game.Time).Select(e => e.Value).DefaultIfEmpty(0).Sum();
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return 0;
            }

            public static void Clean()
            {
                try
                {
                    foreach (var unit in Units)
                    {
                        var damages = unit.Value.Where(entry => entry.Key < Game.Time).ToArray();
                        foreach (var damage in damages)
                        {
                            float old;
                            Units[unit.Key].TryRemove(damage.Key, out old);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public static void Add(int networkId, float time, float damage)
            {
                try
                {
                    if (!Units.ContainsKey(networkId))
                    {
                        Units[networkId] = new ConcurrentDictionary<float, float>();
                    }
                    ConcurrentDictionary<float, float> unit;
                    if (Units.TryGetValue(networkId, out unit))
                    {
                        float value;
                        if (unit.TryGetValue(time, out value))
                        {
                            unit[time] = value + damage;
                        }
                        else
                        {
                            unit[time] = damage;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public static bool IsDying(Obj_AI_Hero target)
            {
                try
                {
                    return TotalDamage(target.NetworkId) * 0.8f > target.Health + target.HPRegenRate;
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return false;
            }
        }
    }
}