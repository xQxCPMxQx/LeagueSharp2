#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Waypoint.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class Waypoint : Child<Drawings>
    {
        private const float CheckInterval = 50f;
        private readonly Dictionary<int, List<Vector2>> _waypoints = new Dictionary<int, List<Vector2>>();
        private float _lastCheck = Environment.TickCount;

        public Waypoint(Drawings parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Waypoint"; }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (_lastCheck + CheckInterval > Environment.TickCount)
                {
                    return;
                }
                _lastCheck = Environment.TickCount;

                foreach (var hero in
                    GameObjects.Heroes.Where(
                        hero =>
                            Menu.Item(Name + "DrawAlly").GetValue<bool>() && hero.IsAlly ||
                            Menu.Item(Name + "DrawEnemy").GetValue<bool>() && hero.IsEnemy))
                {
                    _waypoints[hero.NetworkId] = hero.GetWaypoints();
                }
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
                var crossColor = Menu.Item(Name + "DrawingCrossColor").GetValue<Color>();
                var lineColor = Menu.Item(Name + "DrawingLineColor").GetValue<Color>();

                foreach (var waypoints in _waypoints.Values)
                {
                    var arrivalTime = 0.0f;
                    for (int i = 0, l = waypoints.Count - 1; i < l; i++)
                    {
                        if (!waypoints[i].IsValid() || !waypoints[i + 1].IsValid())
                        {
                            continue;
                        }

                        var current = Drawing.WorldToScreen(waypoints[i].To3D());
                        var next = Drawing.WorldToScreen(waypoints[i + 1].To3D());

                        arrivalTime += Vector3.Distance(waypoints[i].To3D(), waypoints[i + 1].To3D()) /
                                       (ObjectManager.Player.MoveSpeed / 1000) / 1000;
                        if (current.IsOnScreen(next))
                        {
                            Drawing.DrawLine(current.X, current.Y, next.X, next.Y, 1, lineColor);
                            if (i == l - 1 && arrivalTime > 0.1f)
                            {
                                Draw.Cross(next, 10f, 2f, crossColor);
                                Draw.TextCentered(
                                    new Vector2(next.X - 5, next.Y + 15), crossColor, arrivalTime.ToString("0.0"));
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

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "CrossColor", "Cross Color").SetValue(Color.DarkRed));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "LineColor", "Line Color").SetValue(Color.White));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "DrawAlly", "Ally").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "DrawEnemy", "Enemy").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Menu.Item(Name + "DrawAlly").ValueChanged += delegate
                {
                    if (_waypoints == null)
                    {
                        return;
                    }
                    foreach (var ally in GameObjects.AllyHeroes.Where(ally => _waypoints.ContainsKey(ally.NetworkId)))
                    {
                        _waypoints.Remove(ally.NetworkId);
                    }
                };

                Menu.Item(Name + "DrawEnemy").ValueChanged += delegate
                {
                    if (_waypoints == null)
                    {
                        return;
                    }
                    foreach (
                        var enemy in GameObjects.EnemyHeroes.Where(enemy => _waypoints.ContainsKey(enemy.NetworkId)))
                    {
                        _waypoints.Remove(enemy.NetworkId);
                    }
                };

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}