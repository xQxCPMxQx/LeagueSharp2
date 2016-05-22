#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ResourceManager.cs is part of SFXChallenger.

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
using SFXChallenger.Args;
using SFXChallenger.Enumerations;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public class ResourceManager
    {
        private static readonly Dictionary<string, Tuple<Menu, ResourceManagerArgs>> Menues =
            new Dictionary<string, Tuple<Menu, ResourceManagerArgs>>();

        public static void AddToMenu(Menu menu, ResourceManagerArgs args)
        {
            try
            {
                if (Menues.ContainsKey(args.UniqueId))
                {
                    throw new ArgumentException(
                        string.Format("ResourceManager: UniqueID \"{0}\" does already exist.", args.UniqueId));
                }

                var prefix = string.IsNullOrEmpty(args.Prefix) ? string.Empty : args.Prefix + " ";
                var checkTypeString = args.CheckType == ResourceCheckType.Maximum
                    ? "Max"
                    : (args.CheckType == ResourceCheckType.Minimum ? "Min" : string.Empty);
                var valueTypeString = args.ValueType == ResourceValueType.Percent ? "%" : string.Empty;

                if (args.Advanced)
                {
                    if (args.LevelRanges.Count <= 0 || args.DefaultValues.Count <= 0)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "ResourceManager: \"{0}\" doesn't contain any \"LevelRanges\" or \"DefaultValues\".",
                                args.UniqueId));
                    }
                    if (args.LevelRanges.Count != args.DefaultValues.Count)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "ResourceManager: \"{0}\" \"LevelRanges\" and \"DefaultValues\" doesn't match.",
                                args.UniqueId));
                    }
                    var subMenu =
                        menu.AddSubMenu(
                            new Menu(
                                string.Format("{0}{1} Settings", prefix, args.Type),
                                string.Format("{0}.{1}-{2}", menu.Name, args.Type, args.UniqueId)));

                    subMenu.AddItem(
                        new MenuItem(
                            string.Format("{0}.header", subMenu.Name),
                            string.Format("{0} {1} Values", args.CheckType, args.ValueType)));

                    for (var i = 0; i < args.LevelRanges.Count; i++)
                    {
                        var levelFrom = args.LevelRanges.Keys[i];
                        var levelTo = args.LevelRanges.Values[i];
                        var defaultValue = args.DefaultValues[i];

                        subMenu.AddItem(
                            new MenuItem(
                                string.Format("{0}.{1}-{2}", subMenu.Name, levelFrom, levelTo),
                                string.Format("Level {0} - {1}", levelFrom, levelTo)).SetValue(
                                    new Slider(defaultValue, args.MinValue, args.MaxValue)));
                    }
                }
                else
                {
                    menu.AddItem(
                        new MenuItem(
                            string.Format("{0}.{1}-{2}", menu.Name, args.Type, args.UniqueId),
                            string.Format("{0}{1} {2}. {3}", prefix, checkTypeString, args.Type, valueTypeString))
                            .SetValue(new Slider(args.DefaultValue, args.MinValue, args.MaxValue)));
                }


                Menues[args.UniqueId] = new Tuple<Menu, ResourceManagerArgs>(menu, args);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool Check(string uniqueId)
        {
            try
            {
                Tuple<Menu, ResourceManagerArgs> tuple;
                if (Menues.TryGetValue(uniqueId, out tuple))
                {
                    var menu = tuple.Item1;
                    var args = tuple.Item2;
                    if (args.Advanced)
                    {
                        var subMenuName = string.Format("{0}.{1}-{2}", menu.Name, args.Type, args.UniqueId);
                        for (var i = args.LevelRanges.Count - 1; i >= 0; i--)
                        {
                            var levelFrom = args.LevelRanges.Keys[i];
                            var levelTo = args.LevelRanges.Values[i];

                            if (ObjectManager.Player.Level <= levelTo && ObjectManager.Player.Level >= levelFrom)
                            {
                                var menuItem = menu.Item(string.Format("{0}.{1}-{2}", subMenuName, levelFrom, levelTo));
                                if (menuItem != null)
                                {
                                    return Check(menuItem.GetValue<Slider>().Value, args);
                                }
                            }
                        }
                    }
                    else
                    {
                        var menuItem = menu.Item(string.Format("{0}.{1}-{2}", menu.Name, args.Type, args.UniqueId));
                        if (menuItem != null)
                        {
                            return Check(menuItem.GetValue<Slider>().Value, args);
                        }
                    }
                }
                throw new KeyNotFoundException(string.Format("ResourceManager: UniqueID \"{0}\" not found.", uniqueId));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return true;
        }

        private static bool Check(int menuValue, ResourceManagerArgs args)
        {
            if (menuValue >= 100)
            {
                return false;
            }
            var resourceValue = 0f;
            if (args.ValueType == ResourceValueType.Percent)
            {
                resourceValue = args.Type == ResourceType.Health
                    ? ObjectManager.Player.HealthPercent
                    : (args.Type == ResourceType.Mana ? ObjectManager.Player.ManaPercent : 0);
            }
            else if (args.ValueType == ResourceValueType.Total)
            {
                resourceValue = args.Type == ResourceType.Health
                    ? ObjectManager.Player.Health
                    : (args.Type == ResourceType.Mana ? ObjectManager.Player.Mana : 0);
            }
            if (args.CheckType == ResourceCheckType.Maximum)
            {
                return resourceValue <= menuValue;
            }
            if (args.CheckType == ResourceCheckType.Minimum)
            {
                return resourceValue >= menuValue;
            }
            return true;
        }
    }
}