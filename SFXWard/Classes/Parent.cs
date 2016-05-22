#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Parent.cs is part of SFXWard.

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
using LeagueSharp.Common;
using SFXWard.Library.Logger;

#endregion

namespace SFXWard.Classes
{
    public abstract class Parent : Base
    {
        protected Parent()
        {
            OnLoad();
        }

        public override bool Enabled
        {
            get { return !Unloaded && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                Global.SFX.Menu.AddSubMenu(Menu);

                OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}