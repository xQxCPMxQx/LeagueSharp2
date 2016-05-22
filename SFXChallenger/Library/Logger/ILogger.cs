#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ILogger.cs is part of SFXChallenger.

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
using System.ComponentModel;

#endregion

namespace SFXChallenger.Library.Logger
{
    public interface ILogger
    {
        LogLevel LogLevel { get; set; }
        void AddItem(LogItem item);
        event EventHandler OnItemAdded;
    }

    [DefaultValue(High)]
    public enum LogLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public class LogItem
    {
        public LogItem(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }
}