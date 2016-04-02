#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AutoLantern.cs is part of SFXUtility.

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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Logger;

#endregion

namespace SFXUtility.Features.Others
{
    internal class AutoLantern : Child<Others>
    {
        private GameObject _lantern;

        public AutoLantern(Others parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Auto Lantern"; }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;
            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                Menu.AddItem(new MenuItem(Name + "Percent", "Health Percent").SetValue(new Slider(20)));
                Menu.AddItem(new MenuItem(Name + "Hotkey", "Hotkey").SetValue(new KeyBind('U', KeyBindType.Press)));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
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
                if (
                    !GameObjects.AllyHeroes.Any(
                        a => !a.IsMe && a.ChampionName.Equals("Thresh", StringComparison.OrdinalIgnoreCase)))
                {
                    OnUnload(null, new UnloadEventArgs(true));
                }
                base.OnInitialize();
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
                if (!sender.IsValid || !sender.IsAlly || sender.Type != GameObjectType.obj_AI_Minion)
                {
                    return;
                }
                if (sender.Name.Equals("ThreshLantern", StringComparison.OrdinalIgnoreCase))
                {
                    _lantern = sender;
                }
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
                if (!sender.IsValid || _lantern == null)
                {
                    return;
                }
                if (sender.NetworkId == _lantern.NetworkId)
                {
                    _lantern = null;
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
                if (ObjectManager.Player.IsDead || _lantern == null || !_lantern.IsValid)
                {
                    return;
                }
                if (ObjectManager.Player.HealthPercent <= Menu.Item(Name + "Percent").GetValue<Slider>().Value ||
                    Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    if (_lantern.Position.Distance(ObjectManager.Player.Position) <= 500)
                    {
                        ObjectManager.Player.Spellbook.CastSpell((SpellSlot) 62, _lantern);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}