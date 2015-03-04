#region License

/*
 Copyright 2014 - 2014 Nikita Bernthaler
 RecallTracker.cs is part of SFXUtility.
 
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
	using System.Collections.Generic;
	using System.Drawing;
	using System.Linq;
	using Class;
	using IoCContainer;
	using LeagueSharp;
	using LeagueSharp.Common;

	#endregion

	internal class RecallTracker : Base
	{
		#region Fields

		private readonly List<Recall> _recalls = new List<Recall>();
		private Trackers _trackers;

		#endregion

		#region Constructors

		public RecallTracker(IContainer container)
			: base(container)
		{
			CustomEvents.Game.OnGameLoad += OnGameLoad;
		}

		#endregion

		#region Properties

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
			get { return "Recall"; }
		}

		#endregion

		#region Methods

		private void OnDraw(EventArgs args)
		{
			try
			{
				if (!Enabled)
					return;
				int count = 0;
				foreach (Recall recall in _recalls)
				{
					if (recall.LastStatus != Packet.S2C.Teleport.Status.Unknown)
					{
						var text = recall.ToString();
						if (recall.Update() && !string.IsNullOrWhiteSpace(text))
						{
							Drawing.DrawText(Drawing.Width - 655, Drawing.Height - 200 + 15*count++, recall.ToColor(),
							                 text);
						}
					}
				}
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

		private void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
		{
			try
			{
				if (!Enabled) return;					
				Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
				var recall = _recalls.FirstOrDefault(r => r.Hero.NetworkId == decoded.UnitNetworkId);
				if (!Equals(recall, default(Recall)))
				{
					recall.Duration = decoded.Duration;
					recall.LastStatus = decoded.Status;
					recall.LastStatusType = decoded.Type;
				}
				if (decoded.Status == Packet.S2C.Teleport.Status.Finish)
					IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Finished", decoded.UnitNetworkId);
				if (decoded.Status == Packet.S2C.Teleport.Status.Start)
					IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Started", decoded.UnitNetworkId);
				if (decoded.Status == Packet.S2C.Teleport.Status.Abort)
					IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Aborted", decoded.UnitNetworkId);
				if (decoded.Status == Packet.S2C.Teleport.Status.Unknown)
					IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Unknown", decoded.UnitNetworkId);
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

					var eMenuItem = new MenuItem(Name + "Enabled", "Enabled").SetValue(true);

					eMenuItem.ValueChanged +=
						(sender, args) =>
						IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Enabled", args.GetNewValue<bool>());
					
					Menu.AddItem(eMenuItem);

					_trackers.Menu.AddSubMenu(Menu);

					foreach (
						Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValid && hero.IsEnemy)
					)
					{
						_recalls.Add(new Recall(hero));
					}
					Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
					Drawing.OnDraw += OnDraw;

					IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Enabled", Menu.Item(Name + "Enabled"));

					Initialized = true;
				}
			}
			catch (Exception ex)
			{
				Logger.WriteBlock(ex.Message, ex.ToString());
			}
		}

		#endregion

		#region Nested Types

		private class Recall
		{
			#region Fields

			public readonly Obj_AI_Hero Hero;
			private float _duration;
			private float _lastActionTime;
			private Packet.S2C.Teleport.Status _lastStatus;
			private Packet.S2C.Teleport.Type _lastStatusType;
			private float _recallStart;

			#endregion

			#region Constructors

			public Recall(Obj_AI_Hero hero)
			{
				Hero = hero;
				LastStatus = Packet.S2C.Teleport.Status.Unknown;
				LastStatusType = Packet.S2C.Teleport.Type.Unknown;
			}

			#endregion

			#region Properties
			public Packet.S2C.Teleport.Type LastStatusType
			{
				get {return _lastStatusType;}
				set {_lastStatusType = value;}
			}

			public float Duration
			{
				private get { return _duration; }
				set { _duration = value/1000; }
			}

			public Packet.S2C.Teleport.Status LastStatus
			{
				get { return _lastStatus; }
				set
				{
					_lastStatus = value;
					_recallStart = _lastStatus == Packet.S2C.Teleport.Status.Start
						? Game.Time
						: 0f;
					_lastActionTime = Game.Time;
				}
			}

			#endregion

			#region Methods

			public override string ToString()
			{
				var time = _recallStart + Duration - Game.Time;
				if (time <= 0)
				{
					time = Game.Time - _lastActionTime;
				}
				if (LastStatusType == Packet.S2C.Teleport.Type.Recall)
				{
					if (LastStatus == Packet.S2C.Teleport.Status.Start)
						return string.Format("Recall: {0}({1}%) Recalling ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Finish)
						return string.Format("Recall: {0}({1}%) Recalled ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Abort)
						return string.Format("Recall: {0}({1}%) Aborted", Hero.ChampionName, (int) Hero.HealthPercentage());
				}
				if (LastStatusType == Packet.S2C.Teleport.Type.Teleport)
				{
					if (LastStatus == Packet.S2C.Teleport.Status.Start)
						return string.Format("Teleport: {0}({1}%) Teleporting ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Finish)
						return string.Format("Teleport: {0}({1}%) Ported ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Abort)
						return string.Format("Teleport: {0}({1}%) Aborted", Hero.ChampionName, (int) Hero.HealthPercentage());
				}
				if (LastStatusType == Packet.S2C.Teleport.Type.Shen || LastStatusType == Packet.S2C.Teleport.Type.TwistedFate)
				{
					if (LastStatus == Packet.S2C.Teleport.Status.Start)
						return string.Format("(R): {0}({1}%) Transporting ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Finish)
						return string.Format("(R): {0}({1}%) Ported ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Abort)
						return string.Format("(R): {0}({1}%) Aborted", Hero.ChampionName, (int) Hero.HealthPercentage());
				}
				return string.Empty;
			}

			public Color ToColor()
			{
				if (LastStatus == Packet.S2C.Teleport.Status.Start)
					return Color.Beige;
				if (LastStatus == Packet.S2C.Teleport.Status.Finish)
					return Color.GreenYellow;
				if (LastStatus == Packet.S2C.Teleport.Status.Abort)
					return Color.Red;
				return Color.Black;
			}

			public bool Update()
			{
				var additional = LastStatus == Packet.S2C.Teleport.Status.Start
					? Duration + 20f
					: 20f;
				if (_lastActionTime + additional <= Game.Time)
				{
					_lastActionTime = 0f;
					return false;
				}
				return true;
			}

			#endregion
		}

		#endregion
	}
}