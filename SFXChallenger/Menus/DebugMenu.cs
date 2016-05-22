#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DebugMenu.cs is part of SFXChallenger.

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
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Helpers;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Menus
{
    internal class DebugMenu
    {
        private static readonly Dictionary<Spell, Spell> DefaultSpells = new Dictionary<Spell, Spell>();
        private static Menu _menu;
        private static readonly float MinMultiplicator = 0.5f;
        private static readonly float MaxMultiplicator = 1.5f;

        public static void AddToMenu(Menu menu, List<Wrappers.Spell> spells)
        {
            try
            {
                _menu = menu.AddSubMenu(new Menu("Debug", menu.Name + ".debug"));

                if (spells.All(s => s == null || s.Slot == SpellSlot.Unknown))
                {
                    return;
                }

                foreach (var spell in spells.Where(s => s != null && s.Slot != SpellSlot.Unknown))
                {
                    var lSpell = spell;
                    var range = spell.Range;
                    var width = spell.Width;
                    var delay = spell.Delay;
                    var speed = spell.Speed;
                    var duration = spell.ChargeDuration;
                    var minRange = spell.ChargedMinRange;
                    var maxRange = spell.ChargedMaxRange;

                    DefaultSpells[spell] = new Spell(spell.Slot, spell.Range)
                    {
                        Range = range,
                        Width = width,
                        Delay = delay,
                        Speed = speed,
                        ChargeDuration = duration,
                        ChargedMinRange = minRange,
                        ChargedMaxRange = maxRange
                    };

                    var spellMenu =
                        _menu.AddSubMenu(
                            new Menu(spell.Slot.ToString(), _menu.Name + ObjectManager.Player.ChampionName + spell.Slot));

                    if (Math.Abs(range - float.MaxValue) > 1 && range > 0)
                    {
                        spellMenu.AddItem(
                            new MenuItem(spellMenu.Name + ".range", "Range").SetValue(
                                new Slider(
                                    (int) range, (int) (range * MinMultiplicator), (int) (range * MaxMultiplicator)))
                                .DontSave()).ValueChanged +=
                            delegate(object sender, OnValueChangeEventArgs args)
                            {
                                lSpell.Range = args.GetNewValue<Slider>().Value;
                            };
                        spell.Range = _menu.Item(spellMenu.Name + ".range").GetValue<Slider>().Value;
                    }

                    if (Math.Abs(width - float.MaxValue) > 1 && width > 0)
                    {
                        spellMenu.AddItem(
                            new MenuItem(spellMenu.Name + ".width", "Width").SetValue(
                                new Slider(
                                    (int) width, (int) (width * MinMultiplicator), (int) (width * MaxMultiplicator)))
                                .DontSave()).ValueChanged +=
                            delegate(object sender, OnValueChangeEventArgs args)
                            {
                                lSpell.Width = args.GetNewValue<Slider>().Value;
                            };
                        spell.Width = _menu.Item(spellMenu.Name + ".width").GetValue<Slider>().Value;
                    }

                    if (Math.Abs(speed - float.MaxValue) > 1 && speed > 0)
                    {
                        spellMenu.AddItem(
                            new MenuItem(spellMenu.Name + ".speed", "Speed").SetValue(
                                new Slider(
                                    (int) speed, (int) (speed * MinMultiplicator), (int) (speed * MaxMultiplicator)))
                                .DontSave()).ValueChanged +=
                            delegate(object sender, OnValueChangeEventArgs args)
                            {
                                lSpell.Speed = args.GetNewValue<Slider>().Value;
                            };
                        spell.Speed = _menu.Item(spellMenu.Name + ".speed").GetValue<Slider>().Value;
                    }

                    spellMenu.AddItem(
                        new MenuItem(spellMenu.Name + ".delay", "Delay").SetValue(
                            new Slider(
                                (int) (delay * 1000), 0, (int) ((delay > 1 ? delay * MaxMultiplicator : 1) * 1000)))
                            .DontSave()).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            lSpell.Delay = args.GetNewValue<Slider>().Value / 1000f;
                        };
                    spell.Delay = _menu.Item(spellMenu.Name + ".delay").GetValue<Slider>().Value / 1000f;

                    if (spell.IsChargedSpell)
                    {
                        spellMenu.AddItem(
                            new MenuItem(spellMenu.Name + ".duration", "Duration").SetValue(
                                new Slider(
                                    duration, (int) (duration * MinMultiplicator), (int) (duration * MaxMultiplicator)))
                                .DontSave()).ValueChanged +=
                            delegate(object sender, OnValueChangeEventArgs args)
                            {
                                lSpell.ChargeDuration = args.GetNewValue<Slider>().Value;
                            };
                        spellMenu.AddItem(
                            new MenuItem(spellMenu.Name + ".min-range", "Min. Range").SetValue(
                                new Slider(
                                    minRange, (int) (minRange * MinMultiplicator), (int) (minRange * MaxMultiplicator)))
                                .DontSave()).ValueChanged +=
                            delegate(object sender, OnValueChangeEventArgs args)
                            {
                                lSpell.ChargedMinRange = args.GetNewValue<Slider>().Value;
                            };
                        spellMenu.AddItem(
                            new MenuItem(spellMenu.Name + ".max-range", "Max. Range").SetValue(
                                new Slider(
                                    maxRange, (int) (maxRange * MinMultiplicator), (int) (maxRange * MaxMultiplicator)))
                                .DontSave()).ValueChanged +=
                            delegate(object sender, OnValueChangeEventArgs args)
                            {
                                lSpell.ChargedMaxRange = args.GetNewValue<Slider>().Value;
                            };

                        spell.ChargeDuration = _menu.Item(spellMenu.Name + ".duration").GetValue<Slider>().Value;
                        spell.ChargedMinRange = _menu.Item(spellMenu.Name + ".min-range").GetValue<Slider>().Value;
                        spell.ChargedMaxRange = _menu.Item(spellMenu.Name + ".max-range").GetValue<Slider>().Value;
                    }
                }

                _menu.AddItem(new MenuItem(_menu.Name + ".tick", "Tick").SetValue(new Slider(50, 1, 300))).ValueChanged
                    +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        Core.SetInterval(args.GetNewValue<Slider>().Value);
                    };
                Core.SetInterval(_menu.Item(_menu.Name + ".tick").GetValue<Slider>().Value);

                _menu.AddItem(new MenuItem(_menu.Name + ".reset", "Reset").SetValue(false).DontSave()).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        try
                        {
                            if (args.GetNewValue<bool>())
                            {
                                Utility.DelayAction.Add(0, () => _menu.Item(_menu.Name + ".reset").SetValue(false));
                                foreach (var entry in DefaultSpells)
                                {
                                    entry.Key.Range = entry.Value.Range;
                                    entry.Key.Width = entry.Value.Width;
                                    entry.Key.Delay = entry.Value.Delay;
                                    entry.Key.Speed = entry.Value.Speed;
                                    entry.Key.ChargeDuration = entry.Value.ChargeDuration;
                                    entry.Key.ChargedMinRange = entry.Value.ChargedMinRange;
                                    entry.Key.ChargedMaxRange = entry.Value.ChargedMaxRange;

                                    var name = _menu.Name + ObjectManager.Player.ChampionName + entry.Key.Slot;
                                    if (Math.Abs(entry.Key.Range - float.MaxValue) > 1 && entry.Key.Range > 0)
                                    {
                                        _menu.Item(name + ".range")
                                            .SetValue(
                                                new Slider(
                                                    (int) entry.Key.Range, (int) (entry.Key.Range * MinMultiplicator),
                                                    (int) (entry.Key.Range * MaxMultiplicator)));
                                    }
                                    if (Math.Abs(entry.Key.Speed - float.MaxValue) > 1 && entry.Key.Speed > 0)
                                    {
                                        _menu.Item(name + ".speed")
                                            .SetValue(
                                                new Slider(
                                                    (int) entry.Key.Speed, (int) (entry.Key.Speed * MinMultiplicator),
                                                    (int) (entry.Key.Speed * MaxMultiplicator)));
                                    }
                                    if (Math.Abs(entry.Key.Width - float.MaxValue) > 1 && entry.Key.Width > 0)
                                    {
                                        _menu.Item(name + ".width")
                                            .SetValue(
                                                new Slider(
                                                    (int) entry.Key.Width, (int) (entry.Key.Width * MinMultiplicator),
                                                    (int) (entry.Key.Width * MaxMultiplicator)));
                                    }
                                    _menu.Item(name + ".delay")
                                        .SetValue(
                                            new Slider(
                                                (int) (entry.Key.Delay * 1000), 0,
                                                (int)
                                                    ((entry.Key.Delay > 1 ? entry.Key.Delay * MaxMultiplicator : 1) *
                                                     1000)));

                                    if (entry.Key.IsChargedSpell)
                                    {
                                        _menu.Item(name + ".duration")
                                            .SetValue(
                                                new Slider(
                                                    entry.Key.ChargeDuration,
                                                    (int) (entry.Key.ChargeDuration * MinMultiplicator),
                                                    (int) (entry.Key.ChargeDuration * MaxMultiplicator)));
                                        _menu.Item(name + ".min-range")
                                            .SetValue(
                                                new Slider(
                                                    entry.Key.ChargedMinRange,
                                                    (int) (entry.Key.ChargedMinRange * MinMultiplicator),
                                                    (int) (entry.Key.ChargedMinRange * MaxMultiplicator)));
                                        _menu.Item(name + ".max-range")
                                            .SetValue(
                                                new Slider(
                                                    entry.Key.ChargedMaxRange,
                                                    (int) (entry.Key.ChargedMaxRange * MinMultiplicator),
                                                    (int) (entry.Key.ChargedMaxRange * MaxMultiplicator)));
                                    }
                                }

                                _menu.Item(_menu.Name + ".tick").SetValue(new Slider(50, 1, 300));
                            }
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    };

                _menu.AddItem(new MenuItem(_menu.Name + ".report", "Generate Report").SetValue(false)).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        try
                        {
                            if (!args.GetNewValue<bool>())
                            {
                                return;
                            }
                            Utility.DelayAction.Add(0, () => _menu.Item(_menu.Name + ".report").SetValue(false));
                            File.WriteAllText(
                                Path.Combine(Global.BaseDir, string.Format("{0}.report.txt", Global.Name.ToLower())),
                                GenerateReport.Generate());
                            Notifications.AddNotification("Report Generated", 5000);
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}