#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Sidebar.cs is part of SFXUtility.

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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Data;
using SFXUtility.Library;
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SFXUtility.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using Utils = LeagueSharp.Common.Utils;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Trackers
{
    internal class Sidebar : Child<Trackers>
    {
        private const float HealthWidth = 74.5f;
        private const float HudWidth = 90f;
        private const float HudHeight = 84f;
        private const float SummonerWidth = 26f;
        private const float SummonerHeight = 26f;
        private readonly string[] _champsEnergy = { "Akali", "Kennen", "LeeSin", "Shen", "Zed", "Gnar", "Rengar" };

        private readonly string[] _champsNoEnergy =
        {
            "Aatrox", "DrMundo", "Vladimir", "Zac", "Katarina", "Garen",
            "Riven"
        };

        private readonly string[] _champsRage = { "Shyvana", "RekSai", "Renekton", "Rumble" };
        private readonly List<EnemyObject> _enemyObjects = new List<EnemyObject>();
        private readonly Dictionary<int, List<SpellDataInst>> _spellDatas = new Dictionary<int, List<SpellDataInst>>();
        private readonly SpellSlot[] _summonerSpellSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
        private readonly Dictionary<string, Texture> _summonerTextures = new Dictionary<string, Texture>();
        private readonly Dictionary<int, float> _teleports = new Dictionary<int, float>();
        private Texture _hudTexture;
        private Texture _invisibleTexture;
        private float _lastChatSend;
        private Line _line17;
        private Line _line24;
        private Line _line7;
        private float _scale;
        private Sprite _sprite;
        private Texture _teleportAbortTexture;
        private Texture _teleportFinishTexture;
        private Texture _teleportStartTexture;
        private Font _text14;
        private Font _text18;
        private Texture _ultimateTexture;

        public Sidebar(Trackers parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name => "Sidebar";

        protected override void OnEnable()
        {
            Game.OnWndProc += OnGameWndProc;
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnWndProc -= OnGameWndProc;
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

                var index = 0;

                var hudWidth = (float) Math.Ceiling(HudWidth * _scale);
                var hudHeight = (float) Math.Ceiling(HudHeight * _scale);

                var spacing =
                    (float)
                        Math.Ceiling((10f + Menu.Item(Menu.Name + "DrawingSpacing").GetValue<Slider>().Value) * _scale) +
                    hudHeight;

                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight / 2f;
                var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value -
                                  (hudWidth + (float) Math.Ceiling(4 * _scale)) / 2f;
                
                foreach (var enemy in _enemyObjects)
                {
                    if (enemy.Unit.IsDead && Game.Time > enemy.DeathEndTime)
                    {
                        enemy.DeathEndTime = Game.Time + enemy.Unit.DeathDuration + 1;
                    }
                    else if (!enemy.Unit.IsDead)
                    {
                        enemy.DeathEndTime = 0;
                    }

                    var offset = spacing * index;

                    var spellData = _spellDatas[enemy.Unit.NetworkId];
                    for (var i = 0; spellData.Count > i; i++)
                    {
                        var spell = spellData[i];
                        if (spell != null && _summonerTextures.ContainsKey(Summoners.FixName(spell.Name)))
                        {
                            var teleportCd = 0f;
                            if (spell.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase) &&
                                _teleports.ContainsKey(enemy.Unit.NetworkId))
                            {
                                _teleports.TryGetValue(enemy.Unit.NetworkId, out teleportCd);
                            }
                            var time = (teleportCd > 0.1f ? teleportCd : spell.CooldownExpires) - Game.Time;
                            _sprite.Begin(SpriteFlags.AlphaBlend);
                            _sprite.DrawCentered(
                                _summonerTextures[Summoners.FixName(spell.Name)],
                                new Vector2(
                                    offsetRight + hudWidth * 0.355f,
                                    offsetTop - hudHeight * 0.275f + offset + (float) Math.Ceiling(26 * _scale) * i));
                            _sprite.End();
                            if (time > 0)
                            {
                                _line24.Begin();
                                _line24.Draw(
                                    new[]
                                    {
                                        new Vector2(
                                            offsetRight + hudWidth * 0.23f,
                                            offsetTop - hudHeight * 0.28f + offset +
                                            (float) Math.Ceiling(26 * _scale) * i),
                                        new Vector2(
                                            offsetRight + hudWidth * 0.23f + (float) Math.Ceiling(24 * _scale),
                                            offsetTop - hudHeight * 0.28f + offset +
                                            (float) Math.Ceiling(26 * _scale) * i)
                                    }, new Color(0, 0, 0, 175));
                                _line24.End();

                                _text14.DrawTextCentered(
                                    ((int) time).ToStringLookUp(),
                                    new Vector2(
                                        offsetRight + hudWidth * 0.359f,
                                        offsetTop - hudHeight * 0.28f + offset + (float) Math.Ceiling(26 * _scale) * i),
                                    new Color(255, 255, 255, 210), true);
                            }
                        }
                    }
                    
                    _sprite.Begin(SpriteFlags.AlphaBlend);

                    _sprite.DrawCentered(
                        enemy.Texture,
                        new Vector2(offsetRight - hudWidth * 0.1f, offsetTop - hudHeight * 0.13f + offset));

                    _sprite.DrawCentered(
                        _hudTexture, new Vector2(offsetRight + (float) Math.Ceiling(3 * _scale), offsetTop + offset));

                    if (enemy.RSpell != null && enemy.RSpell.Level > 0 && enemy.RSpell.CooldownExpires - Game.Time < 0)
                    {
                        _sprite.DrawCentered(
                            _ultimateTexture,
                            new Vector2(offsetRight - hudWidth * 0.34f, offsetTop - hudHeight * 0.375f + offset));
                    }

                    _sprite.End();

                    if (enemy.RSpell != null && enemy.RSpell.Level > 0 && enemy.RSpell.CooldownExpires - Game.Time > 0 &&
                        enemy.RSpell.CooldownExpires - Game.Time < 100)
                    {
                        _text14.DrawTextCentered(
                            ((int) (enemy.RSpell.CooldownExpires - Game.Time)).ToStringLookUp(),
                            new Vector2(offsetRight - hudWidth * 0.338f, offsetTop - hudHeight * 0.365f + offset),
                            Color.White);
                    }
                    
                    _line17.Begin();
                    _line17.Draw(
                        new[]
                        {
                            new Vector2(offsetRight - hudWidth * 0.035f, offsetTop + hudHeight * 0.035f + offset),
                            new Vector2(
                                offsetRight - hudWidth * 0.035f + (float) Math.Ceiling(18 * _scale),
                                offsetTop + hudHeight * 0.035f + offset)
                        }, new Color(0, 0, 0, 215));
                    _line17.End();

                    _text14.DrawTextCentered(
                        enemy.Unit.Level.ToStringLookUp(),
                        new Vector2(offsetRight + hudWidth * 0.075f, offsetTop + hudHeight * 0.045f + offset),
                        !enemy.Unit.IsVisible || enemy.Unit.IsDead
                            ? new Color(255, 255, 255, 215)
                            : new Color(255, 255, 255, 240));

                    _text14.DrawTextLeft(
                        enemy.Unit.Name,
                        new Vector2(offsetRight + hudWidth * 0.52f, offsetTop - hudHeight * 0.57f + offset),
                        !enemy.Unit.IsVisible || enemy.Unit.IsDead
                            ? new Color(255, 255, 255, 215)
                            : new Color(255, 255, 255, 240));

                    var healthStart = new Vector2(
                        offsetRight - hudWidth * 0.358f, offsetTop + hudHeight * 0.268f + offset);
                    var healthWidth = (float) Math.Ceiling(HealthWidth * _scale) / enemy.Unit.MaxHealth *
                                      enemy.Unit.Health;
                    _line7.Begin();
                    _line7.Draw(
                        new[] { healthStart, new Vector2(healthStart.X + healthWidth, healthStart.Y) }, Color.Green);


                    var resStart = new Vector2(healthStart.X, healthStart.Y + (float) Math.Ceiling(9 * _scale));
                    var resWidth = (float) Math.Ceiling(HealthWidth * _scale);
                    if (!Enumerable.Contains(_champsNoEnergy, enemy.Unit.ChampionName) && enemy.Unit.MaxMana > 0)
                    {
                        resWidth = (float) Math.Ceiling(HealthWidth * _scale) / enemy.Unit.MaxMana * enemy.Unit.Mana;
                    }
                    _line7.Draw(
                        new[] { resStart, new Vector2(resStart.X + resWidth, resStart.Y) },
                        Enumerable.Contains(_champsEnergy, enemy.Unit.ChampionName)
                            ? Color.Yellow
                            : (Enumerable.Contains(_champsRage, enemy.Unit.ChampionName)
                                ? Color.DarkRed
                                : (Enumerable.Contains(_champsNoEnergy, enemy.Unit.ChampionName) ||
                                   enemy.Unit.MaxMana <= 0
                                    ? new Color(255, 255, 255, 75)
                                    : Color.Blue)));
                    _line7.End();

                    if (enemy.Unit.IsDead)
                    {
                        _line17.Begin();
                        _line17.Draw(
                            new[]
                            {
                                new Vector2(offsetRight - hudWidth * 0.36f, offsetTop + hudHeight * 0.338f + offset),
                                new Vector2(
                                    offsetRight - hudWidth * 0.36f + (float) Math.Ceiling(HealthWidth * _scale),
                                    offsetTop + hudHeight * 0.335f + offset)
                            }, Color.Black);
                        _line17.End();

                        _text18.DrawTextCentered(
                            ((int) (enemy.DeathEndTime - Game.Time)).ToStringLookUp(),
                            new Vector2(offsetRight + hudWidth * 0.07f, offsetTop + hudHeight * 0.335f + offset),
                            Color.DarkRed);
                    }

                    if (!enemy.Unit.IsVisible || enemy.Unit.IsDead)
                    {
                        _sprite.Begin(SpriteFlags.AlphaBlend);
                        _sprite.DrawCentered(
                            _invisibleTexture,
                            new Vector2(offsetRight - hudWidth * 0.09f, offsetTop - hudHeight * 0.12f + offset));
                        _sprite.End();
                    }
                    if (!enemy.Unit.IsDead && !enemy.LastPosition.Equals(Vector3.Zero) &&
                        enemy.LastPosition.Distance(enemy.Unit.Position) > 500)
                    {
                        enemy.LastVisible = Game.Time;
                    }
                    enemy.LastPosition = enemy.Unit.Position;
                    if (enemy.Unit.IsVisible || enemy.Unit.IsDead)
                    {
                        enemy.LastVisible = Game.Time;
                    }

                    if (!enemy.Unit.IsVisible && !enemy.Unit.IsDead && Game.Time - enemy.LastVisible > 3)
                    {
                        _text18.DrawTextCentered(
                            ((int) (Game.Time - enemy.LastVisible)).ToStringLookUp(),
                            new Vector2(offsetRight - hudWidth * 0.07f, offsetTop - hudHeight * 0.15f + offset),
                            new Color(255, 255, 255, 215));
                    }

                    if (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Start ||
                        (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Finish ||
                         enemy.TeleportStatus == Packet.S2C.Teleport.Status.Abort) &&
                        Game.Time <= enemy.LastTeleportStatusTime + 5f)
                    {
                        _sprite.Begin(SpriteFlags.AlphaBlend);
                        _sprite.DrawCentered(
                            enemy.TeleportStatus == Packet.S2C.Teleport.Status.Start
                                ? _teleportStartTexture
                                : (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Finish
                                    ? _teleportFinishTexture
                                    : _teleportAbortTexture),
                            new Vector2(offsetRight + (float) Math.Ceiling(3 * _scale), offsetTop + offset));
                        _sprite.End();
                    }

                    index++;
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
                    new MenuItem(drawingMenu.Name + "OffsetTop", "Offset Top").SetValue(
                        new Slider(150, 0, Drawing.Height)));

                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetRight", "Offset Right").SetValue(
                        new Slider(0, 0, Drawing.Width)));

                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Spacing", "Spacing").SetValue(new Slider(10, 0, 30)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Scale", "Scale").SetValue(new Slider(10, 5, 15)));

                Menu.AddSubMenu(drawingMenu);
                Menu.AddItem(new MenuItem(Name + "Clickable", "Clickable").SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value / 10f;

                _text14 = MDrawing.GetFont((int) Math.Ceiling(14 * _scale));
                _text18 = MDrawing.GetFont((int) Math.Ceiling(18 * _scale));
                _line7 = MDrawing.GetLine((int) Math.Ceiling(7 * _scale));
                _line17 = MDrawing.GetLine((int) Math.Ceiling(17 * _scale));
                _line24 = MDrawing.GetLine((int) Math.Ceiling(24 * _scale));
                _sprite = MDrawing.GetSprite();
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
                if (!HeroManager.Enemies.Any())
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                _hudTexture = Resources.SB_Hud.Scale(_scale).ToTexture();
                _invisibleTexture = Resources.SB_Invisible.Scale(_scale).ToTexture();
                _teleportAbortTexture = Resources.SB_RecallAbort.Scale(_scale).ToTexture();
                _teleportFinishTexture = Resources.SB_RecallFinish.Scale(_scale).ToTexture();
                _teleportStartTexture = Resources.SB_RecallStart.Scale(_scale).ToTexture();
                _ultimateTexture = Resources.SB_Ultimate.Scale(_scale).ToTexture();

                foreach (var enemy in HeroManager.Enemies)
                {
                    var lEnemy = enemy;
                    _spellDatas.Add(enemy.NetworkId, _summonerSpellSlots.Select(slot => lEnemy.GetSpell(slot)).ToList());
                }

                foreach (var enemy in HeroManager.Enemies)
                {
                    _enemyObjects.Add(
                        new EnemyObject(
                            enemy,
                            (ImageLoader.Load("SB", enemy.ChampionName) ?? Resources.SB_Default).Scale(_scale)
                                .ToTexture()));
                }

                foreach (var summonerSlot in _summonerSpellSlots)
                {
                    foreach (var enemy in HeroManager.Enemies)
                    {
                        var spell = enemy.Spellbook.GetSpell(summonerSlot);
                        if (!_summonerTextures.ContainsKey(Summoners.FixName(spell.Name)))
                        {
                            _summonerTextures[Summoners.FixName(spell.Name)] =
                                ((Bitmap)
                                    Resources.ResourceManager.GetObject(
                                        $"SB_{Summoners.FixName(spell.Name)}") ??
                                 Resources.SB_summonerbarrier).Scale(_scale).ToTexture();
                        }
                    }
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private string ReadableSummonerName(string name)
        {
            name = Summoners.FixName(name);
            switch (name)
            {
                case "summonerflash":
                    return "Flash";
                case "summonerdot":
                    return "Ignite";
                case "summonerheal":
                    return "Heal";
                case "summonerteleport":
                    return "Teleport";
                case "summonerexhaust":
                    return "Exhaust";
                case "summonerhaste":
                    return "Ghost";
                case "summonerbarrier":
                    return "Barrier";
                case "summonerboost":
                    return "Cleanse";
                case "summonersmite":
                    return "Smite";
            }
            return null;
        }

        private void OnObjAiBaseTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var enemyObject = _enemyObjects.FirstOrDefault(e => e.Unit.NetworkId == packet.UnitNetworkId);
                if (enemyObject != null)
                {
                    if (packet.Type == Packet.S2C.Teleport.Type.Teleport &&
                        (packet.Status == Packet.S2C.Teleport.Status.Finish ||
                         packet.Status == Packet.S2C.Teleport.Status.Abort))
                    {
                        var time = Game.Time;
                        Utility.DelayAction.Add(
                            250, delegate
                            {
                                var cd = packet.Status == Packet.S2C.Teleport.Status.Finish ? 300 : 200;
                                _teleports[packet.UnitNetworkId] = time + cd;
                            });
                    }
                    enemyObject.TeleportStatus = packet.Status;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameWndProc(WndEventArgs args)
        {
            try
            {
                if (!Menu.Item(Name + "Clickable").GetValue<bool>())
                {
                    return;
                }

                var index = 0;

                var hudWidth = (float) Math.Ceiling(HudWidth * _scale);
                var hudHeight = (float) Math.Ceiling(HudHeight * _scale);

                var spacing =
                    (float)
                        Math.Ceiling((10f + Menu.Item(Menu.Name + "DrawingSpacing").GetValue<Slider>().Value) * _scale) +
                    hudHeight;

                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight / 2;
                var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value -
                                  (hudWidth + (float) Math.Ceiling(4 * _scale)) / 2f;

                if (args.Msg == (uint) WindowsMessages.WM_RBUTTONUP ||
                    args.Msg == (uint)WindowsMessages.WM_LBUTTONDBLCLK)
                {
                    var pos = Utils.GetCursorPos();
                    foreach (var enemy in _enemyObjects)
                    {
                        var offset = spacing * index;
                        if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDBLCLK)
                        {
                            var spellData = _spellDatas[enemy.Unit.NetworkId];
                            for (var i = 0; spellData.Count > i; i++)
                            {
                                var spell = spellData[i];
                                if (spell != null)
                                {
                                    if (Utils.IsUnderRectangle(
                                        pos, offsetRight + hudWidth * 0.359f - SummonerWidth / 2f,
                                        offsetTop - hudHeight * 0.28f + offset + (float) Math.Ceiling(26 * _scale) * i -
                                        SummonerHeight / 2f, SummonerWidth, SummonerHeight))
                                    {
                                        var teleportCd = 0f;
                                        if (spell.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase) &&
                                            _teleports.ContainsKey(enemy.Unit.NetworkId))
                                        {
                                            _teleports.TryGetValue(enemy.Unit.NetworkId, out teleportCd);
                                        }
                                        var time = (teleportCd > 0.1f ? teleportCd : spell.CooldownExpires) - Game.Time;
                                        if (time > 0 && Environment.TickCount > _lastChatSend + 1500)
                                        {
                                            _lastChatSend = Environment.TickCount;
                                            var sName = ReadableSummonerName(spell.Name);
                                            Game.Say(
                                                string.Format(
                                                    "{0} {1} {2}", enemy.Unit.ChampionName, sName,
                                                    ((float) (Math.Round(time * 2f, MidpointRounding.AwayFromZero) / 2f))
                                                        .FormatTime()));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (enemy.Unit.IsVisible && !enemy.Unit.IsDead &&
                                Utils.IsUnderRectangle(
                                    pos, offsetRight - hudWidth / 2f + hudWidth * 0.1f,
                                    offsetTop + offset - hudHeight / 2f, hudWidth, hudHeight))
                            {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, enemy.Unit);
                            }
                        }
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class EnemyObject
        {
            private Packet.S2C.Teleport.Status _teleportStatus;

            public EnemyObject(Obj_AI_Hero unit, Texture texture)
            {
                TeleportStatus = Packet.S2C.Teleport.Status.Unknown;
                Unit = unit;
                Texture = texture;
                RSpell = unit.GetSpell(SpellSlot.R);
                LastVisible = Game.Time;
                LastPosition = Vector3.Zero;
            }

            public Texture Texture { get; private set; }
            public SpellDataInst RSpell { get; private set; }
            public Obj_AI_Hero Unit { get; private set; }
            public float DeathEndTime { get; set; }
            public float LastVisible { get; set; }
            public Vector3 LastPosition { get; set; }
            public float LastTeleportStatusTime { get; private set; }

            public Packet.S2C.Teleport.Status TeleportStatus
            {
                get { return _teleportStatus; }
                set
                {
                    _teleportStatus = value;
                    LastTeleportStatusTime = Game.Time;
                }
            }
        }
    }
}