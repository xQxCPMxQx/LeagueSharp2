#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 HitchanceManager.cs is part of SFXChallenger.

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
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Logger;
using Spell = SFXChallenger.Wrappers.Spell;

#endregion

namespace SFXChallenger.Managers
{
    public static class HitchanceManager
    {
        private const int MinHitChance = 3;
        private static readonly Dictionary<string, Menu> Menues = new Dictionary<string, Menu>();
        private static float _lastFlash;

        public static void AddToMenu(Menu menu, string uniqueId, Dictionary<string, HitChance> hitChances)
        {
            try
            {
                if (Menues.ContainsKey(uniqueId))
                {
                    throw new ArgumentException(
                        string.Format("HitchanceManager: UniqueID \"{0}\" already exist.", uniqueId));
                }

                var flashMenu = menu.AddSubMenu(new Menu("Flash", menu.Name + ".flash"));
                flashMenu.AddItem(new MenuItem(menu.Name + ".enabled", "Reduce Hitchance").SetValue(true));
                flashMenu.AddItem(new MenuItem(menu.Name + ".amount", "Amount").SetValue(new Slider(1, 0, 3)));
                flashMenu.AddItem(new MenuItem(menu.Name + ".time", "Seconds").SetValue(new Slider(3, 1, 10)));

                foreach (var hit in hitChances)
                {
                    menu.AddItem(
                        new MenuItem(menu.Name + "." + hit.Key.ToLower(), hit.Key.ToUpper()).SetValue(
                            new StringList(
                                new[] { "Medium", "High", "Very High" },
                                hit.Value == HitChance.Medium ? 0 : (hit.Value == HitChance.High ? 1 : 2))));
                }

                Menues[uniqueId] = menu;

                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(SummonerManager.Flash.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _lastFlash = Game.Time;
                }
            }
        }

        public static HitChance Get(string uniqueId, string slot)
        {
            try
            {
                Menu menu;
                if (Menues.TryGetValue(uniqueId, out menu))
                {
                    var hitChance = HitChance.VeryHigh;
                    var reduceBy = 0;
                    var timeDiff = Game.Time - _lastFlash;
                    if (timeDiff <= 15 && menu.Item(menu.Name + ".flash.enabled").GetValue<bool>())
                    {
                        if (timeDiff <= menu.Item(menu.Name + ".flash.time").GetValue<Slider>().Value)
                        {
                            reduceBy = menu.Item(menu.Name + ".flash.amount").GetValue<Slider>().Value;
                        }
                    }
                    switch (menu.Item(menu.Name + "." + slot.ToLower()).GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            hitChance = HitChance.Medium;
                            break;
                        case 1:
                            hitChance = HitChance.High;
                            break;
                        case 2:
                            hitChance = HitChance.VeryHigh;
                            break;
                    }
                    return (HitChance) Math.Max(MinHitChance, (int) hitChance - reduceBy);
                }
                throw new KeyNotFoundException($"HitchanceManager: UniqueID \"{uniqueId}\" not found.");
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return HitChance.High;
        }

        public static HitChance GetHitChance(this Spell spell, string uniqueId)
        {
            try
            {
                if (spell != null && spell.Slot != SpellSlot.Unknown)
                {
                    return Get(uniqueId, spell.Slot.ToString());
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return HitChance.High;
        }
    }
}