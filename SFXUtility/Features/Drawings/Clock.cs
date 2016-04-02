#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Clock.cs is part of SFXUtility.

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
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library.Logger;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class Clock : Child<Drawings>
    {
        public Clock(Drawings parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Clock"; }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                Drawing.DrawText(
                    Drawing.Width - Menu.Item(Name + "DrawingOffsetRight").GetValue<Slider>().Value,
                    Menu.Item(Name + "DrawingOffsetTop").GetValue<Slider>().Value,
                    Menu.Item(Name + "DrawingColor").GetValue<Color>(), DateTime.Now.ToShortTimeString());
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnEnable()
        {
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetTop", "Offset Top").SetValue(new Slider(75, 0, 500)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetRight", "Offset Right").SetValue(new Slider(100, 0, 500)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Color", "Color").SetValue(Color.Gold));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}