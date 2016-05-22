#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Utils.cs is part of SFXChallenger.

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
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using LeagueSharp;

#endregion

namespace SFXChallenger.Library
{
    public class Utils
    {
        public static SpellSlot GetSpellSlotByChar(string c)
        {
            switch (c.ToUpper())
            {
                case "Q":
                    return SpellSlot.Q;

                case "W":
                    return SpellSlot.W;

                case "E":
                    return SpellSlot.E;

                case "R":
                    return SpellSlot.R;

                default:
                    return SpellSlot.Unknown;
            }
        }

        public static string GetEnumName(SpellSlot slot)
        {
            return Enum.GetName(typeof(SpellSlot), slot);
        }

        public static bool IsXmlValid(string schemaFile, string xmlFile)
        {
            try
            {
                var valid = true;
                var sc = new XmlSchemaSet();
                sc.Add(string.Empty, schemaFile);
                var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = sc };
                settings.ValidationEventHandler += delegate { valid = false; };
                settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.IgnoreWhitespace = true;
                var reader = XmlReader.Create(xmlFile, settings);

                try
                {
                    while (reader.Read()) {}
                }
                catch (XmlException xmlException)
                {
                    Console.WriteLine(xmlException);
                }
                return valid;
            }
            catch
            {
                return false;
            }
        }

        public static string ReadResourceString(string resource, Assembly asm)
        {
            try
            {
                using (var stream = asm.GetManifestResourceStream(resource))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch
            {
                //ignored
            }
            return string.Empty;
        }
    }
}