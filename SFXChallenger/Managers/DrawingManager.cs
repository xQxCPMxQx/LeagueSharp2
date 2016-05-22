#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DrawingManager.cs is part of SFXChallenger.

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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Interfaces;
using SFXChallenger.Library.Extensions.SharpDX;
using SFXChallenger.Library.Logger;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Managers
{
    public class DrawingManager
    {
        private static IChampion _champion;
        private static bool _separator;
        private static readonly Dictionary<string, float> Customs = new Dictionary<string, float>();
        private static readonly Dictionary<string, MenuItem> Others = new Dictionary<string, MenuItem>();
        public static Menu Menu { get; private set; }

        public static void AddToMenu(Menu menu, IChampion champion)
        {
            try
            {
                _champion = champion;
                Menu = menu;

                Menu.AddItem(
                    new MenuItem(Menu.Name + ".circle-thickness", "Circle Thickness").SetValue(new Slider(5, 1, 10)));

                foreach (var spell in _champion.Spells.Where(s => s != null && s.Range > 0 && s.Range < 5000))
                {
                    if (spell.IsChargedSpell)
                    {
                        Menu.AddItem(
                            new MenuItem(
                                Menu.Name + "." + spell.Slot.ToString().ToLower() + "-min",
                                spell.Slot.ToString().ToUpper() + " Min.").SetValue(
                                    new Circle(false, Color.DeepSkyBlue)));
                        Menu.AddItem(
                            new MenuItem(
                                Menu.Name + "." + spell.Slot.ToString().ToLower() + "-max",
                                spell.Slot.ToString().ToUpper() + " Max").SetValue(new Circle(false, Color.DeepSkyBlue)));
                    }
                    else
                    {
                        Menu.AddItem(
                            new MenuItem(
                                Menu.Name + "." + spell.Slot.ToString().ToLower(), spell.Slot.ToString().ToUpper())
                                .SetValue(new Circle(false, Color.DeepSkyBlue)));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void Add(string name, float range)
        {
            try
            {
                if (!_separator)
                {
                    Menu.AddItem(new MenuItem(Menu.Name + ".separator", string.Empty));
                    _separator = true;
                }
                var key = name.Trim().ToLower();
                if (Customs.ContainsKey(key))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" already exist.", name));
                }

                Menu.AddItem(new MenuItem(Menu.Name + "." + key, name).SetValue(new Circle(false, Color.DeepSkyBlue)));

                Customs[key] = range;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static MenuItem Add<T>(string name, T value)
        {
            try
            {
                if (!_separator)
                {
                    Menu.AddItem(new MenuItem(Menu.Name + ".separator", string.Empty));
                    _separator = true;
                }
                var key = name.Trim().ToLower();
                if (Others.ContainsKey(key))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" already exist.", name));
                }
                var item = new MenuItem(Menu.Name + "." + key, name).SetValue(value);
                Menu.AddItem(item);

                Others[key] = item;

                return item;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static MenuItem Get(string name)
        {
            try
            {
                var key = name.Trim().ToLower();
                MenuItem value;
                if (!Others.TryGetValue(key, out value))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" not found.", name));
                }
                return value;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static void Update(string name, float range)
        {
            try
            {
                var key = name.Trim().ToLower();
                if (!Customs.ContainsKey(key))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" not found.", name));
                }
                Customs[key] = range;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void Draw()
        {
            try
            {
                if (Menu == null || _champion.Spells == null || !Utils.ShouldDraw())
                {
                    return;
                }

                var circleThickness = Menu.Item(Menu.Name + ".circle-thickness").GetValue<Slider>().Value;
                foreach (
                    var spell in _champion.Spells.Where(s => s != null && s.Range > 0 && s.Range < 5000 && s.Level > 0))
                {
                    if (spell.IsChargedSpell)
                    {
                        var min =
                            Menu.Item(Menu.Name + "." + spell.Slot.ToString().ToLower() + "-min").GetValue<Circle>();
                        var max =
                            Menu.Item(Menu.Name + "." + spell.Slot.ToString().ToLower() + "-max").GetValue<Circle>();
                        if (min.Active && ObjectManager.Player.Position.IsOnScreen(spell.ChargedMinRange))
                        {
                            Render.Circle.DrawCircle(
                                ObjectManager.Player.Position, spell.ChargedMinRange, min.Color, circleThickness);
                        }
                        if (max.Active && ObjectManager.Player.Position.IsOnScreen(spell.ChargedMaxRange))
                        {
                            Render.Circle.DrawCircle(
                                ObjectManager.Player.Position, spell.ChargedMaxRange, max.Color, circleThickness);
                        }
                    }
                    else
                    {
                        var item = Menu.Item(Menu.Name + "." + spell.Slot.ToString().ToLower()).GetValue<Circle>();
                        if (item.Active && ObjectManager.Player.Position.IsOnScreen(spell.Range))
                        {
                            Render.Circle.DrawCircle(
                                ObjectManager.Player.Position, spell.Range, item.Color, circleThickness);
                        }
                    }
                }

                foreach (var custom in Customs)
                {
                    var item = Menu.Item(Menu.Name + "." + custom.Key).GetValue<Circle>();
                    if (item.Active && ObjectManager.Player.Position.IsOnScreen(custom.Value))
                    {
                        Render.Circle.DrawCircle(
                            ObjectManager.Player.Position, custom.Value, item.Color, circleThickness);
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