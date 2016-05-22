#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Global.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.IO;
using System.Linq;
using SFXChallenger.Interfaces;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger
{
    internal class Global
    {
        public static string Prefix = "SFX";
        public static string Name = "SFXChallenger";
        public static string DefaultFont = "Calibri";
        public static IChampion Champion;
        public static ILogger Logger;
        public static string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static string LogDir = Path.Combine(BaseDir, Name + " - Logs");
        public static string CacheDir = Path.Combine(BaseDir, Name + " - Cache");
        public static string UpdatePath = "Lizzaran/LeagueSharp-Dev/master/SFXChallenger";

        static Global()
        {
            Logger = new SimpleFileLogger(LogDir) { LogLevel = LogLevel.High };

            try
            {
                Directory.GetFiles(LogDir)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < DateTime.Now.AddDays(-7))
                    .ToList()
                    .ForEach(f => f.Delete());
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex));
            }
        }

        public class Testing
        {
            private static readonly string _file = "sfx.testing";
            public static bool Enabled;

            static Testing()
            {
                try
                {
                    var bParent = Directory.GetParent(BaseDir);
                    if (bParent != null)
                    {
                        var bbParent = Directory.GetParent(bParent.FullName);
                        if (bbParent != null)
                        {
                            if (File.Exists(Path.Combine(bbParent.FullName, _file)))
                            {
                                Enabled = true;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("{0} - Testing", Name);
                                Console.ResetColor();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddItem(new LogItem(ex));
                }
            }
        }

        public class Reset
        {
            public static readonly bool Enabled = false;
            public static readonly DateTime MaxAge = new DateTime(2015, 10, 6);
        }
    }
}