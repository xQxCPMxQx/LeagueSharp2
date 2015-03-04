#region License

/*
 Copyright 2014 - 2014 Nikita Bernthaler
 WardTracker.cs is part of SFXUtility.
 
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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Class;
	using IoCContainer;
	using LeagueSharp;
	using LeagueSharp.Common;
	using SharpDX;
	using Color = System.Drawing.Color;
	
	internal class WardTracker : Base
	{
		private Trackers _trackers;
		private List<ListedHO> allObjects = new List<ListedHO>();
		
		public WardTracker(IContainer container)
			: base(container)
		{
			CustomEvents.Game.OnGameLoad += OnGameLoad;
		}
		
		public override bool Enabled
		{
			get
			{
				return _trackers != null && _trackers.Menu != null &&
					_trackers.Menu.Item(_trackers.Name + "Enabled").GetValue<bool>() && Menu != null &&
					Menu.Item(Name + "Enabled").GetValue<bool>();
			}
		}

		public override string Name
		{
			get { return "Ward"; }
		}

		private void OnGameLoad(EventArgs args)
		{
			try
			{
				Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

				if (IoC.IsRegistered<Trackers>() && IoC.Resolve<Trackers>().Initialized)
				{
					TrackersLoaded(IoC.Resolve<Trackers>());
				}
				else
				{
					if (IoC.IsRegistered<Mediator>())
					{
						IoC.Resolve<Mediator>().Register("Trackers_initialized", TrackersLoaded);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.WriteBlock(ex.Message, ex.ToString());
			}
		}
		
		private void TrackersLoaded(object o)
		{
			try
			{
				if (o is Trackers && (o as Trackers).Menu != null)
				{
					_trackers = (o as Trackers);

					Menu = new Menu(Name, Name);

					Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

					_trackers.Menu.AddSubMenu(Menu);

					GameObject.OnCreate += OnCreateObject;
					GameObject.OnDelete += OnDeleteObject;
					Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
					Drawing.OnDraw += OnDraw;
					
					Initialized = true;
				}
			}
			catch (Exception ex)
			{
				Logger.WriteBlock(ex.Message, ex.ToString());
			}
		}
				
		private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.IsAlly || !Enabled) return;
			HidObject ho = HidObjects.IsSpellHidObj(args.SData.Name);
			if (ho != null)
			{
				var endPosition = ObjectManager.Player.GetPath(args.End).ToList().Last();
				allObjects.Add(new ListedHO(ho.Duration,ho.ObjColor,ho.Range,endPosition,Game.Time));
			}
		}
		
		private void OnDeleteObject(GameObject sender, EventArgs args)
		{
			if (!Enabled) return;
			int i=0;
			foreach (var lho in allObjects)
			{
				if (sender.NetworkId == lho.WardObj.NetworkId)
				{
					allObjects.RemoveAt(i);
					break;
				}
				i++;
			}
		}
		
		private void OnCreateObject(GameObject sender, EventArgs args)
		{
			if (sender.Name.Contains("missile") || sender.Name.Contains("Minion") || sender.IsAlly || !Enabled) return;
				
			Obj_AI_Base objis = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(sender.NetworkId);

			HidObject ho = HidObjects.IsHidObj(objis.SkinName);
			
			if (ho != null)
			{
				foreach (var lho in allObjects)
				{
					if (GetNearestWard(lho.Position) != null)
					{
						lho.WardObj = GetNearestWard(lho.Position);
						break;
					}
				}
			}
		}

		private void OnDraw(EventArgs args)
		{
			if (!Enabled) return;
			foreach (var lho in allObjects)
			{
				if ((lho.Duration == -1 || (int)((lho.CreatedAt + lho.Duration + 1) - Game.Time) > 0) && lho.Position.IsOnScreen())
				{
					Render.Circle.DrawCircle(lho.Position, 50, lho.ObjColor);
					if (lho.Duration > 0)
					{
						Vector2 locOnScreen = Drawing.WorldToScreen(lho.Position);
						Drawing.DrawText(locOnScreen.X - 10, locOnScreen.Y - 10, Color.White, "" + (int)((lho.CreatedAt + lho.Duration + 1) - Game.Time));
					}
				}
			}
		}
		
		private Obj_AI_Base GetNearestWard(Vector3 pos)
		{
			return
				ObjectManager.Get<Obj_AI_Base>().OrderBy(wards => pos.Distance(wards.Position))
				.First(ward => ward.IsValid && HidObjects.IsHidObj(ward.SkinName) != null && ward.Position.Distance(pos) <= 200);
		}
		
		private class HidObject
		{
			public string SpellName;
			public string SkinName;
			public int Duration;
			public Color ObjColor;
			public int Range;

			public HidObject(string spellName, string skinName, int duration, Color objColor, int range)
			{
				SpellName = spellName;
				SkinName = skinName;
				Duration = duration;
				ObjColor = objColor;
				Range = range;
			}
		}
		
		private class HidObjects
		{
			public static List<HidObject> HObjects = new List<HidObject>();

			static HidObjects()
			{
				HObjects.Add(new HidObject("TrinketTotemLvl3B","VisionWard", -1, Color.Magenta, 1450));
				HObjects.Add(new HidObject("VisionWard","VisionWard", -1, Color.Magenta, 1450));
				HObjects.Add(new HidObject("TrinketTotemLvl3","SightWard", 180, Color.Lime, 1450));
				HObjects.Add(new HidObject("SightWard","SightWard", 180, Color.Lime, 1450));
				HObjects.Add(new HidObject("ItemGhostWard","SightWard", 180, Color.Lime, 1450));
				HObjects.Add(new HidObject("TrinketTotemLvl1","YellowTrinket", 60, Color.Lime, 1450));
				HObjects.Add(new HidObject("TrinketTotemLvl2","YellowTrinketUpgrade", 120, Color.Lime, 1450));
				HObjects.Add(new HidObject("BantamTrap","TeemoMushroom", 600, Color.Red, 1450));
				HObjects.Add(new HidObject("CaitlynYordleTrap","CaitlynTrap", 240, Color.Red, 1450));
				HObjects.Add(new HidObject("Bushwhack","Nidalee_Spear", 120, Color.Red, 1450));
				HObjects.Add(new HidObject("JackInTheBox","ShacoBox", 60, Color.Red, 1450));
			}

			public static HidObject IsHidObj(string hidName)
			{
				foreach (var hidObj in HObjects)
				{
					if (hidObj.SkinName.ToLower() == hidName.ToLower())
						return hidObj;
				}
				return null;
			}
			
			public static HidObject IsSpellHidObj(string spell)
			{
				foreach (var hidObj in HObjects)
				{
					if (hidObj.SpellName.ToLower() == spell.ToLower())
						return hidObj;
				}
				return null;
			}

		}
		
		private class ListedHO
		{
			public int Duration;
			public System.Drawing.Color ObjColor;
			public int Range;
			public Vector3 Position;
			public float CreatedAt;
			public Obj_AI_Base WardObj;

			public ListedHO(int duration, System.Drawing.Color objColor, int range, Vector3 position, float createdAt, Obj_AI_Base wardObj = null)
			{
				Duration = duration;
				ObjColor = objColor;
				Range = range;
				Position = position;
				CreatedAt = createdAt;
				WardObj = wardObj;
			}
		}
	}
}