#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DictionaryExtensions.cs is part of SFXLibrary.

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
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SFXUtility.Library.Extensions.NET
{
    public static class DictionaryExtensions
    {
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public static void RemoveAll<TK, TV>(this IDictionary<TK, TV> dict, Func<TK, TV, bool> match)
        {
            foreach (var key in dict.Keys.ToArray().Where(key => match(key, dict[key])))
            {
                dict.Remove(key);
            }
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary == null
                ? string.Empty
                : string.Join("," + Environment.NewLine, dictionary.Select(kv => kv.Key + " = " + kv.Value));
        }
    }
}