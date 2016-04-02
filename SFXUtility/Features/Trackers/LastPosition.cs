#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 LastPosition.cs is part of SFXUtility.

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
using SFXUtility.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Trackers
{
    internal class LastPosition : Child<Trackers>
    {
        private readonly Dictionary<int, Texture> _heroTextures = new Dictionary<int, Texture>();
        private readonly List<LastPositionStruct> _lastPositions = new List<LastPositionStruct>();
        private Line _line;
        private Vector3 _spawnPoint;
        private Sprite _sprite;
        private Texture _teleportTexture;
        private Font _text;

        public LastPosition(Trackers parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Last Position"; }
        }

        protected override void OnEnable()
        {
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnEndScene -= OnDrawingEndScene;
            Obj_AI_Base.OnTeleport -= OnObjAiBaseTeleport;

            base.OnDisable();
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                var map = Menu.Item(Name + "Map").GetValue<bool>();
                var minimap = Menu.Item(Name + "Minimap").GetValue<bool>();

                var ssCircle = Menu.Item(Name + "SSCircle").GetValue<bool>();
                var circleThickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;
                var circleColor = Menu.Item(Name + "DrawingCircleColor").GetValue<Color>();

                var totalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var timerOffset = Menu.Item(Name + "DrawingSSTimerOffset").GetValue<Slider>().Value;
                var timer = Menu.Item(Name + "SSTimer").GetValue<bool>();

                _sprite.Begin(SpriteFlags.AlphaBlend);
                foreach (var lp in _lastPositions)
                {
                    if (!lp.Hero.IsDead && !lp.LastPosition.Equals(Vector3.Zero) &&
                        lp.LastPosition.Distance(lp.Hero.Position) > 500)
                    {
                        lp.Teleported = false;
                        lp.LastSeen = Game.Time;
                    }
                    lp.LastPosition = lp.Hero.Position;
                    if (lp.Hero.IsVisible)
                    {
                        lp.Teleported = false;
                        if (!lp.Hero.IsDead)
                        {
                            lp.LastSeen = Game.Time;
                        }
                    }
                    if (!lp.Hero.IsVisible && !lp.Hero.IsDead)
                    {
                        var pos = lp.Teleported ? _spawnPoint : lp.LastPosition;
                        var mpPos = Drawing.WorldToMinimap(pos);
                        var mPos = Drawing.WorldToScreen(pos);

                        if (ssCircle && !lp.LastSeen.Equals(0f) && Game.Time - lp.LastSeen > 3f)
                        {
                            var radius = Math.Abs((Game.Time - lp.LastSeen - 1) * lp.Hero.MoveSpeed * 0.9f);
                            if (radius <= 8000)
                            {
                                if (map && pos.IsOnScreen(50))
                                {
                                    Render.Circle.DrawCircle(pos, radius, circleColor, circleThickness, true);
                                }
                                if (minimap)
                                {
                                    DrawCircleMinimap(pos, radius, circleColor, circleThickness);
                                }
                            }
                        }

                        if (map && pos.IsOnScreen(50))
                        {
                            _sprite.DrawCentered(_heroTextures[lp.Hero.NetworkId], mPos);
                        }
                        if (minimap)
                        {
                            _sprite.DrawCentered(_heroTextures[lp.Hero.NetworkId], mpPos);
                        }

                        if (lp.IsTeleporting)
                        {
                            if (map && pos.IsOnScreen(50))
                            {
                                _sprite.DrawCentered(_teleportTexture, mPos);
                            }
                            if (minimap)
                            {
                                _sprite.DrawCentered(_teleportTexture, mpPos);
                            }
                        }

                        if (timer && !lp.LastSeen.Equals(0f) && Game.Time - lp.LastSeen > 3f)
                        {
                            var time = (Game.Time - lp.LastSeen).FormatTime(totalSeconds);
                            if (map && pos.IsOnScreen(50))
                            {
                                _text.DrawTextCentered(
                                    time, new Vector2(mPos.X, mPos.Y + 15 + timerOffset), SharpDX.Color.White);
                            }
                            if (minimap)
                            {
                                _text.DrawTextCentered(
                                    time, new Vector2(mpPos.X, mpPos.Y + 15 + timerOffset), SharpDX.Color.White);
                            }
                        }
                    }
                }
                _sprite.End();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void DrawCircleMinimap(Vector3 center, float radius, Color color, int thickness = 5, int quality = 30)
        {
            var sharpColor = new ColorBGRA(color.R, color.G, color.B, 255);
            var pointList = new List<Vector3>();
            for (var i = 0; i < quality; i++)
            {
                var angle = i * Math.PI * 2 / quality;
                pointList.Add(
                    new Vector3(
                        center.X + radius * (float) Math.Cos(angle), center.Y + radius * (float) Math.Sin(angle),
                        center.Z));
            }
            _line.Width = thickness;
            _line.Begin();
            for (var i = 0; i < pointList.Count; i++)
            {
                var a = pointList[i];
                var b = pointList[i == pointList.Count - 1 ? 0 : i + 1];

                var aonScreen = Drawing.WorldToMinimap(a);
                var bonScreen = Drawing.WorldToMinimap(b);

                _line.Draw(new[] { aonScreen, bonScreen }, sharpColor);
            }
            _line.End();
        }

        private void OnObjAiBaseTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var lastPosition = _lastPositions.FirstOrDefault(e => e.Hero.NetworkId == packet.UnitNetworkId);
                if (lastPosition != null)
                {
                    switch (packet.Status)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            lastPosition.IsTeleporting = true;
                            break;
                        case Packet.S2C.Teleport.Status.Abort:
                            lastPosition.IsTeleporting = false;
                            break;
                        case Packet.S2C.Teleport.Status.Finish:
                            lastPosition.Teleported = true;
                            lastPosition.IsTeleporting = false;
                            lastPosition.LastSeen = Game.Time;
                            break;
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
                    new MenuItem(drawingMenu.Name + "CircleThickness", "Circle Thickness").SetValue(
                        new Slider(1, 1, 10)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "CircleColor", "Circle Color").SetValue(Color.White));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "TimeFormat", "Time Format").SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "SSTimerOffset", "SS Timer Offset").SetValue(new Slider(5, 0, 20)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "SSTimer", "SS Timer").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "SSCircle", "SS Circle").SetValue(false));
                Menu.AddItem(new MenuItem(Menu.Name + "Minimap", "Minimap").SetValue(true));
                Menu.AddItem(new MenuItem(Menu.Name + "Map", "Map").SetValue(true));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _sprite = MDrawing.GetSprite();
                _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);
                _line = MDrawing.GetLine(1);
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
                if (!GameObjects.EnemyHeroes.Any())
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                _teleportTexture = Resources.LP_Teleport.ToTexture();

                var spawn = GameObjects.EnemySpawnPoints.FirstOrDefault();
                _spawnPoint = spawn != null ? spawn.Position : Vector3.Zero;

                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    _heroTextures[enemy.NetworkId] =
                        (ImageLoader.Load("LP", enemy.ChampionName) ?? Resources.LP_Default).ToTexture();
                    var eStruct = new LastPositionStruct(enemy) { LastPosition = _spawnPoint };
                    _lastPositions.Add(eStruct);
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class LastPositionStruct
        {
            public LastPositionStruct(Obj_AI_Hero hero)
            {
                Hero = hero;
                LastPosition = Vector3.Zero;
            }

            public Obj_AI_Hero Hero { get; private set; }
            public bool IsTeleporting { get; set; }
            public float LastSeen { get; set; }
            public Vector3 LastPosition { get; set; }
            public bool Teleported { get; set; }
        }
    }
}