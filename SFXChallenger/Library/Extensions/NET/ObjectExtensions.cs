#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ObjectExtensions.cs is part of SFXChallenger.

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

using System.IO;
using System.Xml.Serialization;

#endregion

namespace SFXChallenger.Library.Extensions.NET
{
    public static class ObjectExtensions
    {
        public static string ToXml(this object obj)
        {
            var s = new XmlSerializer(obj.GetType());
            using (var writer = new StringWriter())
            {
                s.Serialize(writer, obj);
                return writer.ToString();
            }
        }

        public static T FromXml<T>(this string data)
        {
            var s = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(data))
            {
                var obj = s.Deserialize(reader);
                return (T) obj;
            }
        }
    }
}