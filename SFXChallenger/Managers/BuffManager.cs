#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 BuffManager.cs is part of SFXChallenger.

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
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public static class BuffManager
    {
        private static readonly Dictionary<string, Tuple<Menu, List<BuffType>, bool>> Menues =
            new Dictionary<string, Tuple<Menu, List<BuffType>, bool>>();

        private static readonly Random Random = new Random();

        static BuffManager()
        {
            Core.OnPreUpdate += OnCorePreUpdate;
        }

        public static List<BuffType> ImmobileBuffs
        {
            get
            {
                return new List<BuffType>
                {
                    BuffType.Stun,
                    BuffType.Charm,
                    BuffType.Snare,
                    BuffType.Knockup,
                    BuffType.Polymorph,
                    BuffType.Fear,
                    BuffType.Taunt
                };
            }
        }

        public static void AddToMenu(Menu menu, BuffType buffType, HeroListManagerArgs args, bool randomize)
        {
            AddToMenu(menu, new List<BuffType> { buffType }, args, randomize);
        }

        public static void AddToMenu(Menu menu, List<BuffType> buffTypes, HeroListManagerArgs args, bool randomize)
        {
            try
            {
                if (Menues.ContainsKey(args.UniqueId))
                {
                    throw new ArgumentException(
                        string.Format("BuffManager: UniqueID \"{0}\" already exist.", args.UniqueId));
                }

                args.Enemies = true;
                args.Allies = false;

                menu.AddItem(
                    new MenuItem(menu.Name + ".buff-" + args.UniqueId + ".delay", "Delay").SetValue(
                        new Slider(100, 0, 500)));
                if (randomize)
                {
                    menu.AddItem(
                        new MenuItem(menu.Name + ".buff-" + args.UniqueId + ".randomize", "Randomize Position").SetValue
                            (new Slider(10)));
                }

                menu.AddItem(new MenuItem(menu.Name + ".buff-" + args.UniqueId + ".separator", string.Empty));

                HeroListManager.AddToMenu(menu, args);

                Menues[args.UniqueId] = new Tuple<Menu, List<BuffType>, bool>(menu, buffTypes, randomize);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static event EventHandler<BuffManagerArgs> OnBuff;

        private static void OnCorePreUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }
            foreach (var entry in Menues)
            {
                var uniqueId = entry.Key;
                var menu = entry.Value.Item1;
                var buffTypes = entry.Value.Item2;

                var delay = menu.Item(menu.Name + ".buff-" + uniqueId + ".delay").GetValue<Slider>().Value;
                var randomize = entry.Value.Item3
                    ? menu.Item(menu.Name + ".buff-" + uniqueId + ".randomize").GetValue<Slider>().Value
                    : 0;

                foreach (var enemy in
                    GameObjects.EnemyHeroes.Where(e => HeroListManager.Check(uniqueId, e) && e.IsValidTarget(2000)))
                {
                    var buff =
                        enemy.Buffs.OrderBy(b => b.EndTime)
                            .FirstOrDefault(b => b.IsValid && b.IsActive && buffTypes.Any(bt => b.Type.Equals(bt)));
                    if (buff != null)
                    {
                        var position = enemy.ServerPosition;
                        var lEnemy = enemy;
                        if (delay > 1)
                        {
                            delay = Random.Next((int) (delay * 0.9f), (int) (delay * 1.1f));
                        }
                        if (randomize > 0)
                        {
                            position.X += Random.Next(0, randomize * 2 + 1) - randomize;
                            position.Y += Random.Next(0, randomize * 2 + 1) - randomize;
                        }
                        Utility.DelayAction.Add(
                            Math.Max(1, delay),
                            () => OnBuff.RaiseEvent(null, new BuffManagerArgs(uniqueId, lEnemy, position, buff.EndTime)));
                    }
                }
            }
        }
    }
}