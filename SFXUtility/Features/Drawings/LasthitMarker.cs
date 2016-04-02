#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 LasthitMarker.cs is part of SFXUtility.

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
using SFXUtility.Library.Logger;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class LasthitMarker : Child<Drawings>
    {
        private IEnumerable<Obj_AI_Minion> _minions = new List<Obj_AI_Minion>();

        public LasthitMarker(Drawings parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Lasthit Marker"; }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (!_minions.Any())
                {
                    return;
                }

                var circleColor = Menu.Item(Name + "DrawingCircleColor").GetValue<Color>();
                var hpKillableColor = Menu.Item(Name + "DrawingHpBarKillableColor").GetValue<Color>();
                var hpUnkillableColor = Menu.Item(Name + "DrawingHpBarUnkillableColor").GetValue<Color>();
                var hpLinesThickness = Menu.Item(Name + "DrawingHpBarLineThickness").GetValue<Slider>().Value;
                var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
                var thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;

                var hpBar = Menu.Item(Name + "DrawingHpBarEnabled").GetValue<bool>();
                var circle = Menu.Item(Name + "DrawingCircleEnabled").GetValue<bool>();
                var prediction = Menu.Item(Name + "Prediction").GetValue<bool>();

                foreach (var minion in _minions)
                {
                    var health = prediction
                        ? HealthPrediction.GetHealthPrediction(
                            minion,
                            (int) (ObjectManager.Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                            1000 * (int) ObjectManager.Player.Distance(minion) /
                            (int)
                                (ObjectManager.Player.CombatType == GameObjectCombatType.Melee ||
                                 ObjectManager.Player.ChampionName == "Azir"
                                    ? float.MaxValue
                                    : ObjectManager.Player.BasicAttack.MissileSpeed), 0)
                        : minion.Health;
                    var aaDamage = ObjectManager.Player.GetAutoAttackDamage(minion, true);
                    var killable = health <= aaDamage;
                    if (hpBar)
                    {
                        var barPos = minion.HPBarPosition;
                        var isSuper = minion.CharData.BaseSkinName.Contains("Super");
                        var barWidth = isSuper ? 88 : 63;
                        var xOffset = (isSuper ? 56 : 37) + 8;
                        var offset = (float) (barWidth / (minion.MaxHealth / aaDamage));
                        offset = (offset < barWidth ? offset : barWidth) - hpLinesThickness / 2f;
                        Drawing.DrawLine(
                            new Vector2(barPos.X + xOffset + offset, barPos.Y + 17),
                            new Vector2(barPos.X + xOffset + offset, barPos.Y + 24), hpLinesThickness,
                            killable ? hpKillableColor : hpUnkillableColor);
                    }
                    if (circle && killable)
                    {
                        Render.Circle.DrawCircle(
                            minion.Position, minion.BoundingRadius + radius, circleColor, thickness);
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

                var drawingHpBarMenu = new Menu("HpBar", drawingMenu.Name + "HpBar");
                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "KillableColor", "Killable Color").SetValue(Color.Green));
                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "UnkillableColor", "Unkillable Color").SetValue(Color.White));
                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "LineThickness", "Line Thickness").SetValue(
                        new Slider(1, 1, 10)));
                drawingHpBarMenu.AddItem(new MenuItem(drawingHpBarMenu.Name + "Enabled", "Enabled").SetValue(false));

                var drawingCirclesMenu = new Menu("Circle", drawingMenu.Name + "Circle");
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Color", "Color").SetValue(Color.Fuchsia));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Radius", "Radius").SetValue(new Slider(30)));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Thickness", "Thickness").SetValue(new Slider(2, 1, 10)));
                drawingCirclesMenu.AddItem(new MenuItem(drawingCirclesMenu.Name + "Enabled", "Enabled").SetValue(false));

                drawingMenu.AddSubMenu(drawingHpBarMenu);
                drawingMenu.AddSubMenu(drawingCirclesMenu);

                Menu.AddSubMenu(drawingMenu);
                Menu.AddItem(new MenuItem(Name + "Prediction", "Prediction").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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
                _minions = GameObjects.EnemyMinions.Where(m => m.IsHPBarRendered && m.IsValidTarget());
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}