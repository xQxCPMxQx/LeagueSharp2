#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ConsoleLogger.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections;
using System.Linq;
using SFXChallenger.Library.Extensions.NET;

#endregion

namespace SFXChallenger.Library.Logger
{
    public class ConsoleLogger : ILogger
    {
        public LogLevel LogLevel { get; set; }

        public void AddItem(LogItem item)
        {
            if (LogLevel == LogLevel.None || item == null || string.IsNullOrWhiteSpace(item.Exception.ToString()))
            {
                return;
            }

            try
            {
                OnItemAdded.RaiseEvent(item, new EventArgs());

                var text = item.Exception.ToString();
                text = item.Exception.Data.Cast<DictionaryEntry>()
                    .Aggregate(
                        text,
                        (current, entry) =>
                            current + string.Format("{0}{1}: {2}", Environment.NewLine, entry.Key, entry.Value));


                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(text);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public event EventHandler OnItemAdded;
    }
}