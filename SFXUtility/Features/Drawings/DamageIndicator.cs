#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DamageIndicator.cs is part of SFXUtility.

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
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class DamageIndicator : Child<Drawings>
    {
        private const int BarWidth = 104;
        private const int LineThickness = 9;
        private static readonly Vector2 BarOffset = new Vector2(10f, 29f);

        private readonly List<Spell> _spells = new List<Spell>
        {
            new Spell(SpellSlot.Q),
            new Spell(SpellSlot.W),
            new Spell(SpellSlot.E),
            new Spell(SpellSlot.R)
        };

        private Line _line;

        public DamageIndicator(Drawings parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Damage Indicator"; }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                if (_line != null && !_line.IsDisposed)
                {
                    var color = Menu.Item(Name + "DrawingColor").GetValue<Color>();
                    var alpha = (byte) (Menu.Item(Name + "DrawingOpacity").GetValue<Slider>().Value * 255 / 100);
                    var sharpColor = new ColorBGRA(color.R, color.G, color.B, alpha);

                    foreach (var unit in
                        GameObjects.EnemyHeroes.Where(
                            e => e.IsValid && !e.IsDead && e.IsHPBarRendered && e.Position.IsOnScreen()))
                    {
                        var damage = CalculateComboDamage(unit);
                        if (damage <= 0)
                        {
                            continue;
                        }
                        var damagePercentage = (unit.Health - damage > 0 ? unit.Health - damage : 0) / unit.MaxHealth;
                        var currentHealthPercentage = unit.Health / unit.MaxHealth;
                        var startPoint =
                            new Vector2(
                                (int) (unit.HPBarPosition.X + BarOffset.X + damagePercentage * BarWidth),
                                (int) (unit.HPBarPosition.Y + BarOffset.Y) - 5);
                        var endPoint =
                            new Vector2(
                                (int) (unit.HPBarPosition.X + BarOffset.X + currentHealthPercentage * BarWidth) + 1,
                                (int) (unit.HPBarPosition.Y + BarOffset.Y) - 5);
                        _line.Begin();
                        _line.Draw(new[] { startPoint, endPoint }, sharpColor);
                        _line.End();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnEnable()
        {
            Drawing.OnEndScene += OnDrawingEndScene;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnEndScene -= OnDrawingEndScene;
            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Color", "Color").SetValue(Color.DarkRed));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Opacity", "Opacity").SetValue(new Slider(60, 5)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "AutoAttacks", "Auto Attacks").SetValue(new Slider(2, 0, 5)));
                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _line = MDrawing.GetLine(LineThickness);

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private double CalculateComboDamage(Obj_AI_Hero enemy)
        {
            var damage = 0d;
            try
            {
                foreach (var spell in _spells.Where(spell => spell.IsReady()))
                {
                    switch (spell.DamageType)
                    {
                        case TargetSelector.DamageType.Physical:
                            damage += ObjectManager.Player.CalcDamage(
                                enemy, Damage.DamageType.Physical,
                                ObjectManager.Player.GetSpellDamage(enemy, spell.Slot) *
                                ObjectManager.Player.PercentArmorPenetrationMod);
                            break;
                        case TargetSelector.DamageType.Magical:
                            damage += ObjectManager.Player.CalcDamage(
                                enemy, Damage.DamageType.Magical,
                                ObjectManager.Player.GetSpellDamage(enemy, spell.Slot) *
                                ObjectManager.Player.PercentMagicPenetrationMod);
                            break;
                        case TargetSelector.DamageType.True:
                            damage += ObjectManager.Player.CalcDamage(
                                enemy, Damage.DamageType.True, ObjectManager.Player.GetSpellDamage(enemy, spell.Slot));
                            break;
                    }
                }

                damage += ObjectManager.Player.GetAutoAttackDamage(enemy) *
                          Menu.Item(Name + "AutoAttacks").GetValue<Slider>().Value;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return damage;
        }
    }
}