#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Gank.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Detectors
{
    internal class Gank : Child<Detectors>
    {
        private const float CheckInterval = 300f;
        private readonly List<ChampionObject> _championObjects = new List<ChampionObject>();
        private float _lastCheck;
        private Line _line;
        private Font _text;

        public Gank(Detectors parent) : base(parent)
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
            get { return "Gank"; }
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

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.IsDead || Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }
                var duration = Menu.Item(Name + "Duration").GetValue<Slider>().Value;
                var names = Menu.Item(Name + "DrawingChampionName").GetValue<bool>();
                var tColor = Menu.Item(Name + "DrawingColor").GetValue<System.Drawing.Color>();
                var color = new Color(tColor.R, tColor.G, tColor.B, tColor.A);

                foreach (var obj in
                    _championObjects.Where(c => c.Enabled && !c.Hero.IsDead && c.LastTrigger + duration > Game.Time))
                {
                    _line.Begin();
                    _line.Draw(
                        new[]
                        {
                            Drawing.WorldToScreen(ObjectManager.Player.Position), Drawing.WorldToScreen(obj.Hero.Position)
                        }, obj.Color);
                    _line.End();
                    if (names)
                    {
                        _text.DrawTextCentered(
                            obj.Hero.ChampionName,
                            Drawing.WorldToScreen(ObjectManager.Player.Position.Extend(obj.Hero.Position, 250f)), color);
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
                if (ObjectManager.Player.IsDead || _lastCheck + CheckInterval > Environment.TickCount)
                {
                    return;
                }

                _lastCheck = Environment.TickCount;

                var cooldown = Menu.Item(Name + "Cooldown").GetValue<Slider>().Value;
                var range = Menu.Item(Name + "Range").GetValue<Slider>().Value;
                var ping = Menu.Item(Name + "Ping").GetValue<bool>();

                foreach (var obj in _championObjects.Where(c => c.Enabled && !c.Hero.IsDead && c.Hero.IsVisible))
                {
                    var distance = obj.Hero.Distance(ObjectManager.Player);
                    if (obj.Distance > range && distance <= range && Game.Time > obj.LastTrigger + cooldown)
                    {
                        obj.LastTrigger = Game.Time;
                        if (ping && obj.Hero.IsEnemy)
                        {
                            Game.ShowPing(PingCategory.Danger, obj.Hero, true);
                        }
                    }
                    obj.Distance = distance;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void MenuValueChanged()
        {
            try
            {
                foreach (var obj in _championObjects)
                {
                    var hasSmite =
                        obj.Hero.Spellbook.Spells.Any(
                            spell => spell.Name.Contains("Smite", StringComparison.OrdinalIgnoreCase));
                    var prefix = obj.Hero.IsAlly ? "Allies" : "Enemies";
                    if (Menu.Item(Menu.Name + prefix + obj.Hero.ChampionName).GetValue<Circle>().Active)
                    {
                        if (!Menu.Item(Menu.Name + prefix + "Smite").GetValue<Circle>().Active ||
                            Menu.Item(Menu.Name + prefix + "Smite").GetValue<Circle>().Active && hasSmite)
                        {
                            obj.Enabled = true;
                        }
                        else
                        {
                            obj.Enabled = false;
                        }
                    }
                    else
                    {
                        obj.Enabled = false;
                    }
                    var color = hasSmite
                        ? Menu.Item(Menu.Name + prefix + "Smite").GetValue<Circle>().Color
                        : Menu.Item(Menu.Name + prefix + obj.Hero.ChampionName).GetValue<Circle>().Color;
                    obj.Color = new Color(color.R, color.G, color.B, color.A);
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
                    new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(25, 10, 40)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Color", "Color").SetValue(System.Drawing.Color.White));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Thickness", "Thickness").SetValue(new Slider(5, 2, 10)))
                    .ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                    {
                        if (_line != null)
                        {
                            _line.Width = args.GetNewValue<Slider>().Value;
                        }
                    };

                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "ChampionName", "Champion Name").SetValue(true));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Range", "Range").SetValue(new Slider(3000, 500, 5000)));
                Menu.AddItem(new MenuItem(Name + "Cooldown", "Cooldown").SetValue(new Slider(10, 0, 30)));
                Menu.AddItem(new MenuItem(Name + "Duration", "Duration").SetValue(new Slider(10, 0, 30)));
                Menu.AddItem(new MenuItem(Name + "Ping", "Ping (Local)").SetValue(true));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _line = MDrawing.GetLine(Menu.Item(Name + "DrawingThickness").GetValue<Slider>().Value);
                _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);
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
                var enemyMenu = new Menu("Enemy", Name + "Enemies");
                enemyMenu.AddItem(
                    new MenuItem(enemyMenu.Name + "Smite", "Only Smite").SetValue(
                        new Circle(false, System.Drawing.Color.DarkViolet.ToArgb(125)))).ValueChanged +=
                    delegate { Utility.DelayAction.Add(0, MenuValueChanged); };
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    enemyMenu.AddItem(
                        new MenuItem(enemyMenu.Name + enemy.ChampionName, enemy.ChampionName).SetValue(
                            new Circle(true, System.Drawing.Color.Red.ToArgb(125)))).ValueChanged +=
                        delegate { Utility.DelayAction.Add(0, MenuValueChanged); };
                }

                Menu.AddSubMenu(enemyMenu);

                var allyMenu = new Menu("Ally", Name + "Allies");
                allyMenu.AddItem(
                    new MenuItem(allyMenu.Name + "Smite", "Only Smite").SetValue(
                        new Circle(false, System.Drawing.Color.DodgerBlue.ToArgb(125)))).ValueChanged +=
                    delegate { Utility.DelayAction.Add(0, MenuValueChanged); };
                foreach (var ally in GameObjects.AllyHeroes.Where(a => !a.IsMe))
                {
                    allyMenu.AddItem(
                        new MenuItem(allyMenu.Name + ally.ChampionName, ally.ChampionName).SetValue(
                            new Circle(true, System.Drawing.Color.Green.ToArgb(125)))).ValueChanged +=
                        delegate { Utility.DelayAction.Add(0, MenuValueChanged); };
                }

                Menu.AddSubMenu(allyMenu);

                foreach (var hero in GameObjects.Heroes.Where(h => !h.IsMe))
                {
                    _championObjects.Add(new ChampionObject(hero));
                }
                MenuValueChanged();
                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class ChampionObject
        {
            public ChampionObject(Obj_AI_Hero hero)
            {
                Hero = hero;
            }

            public Obj_AI_Hero Hero { get; private set; }
            public bool Enabled { get; set; }
            public float Distance { get; set; }
            public float LastTrigger { get; set; }
            public Color Color { get; set; }
        }
    }
}