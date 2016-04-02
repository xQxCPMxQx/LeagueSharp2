#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Inhibitor.cs is part of SFXUtility.

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
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Timers
{
    internal class Inhibitor : Child<Timers>
    {
        private const float CheckInterval = 800f;
        private readonly List<InhibitorObject> _inhibs = new List<InhibitorObject>();
        private float _lastCheck = Environment.TickCount;
        private Font _mapText;
        private Font _minimapText;

        public Inhibitor(Timers parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Inhibitor"; }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnEndScene -= OnDrawingEndScene;

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

                if (_inhibs == null)
                {
                    return;
                }

                foreach (var inhib in _inhibs)
                {
                    if (inhib.Object.Health > 0)
                    {
                        inhib.LastHealth = inhib.Object.Health;
                        inhib.Destroyed = false;
                    }
                    else if (!inhib.Destroyed && inhib.LastHealth > 0 && inhib.Object.Health <= 0)
                    {
                        inhib.Destroyed = true;
                        inhib.NextRespawnTime = (int) Game.ClockTime + inhib.RespawnTime;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                var mapTotalSeconds = Menu.Item(Name + "DrawingMapTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var minimapTotalSeconds =
                    Menu.Item(Name + "DrawingMinimapTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var mapEnabled = Menu.Item(Name + "DrawingMapEnabled").GetValue<bool>();
                var minimapEnabled = Menu.Item(Name + "DrawingMinimapEnabled").GetValue<bool>();

                if (!mapEnabled && !minimapEnabled)
                {
                    return;
                }

                foreach (var inhib in _inhibs.Where(i => i != null && i.Destroyed && i.NextRespawnTime > Game.Time))
                {
                    if (mapEnabled && inhib.Object.Position.IsOnScreen())
                    {
                        _mapText.DrawTextCentered(
                            (inhib.NextRespawnTime - (int) Game.Time).FormatTime(mapTotalSeconds),
                            Drawing.WorldToScreen(inhib.Object.Position), Color.White);
                    }
                    if (minimapEnabled)
                    {
                        _minimapText.DrawTextCentered(
                            (inhib.NextRespawnTime - (int) Game.Time).FormatTime(minimapTotalSeconds),
                            Drawing.WorldToMinimap(inhib.Object.Position), Color.White, true);
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
                var drawingMapMenu = new Menu("Map", drawingMenu.Name + "Map");
                var drawingMinimapMenu = new Menu("Minimap", drawingMenu.Name + "Minimap");

                drawingMapMenu.AddItem(
                    new MenuItem(drawingMapMenu.Name + "TimeFormat", "Time Format").SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMapMenu.AddItem(
                    new MenuItem(drawingMapMenu.Name + "FontSize", "Font Size").SetValue(new Slider(20, 3, 30)));
                drawingMapMenu.AddItem(new MenuItem(drawingMapMenu.Name + "Enabled", "Enabled").SetValue(false));

                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "TimeFormat", "Time Format").SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                drawingMinimapMenu.AddItem(new MenuItem(drawingMinimapMenu.Name + "Enabled", "Enabled").SetValue(false));

                drawingMenu.AddSubMenu(drawingMapMenu);
                drawingMenu.AddSubMenu(drawingMinimapMenu);

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _minimapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMinimapFontSize").GetValue<Slider>().Value);
                _mapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMapFontSize").GetValue<Slider>().Value);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                foreach (var inhib in GameObjects.Inhibitors)
                {
                    _inhibs.Add(new InhibitorObject(inhib));
                }

                if (!_inhibs.Any())
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class InhibitorObject
        {
            public InhibitorObject(Obj_BarracksDampener inhibitor)
            {
                Object = inhibitor;
                Destroyed = false;
                NextRespawnTime = -1;
                RespawnTime = 300;
                LastHealth = float.MinValue;
            }

            public Obj_BarracksDampener Object { get; private set; }
            public bool Destroyed { get; set; }
            public int RespawnTime { get; private set; }
            public int NextRespawnTime { get; set; }
            public float LastHealth { get; set; }
        }
    }
}