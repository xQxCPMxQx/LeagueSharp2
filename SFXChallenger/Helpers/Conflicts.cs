#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Conflicts.cs is part of SFXChallenger.

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
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Extensions.NET;

#endregion

namespace SFXChallenger.Helpers
{
    internal static class Conflicts
    {
        private static readonly List<string> AssemblyBlacklist;

        static Conflicts()
        {
            AssemblyBlacklist = new List<string>
            {
                "mscorlib",
                "System",
                "Microsoft",
                "SMDiagnostics",
                "LeagueSharp.Common",
                "Activator",
                "Utility",
                "Awareness",
                "Evade",
                "Tracker",
                "Smite",
                Global.Name
            };
        }

        public static void Check(string championName)
        {
            try
            {
                var messages = new List<string>();
                var orbwalkers = Orbwalking.Orbwalker.Instances.Count +
                                 SFXTargetSelector.Orbwalking.Orbwalker.Instances.Count;
                if (orbwalkers > 1)
                {
                    messages.Add(string.Format("Possible Conflict: Multiple Orbwalkers ({0})", orbwalkers));
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in
                    assemblies.Where(
                        a => !AssemblyBlacklist.Any(b => a.FullName.Contains(b, StringComparison.OrdinalIgnoreCase))))
                {
                    try
                    {
                        if (
                            assembly.GetTypes()
                                .Where(t => t.IsClass)
                                .Any(t => t.FullName.Contains(championName, StringComparison.OrdinalIgnoreCase)))
                        {
                            var asm = assembly.GetName();
                            messages.Add(string.Format("Possible Conflict: {0} v{1}", asm.Name, asm.Version));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var message in messages)
                {
                    Console.WriteLine("{0} - {1}", Global.Name, message);
                    var chatSplitted = message.Split(':');
                    if (chatSplitted.Length == 2)
                    {
                        Game.PrintChat(
                            string.Format(
                                "{0} - <font color='#FF2929'>{1}</font>:<font color='#FFDF29'>{2}</font>", Global.Name,
                                chatSplitted[0], chatSplitted[1]));
                    }
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}