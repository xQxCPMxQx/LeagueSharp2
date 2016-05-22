#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SimpleFileLogger.cs is part of SFXWard.

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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using LeagueSharp;
using SFXWard.Library.Extensions.NET;

#endregion

namespace SFXWard.Library.Logger
{
    public class SimpleFileLogger : ILogger
    {
        private readonly string _fileName;
        private readonly HashSet<string> _unique = new HashSet<string>();

        public SimpleFileLogger(string logDir, string fileName = "{0}_{1}_{2}.txt")
        {
            LogDir = logDir;
            _fileName = fileName;
            try
            {
                Directory.CreateDirectory(LogDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public Dictionary<string, string> AdditionalData { get; set; }
        public bool Compression { get; set; }
        public bool OutputConsole { get; set; }
        public string LogDir { get; set; }
        public LogLevel LogLevel { get; set; }

        public void AddItem(LogItem item)
        {
            if (LogLevel == LogLevel.None || item == null || string.IsNullOrWhiteSpace(item.Exception.ToString()))
            {
                return;
            }

            try
            {
                var uniqueValue = (item.Exception + AdditionalData.ToDebugString()).Trim();
                if (!_unique.Contains(uniqueValue))
                {
                    OnItemAdded.RaiseEvent(item, new EventArgs());
                    _unique.Add(uniqueValue);

                    var file = Path.Combine(
                        LogDir,
                        string.Format(
                            _fileName, DateTime.Now.ToString("yyyy_MM_dd"), LogLevel.ToString().ToLower(),
                            (item.Exception + AdditionalData.ToDebugString()).ToMd5Hash()));

                    if (File.Exists(file))
                    {
                        return;
                    }

                    AddData(item.Exception);

                    if (OutputConsole)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(item.Exception);
                        Console.ResetColor();
                    }

                    using (
                        var fileStream = new FileStream(
                            file, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        using (Stream gzStream = new GZipStream(fileStream, CompressionMode.Compress, false))
                        {
                            var text = item.Exception.ToString();
                            text = item.Exception.Data.Cast<DictionaryEntry>()
                                .Aggregate(
                                    text,
                                    (current, entry) =>
                                        current +
                                        string.Format("{0}{1}: {2}", Environment.NewLine, entry.Key, entry.Value));

                            if (string.IsNullOrWhiteSpace(text.Trim()))
                            {
                                return;
                            }

                            var logByte = new UTF8Encoding(true).GetBytes(text);

                            if (Compression)
                            {
                                gzStream.Write(logByte, 0, logByte.Length);
                            }
                            else
                            {
                                fileStream.Write(logByte, 0, logByte.Length);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public event EventHandler OnItemAdded;

        private void AddData(Exception ex)
        {
            if (ex != null)
            {
                if (GameObjects.Heroes != null && GameObjects.Heroes.Any())
                {
                    ex.Data.Add("Champion", ObjectManager.Player.ChampionName);
                    ex.Data.Add(
                        "Champions", string.Join(", ", GameObjects.Heroes.Select(e => e.ChampionName).ToArray()));
                }
                ex.Data.Add("Version", Game.Version);
                ex.Data.Add("Region", Game.Region);
                ex.Data.Add("MapId", Game.MapId);
                ex.Data.Add("Type", Game.Type);
            }
        }
    }
}