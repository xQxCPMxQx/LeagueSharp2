#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Bootstrap.cs is part of SFXUtility.

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
using System.Reflection;
using LeagueSharp.Common;
using SFXUtility.Interfaces;
using SFXUtility.Library;
using SFXUtility.Library.Logger;

#region Usings-Features

using SFXUtility.Features.Activators;
using SFXUtility.Features.Detectors;
using SFXUtility.Features.Drawings;
using SFXUtility.Features.Events;
using SFXUtility.Features.Others;
using SFXUtility.Features.Timers;
using SFXUtility.Features.Trackers;

#endregion Usings-Features

#endregion

namespace SFXUtility
{
    public class Bootstrap
    {
        public static void Init()
        {
            try
            {
                if (Global.Reset.Enabled)
                {
                    Reset.Force(Global.Name, Global.Reset.MaxAge);
                }

                AppDomain.CurrentDomain.UnhandledException +=
                    delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                    {
                        try
                        {
                            var ex = sender as Exception;
                            if (ex != null)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    };

                #region GameObjects

                GameObjects.Initialize();

                #endregion GameObjects

                Global.SFX = new SFXUtility();

                #region Parents

                var activators = new Activators();
                var detectors = new Detectors();
                var drawings = new Drawings();
                var events = new Events();
                var others = new Others();
                var timers = new Timers();
                var trackers = new Trackers();

                #endregion Parents

                CustomEvents.Game.OnGameLoad += delegate
                {
                    Global.Features.AddRange(
                        new List<IChild>
                        {
                            #region Features

                            new AntiRengar(activators),
                            new AutoJump(activators),
                            new KillSteal(activators),
                            new Potion(activators),
                            new Revealer(activators),
                            new Smite(activators),
                            new Gank(detectors),
                            new Replay(detectors),
                            new Teleport(detectors),
                            new Clock(drawings),
                            new Clone(drawings),
                            new DamageIndicator(drawings),
                            new Health(drawings),
                            new LaneMomentum(drawings),
                            new LasthitMarker(drawings),
                            new PerfectWard(drawings),
                            new Range(drawings),
                            new WallJumpSpot(drawings),
                            new Waypoint(drawings),
                            new AutoLeveler(events),
                            new Game(events),
                            new Trinket(events),
                            new AntiFountain(others),
                            new AutoLantern(others),
                            new Flash(others),
                            new Humanize(others),
                            new MoveTo(others),
                            new Ping(others),
                            new TurnAround(others),
                            new Ability(timers),
                            new Altar(timers),
                            new Cooldown(timers),
                            new Inhibitor(timers),
                            new Jungle(timers),
                            new Relic(timers),
                            new Destination(trackers),
                            new LastPosition(trackers),
                            new Sidebar(trackers),
                            new Ward(trackers)

                            #endregion Features
                        });
                    foreach (var feature in Global.Features)
                    {
                        try
                        {
                            feature.HandleEvents();
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    }
                    try
                    {
                        Update.Check(
                            Global.Name, Assembly.GetExecutingAssembly().GetName().Version, Global.UpdatePath, 10000);
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}