#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Teleport.cs is part of SFXUtility.

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
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Detectors
{
    internal class Teleport : Child<Detectors>
    {
        private readonly List<TeleportObject> _teleportObjects = new List<TeleportObject>();
        private Font _barText;
        private Line _line;
        private Font _text;

        public Teleport(Detectors parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Teleport"; }
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

                try
                {
                    if (Menu.Item(Name + "DrawingTextEnabled").GetValue<bool>())
                    {
                        var posX = Menu.Item(Name + "DrawingTextOffsetLeft").GetValue<Slider>().Value;
                        var posY = Menu.Item(Name + "DrawingTextOffsetTop").GetValue<Slider>().Value;
                        var count = 0;
                        foreach (var teleport in
                            _teleportObjects.Where(
                                t => t.LastStatus != Packet.S2C.Teleport.Status.Unknown && t.Update()))
                        {
                            var text = teleport.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var color = teleport.ToColor(true);
                                _text.DrawTextCentered(
                                    text, posX, posY + (_text.Description.Height + 1) * count++,
                                    new SharpDX.Color(color.R, color.G, color.B));
                            }
                        }
                    }

                    if (Menu.Item(Name + "DrawingBarEnabled").GetValue<bool>())
                    {
                        var dScale = Menu.Item(Name + "DrawingBarScale").GetValue<Slider>().Value / 10d;
                        var barHeight =
                            (int) Math.Ceiling(Menu.Item(Name + "DrawingBarHeight").GetValue<Slider>().Value * dScale);
                        var seperatorHeight = (int) Math.Ceiling(barHeight / 2d);
                        var top = true;
                        var posX = Menu.Item(Name + "DrawingBarOffsetLeft").GetValue<Slider>().Value;
                        var posY = Menu.Item(Name + "DrawingBarOffsetTop").GetValue<Slider>().Value;
                        var barWidth =
                            (float) Math.Ceiling(Menu.Item(Name + "DrawingBarWidth").GetValue<Slider>().Value * dScale);
                        var teleports =
                            _teleportObjects.Where(
                                t => t.LastStatus != Packet.S2C.Teleport.Status.Unknown && t.Update(true))
                                .OrderBy(t => t.Countdown);
                        foreach (var teleport in teleports.Where(t => t.Duration > 0 && !t.Hero.IsDead))
                        {
                            var color = teleport.ToColor();
                            var width = barWidth / teleport.Duration * teleport.Countdown;
                            width = width > barWidth ? barWidth : width;

                            _line.Width = barHeight;
                            _line.Begin();

                            _line.Draw(
                                new[]
                                {
                                    new Vector2(posX, posY + barHeight / 2f),
                                    new Vector2(posX + width, posY + barHeight / 2f)
                                },
                                new SharpDX.Color((int) color.R, color.G, color.B, 100));

                            _line.End();

                            _line.Width = 1;
                            _line.Begin();

                            _line.Draw(
                                new[]
                                {
                                    new Vector2(
                                        posX + width,
                                        top ? posY - seperatorHeight - barHeight / 2f : posY + barHeight + 2),
                                    new Vector2(posX + width, top ? posY : posY + seperatorHeight * 2 + barHeight + 2)
                                },
                                SharpDX.Color.White);

                            _line.End();

                            _barText.DrawTextCentered(
                                teleport.Hero.ChampionName, (int) (posX + width),
                                top
                                    ? posY - barHeight - seperatorHeight - 2
                                    : posY + barHeight * 2 + seperatorHeight * 2 + 2,
                                new ColorBGRA(color.R, color.G, color.B, color.A));

                            _barText.DrawTextCentered(
                                ((int) teleport.Hero.HealthPercent).ToString(), (int) (posX + width - 1),
                                top
                                    ? posY - barHeight - 3 - seperatorHeight - _barText.Description.Height + 3
                                    : posY + barHeight * 2 + 3 + seperatorHeight * 2 + _barText.Description.Height - 1,
                                new ColorBGRA(color.R, color.G, color.B, color.A));

                            top = !top;
                        }
                        if (teleports.Any())
                        {
                            _line.Width = 1;
                            _line.Begin();

                            _line.Draw(
                                new[] { new Vector2(posX, posY), new Vector2(posX + barWidth, posY) },
                                SharpDX.Color.White);
                            _line.Draw(
                                new[]
                                { new Vector2(posX + barWidth, posY), new Vector2(posX + barWidth, posY + barHeight) },
                                SharpDX.Color.White);
                            _line.Draw(
                                new[]
                                { new Vector2(posX, posY + barHeight), new Vector2(posX + barWidth, posY + barHeight) },
                                SharpDX.Color.White);
                            _line.Draw(
                                new[] { new Vector2(posX, posY), new Vector2(posX, posY + barHeight) },
                                SharpDX.Color.White);

                            _line.End();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
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

                var drawingTextMenu = new Menu("Text", drawingMenu.Name + "Text");
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "OffsetTop", "Offset Top").SetValue(
                        new Slider((int) (Drawing.Height * 0.75d), 0, Drawing.Height)));
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "OffsetLeft", "Offset Left").SetValue(
                        new Slider((int) (Drawing.Width * 0.68d), 0, Drawing.Width)));
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "FontSize", "Font Size").SetValue(new Slider(15, 5, 30)));
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "AdditionalTime", "Additional Time").SetValue(
                        new Slider(10, 0, 10))).ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                        {
                            if (_teleportObjects != null)
                            {
                                _teleportObjects.ForEach(t => t.AdditionalTextTime = args.GetNewValue<Slider>().Value);
                            }
                        };
                drawingTextMenu.AddItem(new MenuItem(drawingTextMenu.Name + "Enabled", "Enabled").SetValue(false));

                var drawingBarMenu = new Menu("Bar", drawingMenu.Name + "Bar");
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 5, 30)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Scale", "Scale").SetValue(new Slider(10, 1, 20)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Height", "Height").SetValue(new Slider(10, 3, 20)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Width", "Width").SetValue(
                        new Slider(475, 0, (int) (Drawing.Width / 1.5d))));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "OffsetTop", "Offset Top").SetValue(
                        new Slider((int) (Drawing.Height * 0.75d), 0, Drawing.Height)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "OffsetLeft", "Offset Left").SetValue(
                        new Slider((int) (Drawing.Width / 2f - (int) (Drawing.Width / 1.5f) / 2f), 0, Drawing.Width)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "AdditionalTime", "Additional Time").SetValue(
                        new Slider(5, 0, 10))).ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                        {
                            if (_teleportObjects != null)
                            {
                                _teleportObjects.ForEach(t => t.AdditionalBarTime = args.GetNewValue<Slider>().Value);
                            }
                        };
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "HCenter", "Horizontal Center").SetValue(false)).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        if (args.GetNewValue<bool>())
                        {
                            Utility.DelayAction.Add(
                                1, delegate
                                {
                                    try
                                    {
                                        var dScale = Menu.Item(Name + "DrawingBarScale").GetValue<Slider>().Value / 10d;
                                        var barWidth =
                                            (float)
                                                Math.Ceiling(
                                                    Menu.Item(Name + "DrawingBarWidth").GetValue<Slider>().Value *
                                                    dScale);

                                        var centerPoint = (int) (Drawing.Width / 2f - barWidth / 2f);
                                        Menu.Item(Name + "DrawingBarOffsetLeft")
                                            .SetValue(new Slider(centerPoint, 0, Drawing.Width));
                                        Menu.Item(Name + "DrawingBarHCenter").SetValue(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Global.Logger.AddItem(new LogItem(ex));
                                    }
                                });
                        }
                    };
                drawingBarMenu.AddItem(new MenuItem(drawingBarMenu.Name + "Enabled", "Enabled").SetValue(false));

                drawingMenu.AddSubMenu(drawingTextMenu);
                drawingMenu.AddSubMenu(drawingBarMenu);

                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Self", "Self").SetValue(false))
                    .DontSave()
                    .ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                    {
                        if (args.GetNewValue<bool>())
                        {
                            _teleportObjects.Add(new TeleportObject(ObjectManager.Player));
                        }
                        else
                        {
                            _teleportObjects.RemoveAll(t => t.Hero.NetworkId.Equals(ObjectManager.Player.NetworkId));
                        }
                    };

                Menu.AddSubMenu(drawingMenu);

                var notificationMenu = new Menu("Notification", Name + "Notification");

                notificationMenu.AddItem(new MenuItem(notificationMenu.Name + "Started", "Started").SetValue(false));
                notificationMenu.AddItem(new MenuItem(notificationMenu.Name + "Aborted", "Aborted").SetValue(false));
                notificationMenu.AddItem(new MenuItem(notificationMenu.Name + "Finished", "Finished").SetValue(false));

                Menu.AddSubMenu(notificationMenu);

                var chatMenu = new Menu("Chat (Local)", Name + "Chat");

                chatMenu.AddItem(new MenuItem(chatMenu.Name + "Started", "Started").SetValue(false));
                chatMenu.AddItem(new MenuItem(chatMenu.Name + "Aborted", "Aborted").SetValue(false));
                chatMenu.AddItem(new MenuItem(chatMenu.Name + "Finished", "Finished").SetValue(false));

                Menu.AddSubMenu(chatMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _text = MDrawing.GetFont(Menu.Item(Name + "DrawingTextFontSize").GetValue<Slider>().Value);
                _barText =
                    MDrawing.GetFont(
                        (int)
                            Math.Ceiling(
                                Menu.Item(Name + "DrawingBarFontSize").GetValue<Slider>().Value *
                                (Menu.Item(Menu.Name + "DrawingBarScale").GetValue<Slider>().Value / 10d)));
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
                _teleportObjects.AddRange(
                    GameObjects.EnemyHeroes.Select(
                        hero =>
                            new TeleportObject(hero)
                            {
                                AdditionalTextTime =
                                    Menu.Item(Menu.Name + "DrawingTextAdditionalTime").GetValue<Slider>().Value,
                                AdditionalBarTime =
                                    Menu.Item(Menu.Name + "DrawingBarAdditionalTime").GetValue<Slider>().Value
                            }));

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var teleport = _teleportObjects.FirstOrDefault(r => r.Hero.NetworkId == packet.UnitNetworkId);
                if (teleport != null)
                {
                    var duration = packet.Duration;
                    if (packet.Type == Packet.S2C.Teleport.Type.Recall)
                    {
                        duration = teleport.Hero.HasBuff("exaltedwithbaronnashor") ? 4000 : 8000;
                        if (Utility.Map.GetMap().Type == Utility.Map.MapType.CrystalScar)
                        {
                            duration = 4500;
                        }
                    }
                    if (packet.Type == Packet.S2C.Teleport.Type.Shen)
                    {
                        duration = 3000;
                    }
                    if (packet.Type == Packet.S2C.Teleport.Type.TwistedFate)
                    {
                        duration = 1500;
                    }
                    if (packet.Type == Packet.S2C.Teleport.Type.Teleport)
                    {
                        duration = 4000;
                    }
                    teleport.Duration = duration;
                    teleport.LastStatus = packet.Status;
                    teleport.LastType = packet.Type;

                    if (packet.Status == Packet.S2C.Teleport.Status.Finish)
                    {
                        if (Menu.Item(Name + "NotificationFinished").GetValue<bool>())
                        {
                            Notifications.AddNotification(teleport.Hero.ChampionName + " Finished.", 5000)
                                .SetTextColor(Color.GreenYellow);
                        }
                        if (Menu.Item(Name + "ChatFinished").GetValue<bool>())
                        {
                            Game.PrintChat(
                                string.Format("<font color='#8ACC25'>Finished: {0}</font>", teleport.Hero.ChampionName));
                        }
                    }

                    if (packet.Status == Packet.S2C.Teleport.Status.Abort)
                    {
                        if (Menu.Item(Name + "NotificationAborted").GetValue<bool>())
                        {
                            Notifications.AddNotification(teleport.Hero.ChampionName + " Aborted.", 5000)
                                .SetTextColor(Color.Orange);
                        }
                        if (Menu.Item(Name + "ChatAborted").GetValue<bool>())
                        {
                            Game.PrintChat(
                                string.Format("<font color='#CC0000'>Aborted: {0}</font>", teleport.Hero.ChampionName));
                        }
                    }

                    if (packet.Status == Packet.S2C.Teleport.Status.Start)
                    {
                        if (Menu.Item(Name + "NotificationStarted").GetValue<bool>())
                        {
                            Notifications.AddNotification(teleport.Hero.ChampionName + " Started.", 5000)
                                .SetTextColor(Color.White);
                        }
                        if (Menu.Item(Name + "ChatStarted").GetValue<bool>())
                        {
                            Game.PrintChat(
                                string.Format("<font color='#FFFFFF'>Started: {0}</font>", teleport.Hero.ChampionName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class TeleportObject
        {
            public readonly Obj_AI_Hero Hero;
            private int _duration;
            private Packet.S2C.Teleport.Status _lastStatus;
            private float _preLastActionTime;

            public TeleportObject(Obj_AI_Hero hero)
            {
                Hero = hero;
                LastStatus = Packet.S2C.Teleport.Status.Unknown;
            }

            public int AdditionalTextTime { private get; set; }
            public int AdditionalBarTime { private get; set; }

            public int Duration
            {
                get { return _duration; }
                set { _duration = value / 1000; }
            }

            public Packet.S2C.Teleport.Status LastStatus
            {
                get { return _lastStatus; }
                set
                {
                    _lastStatus = value;
                    _preLastActionTime = LastActionTime;
                    LastActionTime = Game.Time;
                }
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public Packet.S2C.Teleport.Type LastType { get; set; }

            public float Countdown
            {
                get
                {
                    if (Hero.IsMe && LastStatus == Packet.S2C.Teleport.Status.Finish) {}
                    switch (LastStatus)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            return Game.Time - LastActionTime;
                        case Packet.S2C.Teleport.Status.Finish:
                            return Game.Time - LastActionTime > AdditionalBarTime
                                ? 0
                                : LastActionTime - _preLastActionTime;
                        case Packet.S2C.Teleport.Status.Abort:
                            return Game.Time - LastActionTime > AdditionalBarTime
                                ? 0
                                : LastActionTime - _preLastActionTime;
                    }
                    return 0;
                }
            }

            private float LastActionTime { get; set; }

            public override string ToString()
            {
                var time = LastActionTime + Duration - Game.Time;
                if (time <= 0)
                {
                    time = Game.Time - LastActionTime;
                }
                switch (LastType)
                {
                    case Packet.S2C.Teleport.Type.Recall:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Recalling", Hero.ChampionName, (int) Hero.HealthPercent,
                                    time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Recalled", Hero.ChampionName, (int) Hero.HealthPercent,
                                    time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Aborted", Hero.ChampionName, (int) Hero.HealthPercent,
                                    time);
                        }
                        break;

                    case Packet.S2C.Teleport.Type.Teleport:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Teleporting", Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Teleported", Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Aborted", Hero.ChampionName, (int) Hero.HealthPercent,
                                    time);
                        }
                        break;

                    case Packet.S2C.Teleport.Type.Shen:
                    case Packet.S2C.Teleport.Type.TwistedFate:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Transporting", Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Transported", Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", "Aborted", Hero.ChampionName, (int) Hero.HealthPercent,
                                    time);
                        }
                        break;
                }
                return string.Empty;
            }

            public Color ToColor(bool text = false)
            {
                switch (LastStatus)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        return text ? Color.Beige : Color.White;

                    case Packet.S2C.Teleport.Status.Finish:
                        return text ? Color.GreenYellow : Color.White;

                    case Packet.S2C.Teleport.Status.Abort:
                        return text ? Color.Red : Color.Yellow;

                    default:
                        return text ? Color.Black : Color.White;
                }
            }

            public bool Update(bool bar = false)
            {
                var additional = LastStatus == Packet.S2C.Teleport.Status.Start
                    ? Duration + (bar ? AdditionalBarTime : AdditionalTextTime)
                    : (bar ? AdditionalBarTime : AdditionalTextTime);
                if (LastActionTime + additional <= Game.Time)
                {
                    LastActionTime = 0f;
                    return false;
                }
                return true;
            }
        }
    }

    public class TeleportEventArgs : EventArgs
    {
        private readonly Packet.S2C.Teleport.Status _status;
        private readonly Packet.S2C.Teleport.Type _type;
        private readonly int _unitNetworkId;

        public TeleportEventArgs(int unitNetworkId, Packet.S2C.Teleport.Status status, Packet.S2C.Teleport.Type type)
        {
            _unitNetworkId = unitNetworkId;
            _status = status;
            _type = type;
        }

        public Packet.S2C.Teleport.Status Status
        {
            get { return _status; }
        }

        public Packet.S2C.Teleport.Type Type
        {
            get { return _type; }
        }

        public int UnitNetworkId
        {
            get { return _unitNetworkId; }
        }
    }
}