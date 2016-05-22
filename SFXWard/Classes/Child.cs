#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Child.cs is part of SFXWard.

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
using SFXWard.Interfaces;
using SFXWard.Library.Logger;

#endregion

namespace SFXWard.Classes
{
    public abstract class Child<T> : Base, IChild where T : Parent
    {
        protected Child(T parent)
        {
            Parent = parent;
        }

        public T Parent { get; set; }
        public bool Handled { get; protected set; }

        public override bool Enabled
        {
            get
            {
                return !Unloaded && Parent != null && Parent.Enabled && Menu != null &&
                       Menu.Item(Menu.Name + "Enabled").GetValue<bool>();
            }
        }

        public void HandleEvents()
        {
            try
            {
                if (Parent == null || Parent.Menu == null || Menu == null || Handled)
                {
                    return;
                }

                Parent.Menu.Item(Parent.Name + "Enabled").ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        if (!Unloaded && args.GetNewValue<bool>())
                        {
                            if (Menu != null && Menu.Item(Menu.Name + "Enabled").GetValue<bool>())
                            {
                                OnEnable();
                            }
                        }
                        else
                        {
                            OnDisable();
                        }
                    };

                Menu.Item(Menu.Name + "Enabled").ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                {
                    if (!Unloaded && args.GetNewValue<bool>())
                    {
                        if (Parent.Menu != null && Parent.Menu.Item(Parent.Name + "Enabled").GetValue<bool>())
                        {
                            OnEnable();
                        }
                    }
                    else
                    {
                        OnDisable();
                    }
                };

                if (Enabled)
                {
                    OnEnable();
                }

                Handled = true;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected abstract void OnLoad();
    }
}