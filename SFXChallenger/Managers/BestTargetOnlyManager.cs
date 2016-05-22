#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 BestTargetOnlyManager.cs is part of SFXChallenger.

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
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;

#endregion

namespace SFXChallenger.Managers
{
    public class BestTargetOnlyManager
    {
        private static readonly Dictionary<string, Menu> Menues = new Dictionary<string, Menu>();

        public static void AddToMenu(Menu menu, string uniqueId, bool value = false)
        {
            try
            {
                if (Menues.ContainsKey(uniqueId))
                {
                    throw new ArgumentException(
                        string.Format("BestTargetManager: UniqueID \"{0}\" already exist.", uniqueId));
                }

                menu.AddItem(new MenuItem(menu.Name + ".best-target-" + uniqueId + ".enabled", "Only Selected Target"))
                    .SetValue(value);

                Menues[uniqueId] = menu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool Enabled(string uniqueId)
        {
            try
            {
                Menu menu;
                if (Menues.TryGetValue(uniqueId, out menu))
                {
                    return menu.Item(menu.Name + ".best-target-" + uniqueId + ".enabled").GetValue<bool>();
                }
                throw new KeyNotFoundException(
                    string.Format("BestTargetManager: UniqueID \"{0}\" not found.", uniqueId));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static bool Check(string uniqueId, Spell spell, Obj_AI_Hero hero)
        {
            try
            {
                if (hero == null || !Enabled(uniqueId))
                {
                    return true;
                }
                var bestTarget = TargetSelector.GetTarget(spell);
                if (bestTarget == null || hero.NetworkId.Equals(bestTarget.NetworkId))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }
    }
}