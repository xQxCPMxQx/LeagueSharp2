#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Update.cs is part of SFXWard.

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
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LeagueSharp.Common;
using Version = System.Version;

#endregion

namespace SFXWard.Library
{
    public class Update
    {
        public static void Check(string name, Version version, string path, int displayTime)
        {
            try
            {
                Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                var data =
                                    client.DownloadString(
                                        string.Format(
                                            "https://raw.githubusercontent.com/{0}/Properties/AssemblyInfo.cs", path));

                                var gVersion =
                                    Version.Parse(
                                        new Regex("AssemblyFileVersion\\((\"(.+?)\")\\)").Match(data).Groups[1].Value
                                            .Replace("\"", ""));

                                if (gVersion > version)
                                {
                                    CustomEvents.Game.OnGameLoad +=
                                        delegate
                                        {
                                            Notifications.AddNotification(
                                                string.Format(
                                                    "[{0}] Update available: {1} => {2}!", name, version, gVersion),
                                                displayTime);
                                        };
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}