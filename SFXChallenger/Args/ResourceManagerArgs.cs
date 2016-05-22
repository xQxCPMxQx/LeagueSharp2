#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ResourceManagerArgs.cs is part of SFXChallenger.

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

using System.Collections.Generic;
using SFXChallenger.Enumerations;

#endregion

namespace SFXChallenger.Args
{
    public class ResourceManagerArgs
    {
        public ResourceManagerArgs(string uniqueId,
            ResourceType type,
            ResourceValueType valueType,
            ResourceCheckType checkType)
        {
            UniqueId = uniqueId;
            Type = type;
            ValueType = valueType;
            CheckType = checkType;
            Prefix = null;
            DefaultValue = 0;
            MinValue = 0;
            MaxValue = 100;
            Advanced = false;
        }

        public string UniqueId { get; private set; }
        public ResourceType Type { get; private set; }
        public ResourceValueType ValueType { get; private set; }
        public ResourceCheckType CheckType { get; private set; }
        public string Prefix { get; set; }
        public int DefaultValue { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public bool Advanced { get; set; }
        public SortedList<int, int> LevelRanges { get; set; }
        public List<int> DefaultValues { get; set; }
    }
}