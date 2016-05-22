#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ListExtensions.cs is part of SFXLibrary.

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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

#endregion

namespace SFXChallenger.Library.Extensions.NET
{
    public static class ListExtensions
    {
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> target)
        {
            var r = new Random();
            return target.OrderBy(x => r.Next());
        }

        public static T DeepClone<T>(this T input) where T : ISerializable
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                try
                {
                    formatter.Serialize(stream, input);
                    stream.Position = 0;
                    return (T) formatter.Deserialize(stream);
                }
                catch
                {
                    return default(T);
                }
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            return source.DistinctBy(keySelector, null);
        }

        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            return DistinctByImpl(source, keySelector, comparer);
        }

        private static IEnumerable<TSource> DistinctByImpl<TSource, TKey>(IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            var knownKeys = new HashSet<TKey>(comparer);
            return source.Where(element => knownKeys.Add(keySelector(element)));
        }

        private static IEnumerable<int> ConstructSetFromBits(int i)
        {
            for (var n = 0; i != 0; i /= 2, n++)
            {
                if ((i & 1) != 0)
                {
                    yield return n;
                }
            }
        }

        public static IEnumerable<List<T>> ProduceEnumeration<T>(List<T> list)
        {
            for (var i = 0; i < 1 << list.Count; i++)
            {
                yield return ConstructSetFromBits(i).Select(n => list[n]).ToList();
            }
        }
    }
}