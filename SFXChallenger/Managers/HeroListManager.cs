#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 HeroListManager.cs is part of SFXChallenger.

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
using SFXChallenger.Args;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.Managers
{
    public class HeroListManager
    {
        private static readonly Dictionary<string, Tuple<Menu, HeroListManagerArgs>> Menues =
            new Dictionary<string, Tuple<Menu, HeroListManagerArgs>>();

        public static Color WhitelistColor
        {
            get { return Color.SpringGreen; }
        }

        public static Color BlacklistColor
        {
            get { return new Color(255, 60, 60); }
        }

        public static void AddToMenu(Menu menu, HeroListManagerArgs args)
        {
            try
            {
                if (Menues.ContainsKey(args.UniqueId))
                {
                    throw new ArgumentException(
                        string.Format("HeroListManager: UniqueID \"{0}\" already exist.", args.UniqueId));
                }

                menu.Color = args.IsWhitelist ? WhitelistColor : BlacklistColor;

                foreach (var hero in GameObjects.Heroes.Where(h => args.Allies && h.IsAlly || args.Enemies && h.IsEnemy)
                    )
                {
                    var item =
                        new MenuItem(
                            menu.Name + ".hero-list-" + args.UniqueId + hero.ChampionName.ToLower(), hero.ChampionName)
                            .SetTag(args.MenuTag);
                    if (args.DontSave)
                    {
                        item.DontSave();
                    }
                    menu.AddItem(item.SetValue(args.DefaultValue));
                }

                if (args.EnabledButton)
                {
                    menu.AddItem(
                        new MenuItem(menu.Name + ".hero-list-" + args.UniqueId + ".enabled", "Enabled").SetTag(
                            args.MenuTag).SetValue(args.Enabled));
                }

                Menues[args.UniqueId] = new Tuple<Menu, HeroListManagerArgs>(menu, args);
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
                Tuple<Menu, HeroListManagerArgs> tuple;
                if (Menues.TryGetValue(uniqueId, out tuple))
                {
                    return !tuple.Item2.EnabledButton ||
                           tuple.Item1.Item(tuple.Item1.Name + ".hero-list-" + uniqueId + ".enabled").GetValue<bool>();
                }
                throw new KeyNotFoundException(string.Format("HeroListManager: UniqueID \"{0}\" not found.", uniqueId));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static List<Obj_AI_Hero> GetEnabledHeroes(string uniqueId)
        {
            var heroes = new List<Obj_AI_Hero>();
            try
            {
                Tuple<Menu, HeroListManagerArgs> tuple;
                if (Menues.TryGetValue(uniqueId, out tuple))
                {
                    if (!tuple.Item2.EnabledButton ||
                        tuple.Item1.Item(tuple.Item1.Name + ".hero-list-" + uniqueId + ".enabled").GetValue<bool>())
                    {
                        heroes.AddRange(
                            from hero in
                                GameObjects.Heroes.Where(
                                    h => (tuple.Item2.Allies && h.IsAlly) || (tuple.Item2.Enemies && h.IsEnemy))
                            let item =
                                tuple.Item1.Item(
                                    tuple.Item1.Name + ".hero-list-" + uniqueId + hero.ChampionName.ToLower())
                            where item != null && item.GetValue<bool>()
                            select hero);
                    }
                }
                else
                {
                    throw new KeyNotFoundException(
                        string.Format("HeroListManager: UniqueID \"{0}\" not found.", uniqueId));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return heroes;
        }

        public static bool Check(string uniqueId, Obj_AI_Hero hero)
        {
            return Check(uniqueId, hero.ChampionName);
        }

        public static bool Check(string uniqueId, string champ)
        {
            try
            {
                Tuple<Menu, HeroListManagerArgs> tuple;
                if (Menues.TryGetValue(uniqueId, out tuple))
                {
                    if (!tuple.Item2.EnabledButton ||
                        tuple.Item1.Item(tuple.Item1.Name + ".hero-list-" + uniqueId + ".enabled").GetValue<bool>())
                    {
                        return tuple.Item2.IsWhitelist &&
                               tuple.Item1.Item(tuple.Item1.Name + ".hero-list-" + uniqueId + champ.ToLower())
                                   .GetValue<bool>() ||
                               !tuple.Item2.IsWhitelist &&
                               !tuple.Item1.Item(tuple.Item1.Name + ".hero-list-" + uniqueId + champ.ToLower())
                                   .GetValue<bool>();
                    }
                }
                else
                {
                    throw new KeyNotFoundException(
                        string.Format("HeroListManager: UniqueID \"{0}\" not found.", uniqueId));
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