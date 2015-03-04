#region License

/*
 Copyright 2014 - 2014 Nikita Bernthaler
 Health.cs is part of SFXUtility.
 
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

#endregion

namespace SFXUtility.Feature
{
	#region

	using System;
	using System.Globalization;
	using Class;
	using IoCContainer;
	using LeagueSharp;
	using LeagueSharp.Common;
	using SharpDX;
	using SharpDX.Direct3D9;

	#endregion

	internal class Health : Base
	{
		#region Constructors
		
		private Font _miniMapFont;

		public Health(Container container)
			: base(container)
		{
			CustomEvents.Game.OnGameLoad += OnGameLoad;
		}

		#endregion

		#region Properties

		public override bool Enabled
		{
			get { return Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
		}

		public override string Name
		{
			get { return "Health"; }
		}

		#endregion

		#region Methods

		private void InhibitorHealth()
		{
			if (!Menu.Item(Name + "InhibitorEnabled").GetValue<bool>())
				return;
			foreach (var inhibitor in ObjectManager.Get<Obj_BarracksDampener>())
			{
				if (inhibitor.IsValid && !inhibitor.IsDead && inhibitor.Health > 0.1f && !inhibitor.IsInvulnerable
				   && ((int) ((inhibitor.Health/inhibitor.MaxHealth)*100)) < 100)
				{
					var percent = ((int) ((inhibitor.Health/inhibitor.MaxHealth)*100));
					var text = Menu.Item(Name + "InhibitorPercentage").GetValue<bool>()
						? (percent == 0 ? 1 : percent).ToString(CultureInfo.InvariantCulture)
						: ((int) inhibitor.Health).ToString(CultureInfo.InvariantCulture);
					var pos = Drawing.WorldToMinimap(inhibitor.Position);
					Class.Helper.DrawText(_miniMapFont,text,(int) pos.X, (int) pos.Y - 8, Color.White);
				}
			}
		}

		private void Drawing_OnEndScene(EventArgs args)
		{
			try
			{
				if (!Enabled)
					return;

				InhibitorHealth();
				TurretHealth();
			}
			catch (Exception ex)
			{
				Logger.WriteBlock(ex.Message, ex.ToString());
			}
		}

		private void OnGameLoad(EventArgs args)
		{
			try
			{
				Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

				Menu = new Menu(Name, Name);

				var inhibitorMenu = new Menu("Inhibitor", Name + "Inhibitor");
				inhibitorMenu.AddItem(new MenuItem(Name + "InhibitorColor", "Color").SetValue(Color.Yellow));
				inhibitorMenu.AddItem(new MenuItem(Name + "InhibitorEnabled", "Enabled").SetValue(true));
				inhibitorMenu.AddItem(new MenuItem(Name + "InhibitorPercentage", "Percentage").SetValue(true));

				var turretMenu = new Menu("Turret", Name + "Turret");
				turretMenu.AddItem(new MenuItem(Name + "TurretColor", "Color").SetValue(Color.Yellow));
				turretMenu.AddItem(new MenuItem(Name + "TurretEnabled", "Enabled").SetValue(true));
				turretMenu.AddItem(new MenuItem(Name + "TurretPercentage", "Percentage").SetValue(true));

				Menu.AddSubMenu(inhibitorMenu);
				Menu.AddSubMenu(turretMenu);

				Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

				BaseMenu.AddSubMenu(Menu);
				
				_miniMapFont = new Font(Drawing.Direct3DDevice, new System.Drawing.Font("Times New Roman", 8));

				Drawing.OnEndScene += Drawing_OnEndScene;

				Initialized = true;
			}
			catch (Exception ex)
			{
				Logger.WriteBlock(ex.Message, ex.ToString());
			}
		}

		private void TurretHealth()
		{
			if (!Menu.Item(Name + "TurretEnabled").GetValue<bool>())
				return;
			foreach (Obj_AI_Turret turret in ObjectManager.Get<Obj_AI_Turret>())
			{
				if (turret.IsValid && !turret.IsDead && turret.Health > 0f && turret.Health < 9999f && !turret.IsInvulnerable
				    && ((int) ((turret.Health/turret.MaxHealth)*100)) < 100)
				{
					var percent = ((int) ((turret.Health/turret.MaxHealth)*100));
					var text = Menu.Item(Name + "TurretPercentage").GetValue<bool>()
						? (percent == 0 ? 1 : percent).ToString(CultureInfo.InvariantCulture)
						: ((int) turret.Health).ToString(CultureInfo.InvariantCulture);
					var pos = Drawing.WorldToMinimap(turret.Position);
					Class.Helper.DrawText(_miniMapFont,text,(int) pos.X, (int) pos.Y , Color.White);
				}
			}
		}

		#endregion
	}
}