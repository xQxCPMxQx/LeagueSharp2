#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AntiFountain.cs is part of SFXUtility.

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
using SharpDX;

#endregion

namespace SFXUtility.Features.Others
{
    internal class AntiFountain : Child<Others>
    {
        private const float FountainRange = 1450f;
        private Obj_AI_Turret _fountain;

        public AntiFountain(Others parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Anti Fountain"; }
        }

        protected override void OnEnable()
        {
            Obj_AI_Base.OnNewPath += OnObjAiBaseNewPath;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnNewPath -= OnObjAiBaseNewPath;

            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

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
                _fountain =
                    GameObjects.EnemyTurrets.FirstOrDefault(
                        t => t.IsEnemy && t.CharData.Name.ToLower().Contains("shrine"));
                if (_fountain == null)
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

        private void OnObjAiBaseNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Path.Any())
            {
                var first = args.Path.FirstOrDefault(p => p.Distance(_fountain.Position) < FountainRange);
                if (!first.Equals(default(Vector3)))
                {
                    ObjectManager.Player.IssueOrder(
                        GameObjectOrder.MoveTo, _fountain.Position.Extend(first, FountainRange));
                }
            }
        }
    }
}