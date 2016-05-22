#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ward.cs is part of SFXWard.

 SFXWard is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXWard is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXWard. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXWard.Classes;
using SFXWard.Library;
using SFXWard.Library.Extensions.NET;
using SFXWard.Library.Extensions.SharpDX;
using SFXWard.Library.Logger;
using SFXWard.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXWard.Features.Trackers
{
    internal class Ward : Child<App>
    {
        private const float CheckInterval = 300f;
        private readonly List<HeroWard> _heroNoWards = new List<HeroWard>();
        private readonly List<WardObject> _wardObjects = new List<WardObject>();

        private readonly List<WardStruct> _wardStructs = new List<WardStruct>
        {
            new WardStruct(60 * 1, 1100, "YellowTrinket", "TrinketTotemLvl1", WardType.Green),
            new WardStruct(60 * 1, 1100, "BlueTrinket", "TrinketOrbLvl3", WardType.Green),
            new WardStruct(60 * 2, 1100, "YellowTrinketUpgrade", "TrinketTotemLvl2", WardType.Green),
            new WardStruct(60 * 3, 1100, "SightWard", "ItemGhostWard", WardType.Green),
            new WardStruct(60 * 3, 1100, "SightWard", "SightWard", WardType.Green),
            new WardStruct(60 * 3, 1100, "MissileWard", "MissileWard", WardType.Green),
            new WardStruct(int.MaxValue, 1100, "VisionWard", "VisionWard", WardType.Pink),
            new WardStruct(60 * 4, 212, "CaitlynTrap", "CaitlynYordleTrap", WardType.Trap),
            new WardStruct(60 * 10, 212, "TeemoMushroom", "BantamTrap", WardType.Trap),
            new WardStruct(60 * 1, 212, "ShacoBox", "JackInTheBox", WardType.Trap),
            new WardStruct(60 * 2, 212, "Nidalee_Spear", "Bushwhack", WardType.Trap),
            new WardStruct(60 * 10, 212, "Noxious_Trap", "BantamTrap", WardType.Trap)
        };

        private Texture _greenWardTexture;
        private float _lastCheck = Environment.TickCount;
        private Line _line;
        private Texture _pinkWardTexture;
        private Sprite _sprite;
        private Font _text;

        public Ward(App parent) : base(parent)
        {
            OnLoad();
        }

        protected override List<Utility.Map.MapType> BlacklistedMaps
        {
            get { return new List<Utility.Map.MapType> { Utility.Map.MapType.CrystalScar }; }
        }

        public override string Name
        {
            get { return "Ward"; }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            Drawing.OnEndScene += OnDrawingEndScene;
            Game.OnWndProc += OnGameWndProc;
            AttackableUnit.OnEnterVisiblityClient += OnAttackableUnitEnterVisiblityClient;

            base.OnEnable();
        }


        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;
            Drawing.OnEndScene -= OnDrawingEndScene;
            Game.OnWndProc += OnGameWndProc;
            AttackableUnit.OnEnterVisiblityClient -= OnAttackableUnitEnterVisiblityClient;

            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "TimeFormat", "Time Format").SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "CircleRadius", "Circle Radius").SetValue(new Slider(150, 25, 300)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "CircleThickness", "Circle Thickness").SetValue(
                        new Slider(2, 1, 10)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "GreenCircle", "Green Circle").SetValue(true));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "GreenColor", "Green Color").SetValue(Color.Lime));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "PinkColor", "Pink Color").SetValue(Color.Magenta));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "TrapColor", "Trap Color").SetValue(Color.Red));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "VisionRange", "Vision Range").SetValue(true));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Minimap", "Minimap").SetValue(true));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "FilterWards", "Filter Wards").SetValue(new Slider(250, 0, 600)));
                Menu.AddItem(new MenuItem(Name + "Hotkey", "Hotkey").SetValue(new KeyBind(16, KeyBindType.Press)));
                Menu.AddItem(new MenuItem(Name + "PermaShow", "Perma Show").SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                Parent.Menu.AddSubMenu(Menu);

                _sprite = MDrawing.GetSprite();
                _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);
                _line = MDrawing.GetLine(Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value);
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
                _greenWardTexture = Resources.WT_Green.ToTexture();
                _pinkWardTexture = Resources.WT_Pink.ToTexture();

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnAttackableUnitEnterVisiblityClient(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.IsDead || !sender.IsEnemy)
                {
                    return;
                }
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    if (ItemData.Sightstone.GetItem().IsOwned(hero) || ItemData.Ruby_Sightstone.GetItem().IsOwned(hero) ||
                        ItemData.Vision_Ward.GetItem().IsOwned(hero))
                    {
                        _heroNoWards.RemoveAll(h => h.Hero.NetworkId == hero.NetworkId);
                    }
                    else
                    {
                        _heroNoWards.Add(new HeroWard(hero));
                    }
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
                if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONDBLCLK &&
                    Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    var ward = _wardObjects.OrderBy(w => Game.CursorPos.Distance(w.Position)).FirstOrDefault();
                    if (ward != null && Game.CursorPos.Distance(ward.Position) <= 300)
                    {
                        _wardObjects.Remove(ward);
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

                var totalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var circleRadius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
                var circleThickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;
                var visionRange = Menu.Item(Name + "DrawingVisionRange").GetValue<bool>();
                var minimap = Menu.Item(Name + "DrawingMinimap").GetValue<bool>();
                var greenCircle = Menu.Item(Name + "DrawingGreenCircle").GetValue<bool>();
                var hotkey = Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active;
                var permaShow = Menu.Item(Name + "PermaShow").GetValue<bool>();

                _sprite.Begin(SpriteFlags.AlphaBlend);
                foreach (var ward in _wardObjects)
                {
                    var color =
                        Menu.Item(
                            Name + "Drawing" +
                            (ward.Data.Type == WardType.Green
                                ? "Green"
                                : (ward.Data.Type == WardType.Pink ? "Pink" : "Trap")) + "Color").GetValue<Color>();
                    if (ward.Position.IsOnScreen())
                    {
                        if (greenCircle || ward.Data.Type != WardType.Green)
                        {
                            if (ward.Object == null || !ward.Object.IsValid ||
                                (ward.Object != null && ward.Object.IsValid && !ward.Object.IsVisible))
                            {
                                Render.Circle.DrawCircle(ward.Position, circleRadius, color, circleThickness);
                            }
                        }
                        if (ward.Data.Type == WardType.Green)
                        {
                            _text.DrawTextCentered(
                                string.Format(
                                    "{0} {1} {0}", ward.IsFromMissile ? (ward.Corrected ? "?" : "??") : string.Empty,
                                    (ward.EndTime - Game.Time).FormatTime(totalSeconds)),
                                Drawing.WorldToScreen(ward.Position),
                                new SharpDX.Color(color.R, color.G, color.B, color.A));
                        }
                    }
                    if (minimap && ward.Data.Type != WardType.Trap)
                    {
                        _sprite.DrawCentered(
                            ward.Data.Type == WardType.Green ? _greenWardTexture : _pinkWardTexture,
                            ward.MinimapPosition.To2D());
                    }
                    if (hotkey || permaShow)
                    {
                        if (visionRange)
                        {
                            Render.Circle.DrawCircle(
                                ward.Position, ward.Data.Range, Color.FromArgb(30, color), circleThickness);
                        }
                        if (ward.IsFromMissile)
                        {
                            _line.Begin();
                            _line.Draw(
                                new[]
                                { Drawing.WorldToScreen(ward.StartPosition), Drawing.WorldToScreen(ward.EndPosition) },
                                SharpDX.Color.White);
                            _line.End();
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

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                var ward = sender as Obj_AI_Base;
                if (ward != null && sender.Name.Contains("Ward", StringComparison.OrdinalIgnoreCase))
                {
                    _wardObjects.RemoveAll(w => w.Object != null && w.Object.NetworkId == sender.NetworkId);
                    _wardObjects.RemoveAll(
                        w =>
                            (Math.Abs(w.Position.X - ward.Position.X) <= (w.IsFromMissile ? 25 : 10)) &&
                            (Math.Abs(w.Position.Y - ward.Position.Y) <= (w.IsFromMissile ? 25 : 10)));
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
                var missile = sender as MissileClient;
                if (missile != null && missile.IsValid)
                {
                    if (missile.SpellCaster != null && !missile.SpellCaster.IsAlly && missile.SData != null)
                    {
                        if (missile.SData.Name.Equals("itemplacementmissile", StringComparison.OrdinalIgnoreCase) &&
                            !missile.SpellCaster.IsVisible)
                        {
                            var sPos = missile.StartPosition;
                            var ePos = missile.EndPosition;

                            Utility.DelayAction.Add(
                                1000, delegate
                                {
                                    if (
                                        !_wardObjects.Any(
                                            w =>
                                                w.Position.To2D().Distance(sPos.To2D(), ePos.To2D(), false) < 300 &&
                                                ((int) Game.Time - w.StartT < 2)))
                                    {
                                        var wObj = new WardObject(
                                            GetWardStructForInvisible(sPos, ePos),
                                            new Vector3(ePos.X, ePos.Y, NavMesh.GetHeightForPosition(ePos.X, ePos.Y)),
                                            (int) Game.Time, null, true,
                                            new Vector3(sPos.X, sPos.Y, NavMesh.GetHeightForPosition(sPos.X, sPos.Y)));
                                        CheckDuplicateWards(wObj);
                                        _wardObjects.Add(wObj);
                                    }
                                });
                        }
                    }
                }
                else
                {
                    var wardObject = sender as Obj_AI_Base;
                    if (wardObject != null && wardObject.IsValid && !wardObject.IsAlly)
                    {
                        foreach (var ward in _wardStructs)
                        {
                            if (wardObject.CharData.BaseSkinName.Equals(
                                ward.ObjectBaseSkinName, StringComparison.OrdinalIgnoreCase))
                            {
                                _wardObjects.RemoveAll(
                                    w =>
                                        w.Position.Distance(wardObject.Position) < 300 &&
                                        ((int) Game.Time - w.StartT < 0.5));
                                var wObj = new WardObject(
                                    ward,
                                    new Vector3(wardObject.Position.X, wardObject.Position.Y, wardObject.Position.Z),
                                    (int) (Game.Time - (int) (wardObject.MaxMana - wardObject.Mana)), wardObject);
                                CheckDuplicateWards(wObj);
                                _wardObjects.Add(wObj);
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

        private WardStruct GetWardStructForInvisible(Vector3 start, Vector3 end)
        {
            return
                GameObjects.EnemyHeroes.Where(hero => _heroNoWards.All(h => h.Hero.NetworkId != hero.NetworkId))
                    .Any(hero => hero.Distance(start.Extend(end, start.Distance(end) / 2f)) <= 1500f) &&
                GameObjects.EnemyHeroes.Any(e => e.Level > 3)
                    ? _wardStructs[3]
                    : _wardStructs[0];
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender.IsAlly)
                {
                    return;
                }

                foreach (var ward in _wardStructs)
                {
                    if (args.SData.Name.Equals(ward.SpellName, StringComparison.OrdinalIgnoreCase))
                    {
                        var wObj = new WardObject(
                            ward, ObjectManager.Player.GetPath(args.End).LastOrDefault(), (int) Game.Time);
                        CheckDuplicateWards(wObj);
                        _wardObjects.Add(wObj);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void CheckDuplicateWards(WardObject wObj)
        {
            try
            {
                var range = Menu.Item(Name + "FilterWards").GetValue<Slider>().Value;
                if (wObj.Data.Duration != int.MaxValue)
                {
                    foreach (var obj in _wardObjects.Where(w => w.Data.Duration != int.MaxValue).ToList())
                    {
                        if (wObj.Position.Distance(obj.Position) < range)
                        {
                            _wardObjects.Remove(obj);
                            return;
                        }
                        if (obj.IsFromMissile && !obj.Corrected)
                        {
                            var newPoint = obj.StartPosition.Extend(obj.EndPosition, -(range * 1.5f));
                            if (wObj.Position.Distance(newPoint) < range)
                            {
                                _wardObjects.Remove(obj);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var obj in
                        _wardObjects.Where(
                            w =>
                                w.Data.Duration != int.MaxValue && w.IsFromMissile &&
                                w.Position.Distance(wObj.Position) < 100).ToList())
                    {
                        _wardObjects.Remove(obj);
                        return;
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

                _wardObjects.RemoveAll(
                    w =>
                        (w.EndTime <= Game.Time && w.Data.Duration != int.MaxValue) ||
                        (w.Object != null && !w.Object.IsValid));
                foreach (var hw in _heroNoWards.ToArray())
                {
                    if (hw.Hero.IsVisible)
                    {
                        hw.LastVisible = Game.Time;
                    }
                    else
                    {
                        if (Game.Time - hw.LastVisible >= 15)
                        {
                            _heroNoWards.Remove(hw);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class HeroWard
        {
            public HeroWard(Obj_AI_Hero hero)
            {
                Hero = hero;
                LastVisible = Game.Time;
            }

            public float LastVisible { get; set; }
            public Obj_AI_Hero Hero { get; private set; }
        }

        private class WardObject
        {
            public readonly bool Corrected;
            public readonly Vector3 EndPosition;
            public readonly Vector3 MinimapPosition;
            public readonly Obj_AI_Base Object;
            public readonly Vector3 StartPosition;
            public readonly int StartT;
            private Vector3 _position;

            public WardObject(WardStruct data,
                Vector3 position,
                int startT,
                Obj_AI_Base wardObject = null,
                bool isFromMissile = false,
                Vector3 startPosition = default(Vector3))
            {
                try
                {
                    var pos = position;
                    if (isFromMissile)
                    {
                        var newPos = GuessPosition(startPosition, position);
                        if (!position.X.Equals(newPos.X) || !position.Y.Equals(newPos.Y))
                        {
                            pos = newPos;
                            Corrected = true;
                        }
                        if (!Corrected)
                        {
                            pos = startPosition;
                        }
                    }
                    IsFromMissile = isFromMissile;
                    Data = data;
                    Position = RealPosition(pos);
                    EndPosition = Position.Equals(position) || Corrected ? position : RealPosition(position);
                    MinimapPosition = Drawing.WorldToMinimap(Position).To3D();
                    StartT = startT;
                    StartPosition = startPosition.Equals(default(Vector3)) || Corrected
                        ? startPosition
                        : RealPosition(startPosition);
                    Object = wardObject;
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public Vector3 Position
            {
                get
                {
                    if (Object != null && Object.IsValid && Object.IsVisible)
                    {
                        _position = Object.Position;
                    }
                    return _position;
                }
                private set { _position = value; }
            }

            public bool IsFromMissile { get; private set; }

            public int EndTime
            {
                get { return StartT + Data.Duration; }
            }

            public WardStruct Data { get; private set; }

            private Vector3 GuessPosition(Vector3 start, Vector3 end)
            {
                try
                {
                    var grass = new List<Vector3>();
                    var distance = start.Distance(end);
                    for (var i = 0; i < distance; i++)
                    {
                        var pos = start.Extend(end, i);
                        if (NavMesh.IsWallOfGrass(pos, 1))
                        {
                            grass.Add(pos);
                        }
                    }
                    return grass.Count > 0 ? grass[(int) (grass.Count / 2d + 0.5d * Math.Sign(grass.Count / 2d))] : end;
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return end;
            }

            private Vector3 RealPosition(Vector3 end)
            {
                try
                {
                    if (end.IsWall())
                    {
                        for (var i = 0; i < 500; i = i + 5)
                        {
                            var c = new Geometry.Polygon.Circle(end, i, 15).Points;
                            foreach (var item in c.OrderBy(p => p.Distance(end)).Where(item => !item.IsWall()))
                            {
                                return new Vector3(item.X, item.Y, NavMesh.GetHeightForPosition(item.X, item.Y));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return end;
            }
        }

        private enum WardType
        {
            Green,
            Pink,
            Trap
        }

        private struct WardStruct
        {
            public readonly int Duration;
            public readonly string ObjectBaseSkinName;
            public readonly int Range;
            public readonly string SpellName;
            public readonly WardType Type;

            public WardStruct(int duration, int range, string objectBaseSkinName, string spellName, WardType type)
            {
                Duration = duration;
                Range = range;
                ObjectBaseSkinName = objectBaseSkinName;
                SpellName = spellName;
                Type = type;
            }
        }
    }
}