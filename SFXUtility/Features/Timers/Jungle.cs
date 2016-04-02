#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Jungle.cs is part of SFXUtility.

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
    internal class Jungle : Child<Timers>
    {
        private const float CheckInterval = 800f;
        private readonly List<Camp> _camps = new List<Camp>();
        private int _dragonStacks;
        private float _lastCheck = Environment.TickCount;
        private Font _mapText;
        private Font _minimapText;

        public Jungle(Timers parent) : base(parent)
        {
            OnLoad();
        }

        protected override List<Utility.Map.MapType> BlacklistedMaps
        {
            get
            {
                return new List<Utility.Map.MapType>
                {
                    Utility.Map.MapType.CrystalScar,
                    Utility.Map.MapType.HowlingAbyss
                };
            }
        }

        public override string Name
        {
            get { return "Jungle"; }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_AI_Minion ||
                    sender.Team != GameObjectTeam.Neutral)
                {
                    return;
                }

                foreach (var camp in _camps.ToArray())
                {
                    var mob =
                        camp.Mobs.FirstOrDefault(m => m.Name.Contains(sender.Name, StringComparison.OrdinalIgnoreCase));
                    if (mob != null)
                    {
                        if (mob.Name.Contains("Herald", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Game.Time + camp.RespawnTime > 20 * 60 ||
                                GameObjects.Jungle.Any(j => j.CharData.BaseSkinName.Contains("Baron")))
                            {
                                _camps.Remove(camp);
                                continue;
                            }
                        }
                        mob.Dead = true;
                        camp.Dead = camp.Mobs.All(m => m.Dead);
                        if (camp.Dead)
                        {
                            camp.Dead = true;
                            camp.NextRespawnTime = (int) Game.Time + camp.RespawnTime - 3;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_AI_Minion ||
                    sender.Team != GameObjectTeam.Neutral)
                {
                    return;
                }

                foreach (var camp in _camps)
                {
                    var mob =
                        camp.Mobs.FirstOrDefault(m => m.Name.Contains(sender.Name, StringComparison.OrdinalIgnoreCase));
                    if (mob != null)
                    {
                        mob.Dead = false;
                        camp.Dead = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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

                var dragonStacks = 0;
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    var buff =
                        enemy.Buffs.FirstOrDefault(
                            b => b.Name.Equals("s5test_dragonslayerbuff", StringComparison.OrdinalIgnoreCase));
                    if (buff != null)
                    {
                        dragonStacks = buff.Count;
                    }
                }

                if (dragonStacks > _dragonStacks || dragonStacks == 5)
                {
                    var dCamp = _camps.FirstOrDefault(c => c.Mobs.Any(m => m.Name.Contains("Dragon")));
                    if (dCamp != null && !dCamp.Dead)
                    {
                        dCamp.Dead = true;
                        dCamp.NextRespawnTime = (int) Game.Time + dCamp.RespawnTime;
                    }
                }

                _dragonStacks = dragonStacks;

                var bCamp = _camps.FirstOrDefault(c => c.Mobs.Any(m => m.Name.Contains("Baron")));
                if (bCamp != null && !bCamp.Dead)
                {
                    var heroes = GameObjects.EnemyHeroes.Where(e => e.IsVisible);
                    foreach (var hero in heroes)
                    {
                        var buff =
                            hero.Buffs.FirstOrDefault(
                                b => b.Name.Equals("exaltedwithbaronnashor", StringComparison.OrdinalIgnoreCase));
                        if (buff != null)
                        {
                            bCamp.Dead = true;
                            bCamp.NextRespawnTime = (int) buff.StartTime + bCamp.RespawnTime;
                        }
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

                foreach (var camp in _camps.Where(c => c.Dead))
                {
                    if (camp.NextRespawnTime - Game.Time <= 0)
                    {
                        camp.Dead = false;
                        continue;
                    }

                    if (mapEnabled && camp.Position.IsOnScreen())
                    {
                        _mapText.DrawTextCentered(
                            (camp.NextRespawnTime - (int) Game.Time).FormatTime(mapTotalSeconds),
                            Drawing.WorldToScreen(camp.Position), Color.White);
                    }
                    if (minimapEnabled)
                    {
                        _minimapText.DrawTextCentered(
                            (camp.NextRespawnTime - (int) Game.Time).FormatTime(minimapTotalSeconds),
                            camp.MinimapPosition, Color.White);
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
                _camps.AddRange(
                    Data.Jungle.Camps.Where(c => c.MapType == Utility.Map.GetMap().Type)
                        .Select(
                            c => new Camp(c.SpawnTime, c.RespawnTime, c.Position, c.Mobs, c.IsBig, c.MapType, c.Team)));

                if (!_camps.Any())
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

        private class Camp : Data.Jungle.Camp
        {
            public Camp(float spawnTime,
                float respawnTime,
                Vector3 position,
                List<Data.Jungle.Mob> mobs,
                bool isBig,
                Utility.Map.MapType mapType,
                GameObjectTeam team,
                bool dead = false) : base(spawnTime, respawnTime, position, mobs, isBig, mapType, team)
            {
                Dead = dead;
                Mobs = mobs.Select(mob => new Mob(mob.Name)).ToList();
            }

            public new List<Mob> Mobs { get; private set; }
            public float NextRespawnTime { get; set; }
            public bool Dead { get; set; }
        }

        private class Mob : Data.Jungle.Mob
        {
            public Mob(string name, bool dead = false) : base(name)
            {
                Dead = dead;
            }

            public bool Dead { get; set; }
        }
    }
}