#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SFXWard.cs is part of SFXWard.

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
using System.IO;
using System.Reflection;
using LeagueSharp.Common;
using SFXWard.Classes;
using SFXWard.Library.Extensions.NET;
using SFXWard.Library.Logger;
using Version = System.Version;

#endregion

namespace SFXWard
{
    public class SFXWard
    {
        private bool _unloadTriggered;

        public SFXWard()
        {
            try
            {
                Menu = new Menu(Name, Name, true);

                var infoMenu = new Menu("Info", Name + "Info");

                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Version", string.Format("{0}: {1}", "Version", Version)));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Forum", "Forum: Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Github", "GitHub: Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "IRC", "IRC: Appril"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Exception", string.Format("{0}: {1}", "Exception", 0)));

                var globalMenu = new Menu("Settings", Name + "Settings");

                #region Fonts

                AddFont(globalMenu);

                #endregion Fonts

                AddReport(globalMenu);

                Menu.AddSubMenu(infoMenu);
                Menu.AddSubMenu(globalMenu);

                AppDomain.CurrentDomain.DomainUnload += OnExit;
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                CustomEvents.Game.OnGameEnd += OnGameEnd;
                CustomEvents.Game.OnGameLoad += OnGameLoad;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public Menu Menu { get; private set; }

        public string Name
        {
            get { return "SFXWard"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public event EventHandler<UnloadEventArgs> OnUnload;

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (!_unloadTriggered)
                {
                    _unloadTriggered = true;

                    OnUnload.RaiseEvent(null, new UnloadEventArgs(true));
                    Notifications.AddNotification(new Notification(Menu.Item(Name + "InfoException").DisplayName));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameEnd(EventArgs args)
        {
            OnExit(null, args);
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Menu.AddToMainMenu();

                var errorText = "Exception";
                Global.Logger.OnItemAdded += delegate
                {
                    try
                    {
                        var text = Menu.Item(Name + "InfoException").DisplayName.Replace(errorText + ": ", string.Empty);
                        int count;
                        if (int.TryParse(text, out count))
                        {
                            Menu.Item(Name + "InfoException").DisplayName = string.Format(
                                "{0}: {1}", errorText, count + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void AddReport(Menu menu)
        {
            try
            {
                menu.AddItem(new MenuItem(menu.Name + "Report", "Generate Report").SetValue(false)).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        try
                        {
                            if (!args.GetNewValue<bool>())
                            {
                                return;
                            }
                            Utility.DelayAction.Add(0, () => menu.Item(menu.Name + "Report").SetValue(false));
                            File.WriteAllText(
                                Path.Combine(Global.BaseDir, string.Format("{0}.report.txt", Global.Name.ToLower())),
                                GenerateReport.Generate());
                            Notifications.AddNotification("Report Generated", 5000);
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

        #region Fonts

        private void AddFont(Menu menu)
        {
            try
            {
                menu.AddItem(
                    new MenuItem(menu.Name + "Font", "Font").SetValue(
                        new StringList(
                            new[]
                            {
                                "Calibri", "Arial", "Tahoma", "Verdana", "Times New Roman", "Lucida Console",
                                "Comic Sans MS"
                            })));

                Global.DefaultFont = menu.Item(menu.Name + "Font").GetValue<StringList>().SelectedValue;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        #endregion Fonts
    }

    public class UnloadEventArgs : EventArgs
    {
        public bool Final;

        public UnloadEventArgs(bool final = false)
        {
            Final = final;
        }
    }
}