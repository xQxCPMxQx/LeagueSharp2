#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Report.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Interfaces;

#endregion

namespace SFXUtility.Classes
{
    public class GenerateReport
    {
        private static readonly List<string> AssemblyBlacklist = new List<string>
        {
            "mscorlib",
            "System",
            "Microsoft",
            "SMDiagnostics"
        };

        private static readonly StringBuilder Builder = new StringBuilder();

        public static string Generate()
        {
            Builder.Clear();

            GenerateHeader();
            GenerateGame();
            GenerateOverview();
            GenerateHeroes();
            GenerateAssemblies();
            GenerateFeatures();
            GenerateMenu();

            Builder.AppendLine("--------------- THE END ---------------");

            return Builder.ToString();
        }

        private static void GenerateOverview()
        {
            Builder.AppendLine("Overview");
            Builder.AppendLine("--------------------------------------");

            Builder.Append("Assemblies: ");
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !AssemblyBlacklist.Any(b => a.FullName.StartsWith(b)))
                    .ToList();
            var lastAssembly = assemblies.Last();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var info = assembly.FullName.Split(',');
                    if (info.Length > 0)
                    {
                        Builder.Append(info[0]);
                        Builder.Append(assembly.Equals(lastAssembly) ? Environment.NewLine : ", ");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Builder.Append("Features: ");
            var features = Global.Features.Where(f => f.Enabled && f.Initialized && f.Handled && !f.Unloaded).ToList();
            var lastFeature = features.Last();
            foreach (var feature in Global.Features.Where(f => f.Enabled && f.Initialized && f.Handled && !f.Unloaded))
            {
                try
                {
                    Builder.Append(GetFeatureName(feature));
                    Builder.Append(feature.Equals(lastFeature) ? Environment.NewLine : ", ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Builder.Append("Heroes: ");
            BuildHeroesString(ObjectManager.Get<Obj_AI_Hero>().ToList());
            Builder.Append(Environment.NewLine);
            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static void BuildHeroesString(List<Obj_AI_Hero> heroes)
        {
            var lastHero = heroes.Last();
            foreach (var hero in heroes)
            {
                try
                {
                    Builder.Append(hero.ChampionName);
                    Builder.Append(hero.NetworkId.Equals(lastHero.NetworkId) ? string.Empty : ", ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static void GenerateHeroes()
        {
            Builder.AppendLine("Heroes");
            Builder.AppendLine("--------------------------------------");
            Builder.AppendLine(string.Format("[Self]    : {0}", ObjectManager.Player.ChampionName));
            Builder.Append("[Ally]    : ");
            BuildHeroesString(ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly).ToList());
            Builder.Append(Environment.NewLine);
            Builder.Append("[Enemy]   : ");
            BuildHeroesString(ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy).ToList());
            Builder.Append(Environment.NewLine);
            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static void GenerateHeader()
        {
            Builder.AppendLine("Generated Report");
            Builder.AppendLine("--------------------------------------");
            Builder.AppendLine(string.Format("[Name]    : {0}", Global.Name));
            Builder.AppendLine(string.Format("[Version] : {0}", Global.SFX.Version));
            Builder.AppendLine(string.Format("[Date]    : {0}", DateTime.Now.ToString("dd/MM/yyyy")));
            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static string GetFeatureName(IChild feature)
        {
            var split = feature.ToString().Split('.');
            if (split.Length > 0)
            {
                return split.Last();
            }
            return feature.ToString();
        }

        private static void GenerateFeatures()
        {
            Builder.AppendLine("Activated Features");
            Builder.AppendLine("--------------------------------------");

            foreach (var feature in Global.Features.OrderBy(f => !f.Enabled))
            {
                try
                {
                    Builder.AppendLine();
                    Builder.AppendLine(GetFeatureName(feature));
                    Builder.AppendLine("--------------------------------------");
                    Builder.AppendLine(string.Format("[Name]        : {0}", GetFeatureName(feature)));
                    Builder.AppendLine(string.Format("[Full Name]   : {0}", feature));
                    Builder.AppendLine(string.Format("[Enabled]     : {0}", feature.Enabled));
                    Builder.AppendLine(string.Format("[Handled]     : {0}", feature.Handled));
                    Builder.AppendLine(string.Format("[Initialized] : {0}", feature.Initialized));
                    Builder.AppendLine(string.Format("[Unloaded]    : {0}", feature.Unloaded));
                    Builder.AppendLine("--------------------------------------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static void GenerateGame()
        {
            Builder.AppendLine("Game Information");
            Builder.AppendLine("--------------------------------------");

            Builder.AppendLine(string.Format("[Version] : {0}", Game.Version));
            Builder.AppendLine(string.Format("[Region]  : {0}", Game.Region));
            Builder.AppendLine(string.Format("[MapId]   : {0}", Game.MapId));
            Builder.AppendLine(string.Format("[Type]    : {0}", Game.Type));

            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static void GenerateAssemblies()
        {
            Builder.AppendLine("Loaded Assemblies");
            Builder.AppendLine("--------------------------------------");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies.Where(a => !AssemblyBlacklist.Any(b => a.FullName.StartsWith(b))))
            {
                try
                {
                    Builder.AppendLine();
                    Builder.AppendLine("--------------------------------------");
                    var info = assembly.FullName.Split(',');
                    if (info.Length > 0)
                    {
                        Builder.AppendLine(info[0]);
                    }
                    if (info.Length > 1)
                    {
                        Builder.AppendLine(info[1].Replace(" Version=", string.Empty));
                    }
                    Builder.AppendLine("--------------------------------------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static void GenerateMenu()
        {
            Builder.AppendLine("Menu");
            Builder.AppendLine("--------------------------------------");

            HandleMenu(Global.SFX.Menu);

            Builder.AppendLine();
            Builder.AppendLine();
            Builder.AppendLine();
        }

        private static void HandleMenu(Menu menu, int indent = 0)
        {
            var prefix = string.Empty;
            if (indent > 0)
            {
                prefix = new string('-', indent * 3);
            }
            Builder.AppendLine(string.Format("{0}{1}", prefix, menu.DisplayName));
            foreach (var item in menu.Items)
            {
                Builder.AppendLine(string.Format("{0}{1}: {2}", prefix, item.DisplayName, GetItemValueText(item)));
            }
            foreach (var child in menu.Children)
            {
                HandleMenu(child, indent + 1);
            }
        }

        private static string GetItemValueText(MenuItem item)
        {
            object obj;
            try
            {
                if (item != null)
                {
                    obj = item.GetValue<object>();
                    if (obj is bool)
                    {
                        return string.Format("{0}", (bool) obj);
                    }
                    if (obj is Color)
                    {
                        var color = (Color) obj;
                        return string.Format("({0},{1},{2},{3})", color.R, color.G, color.B, color.A);
                    }
                    if (obj is Circle)
                    {
                        var circle = (Circle) obj;
                        return string.Format(
                            "{0} | ({1},{2},{3},{4})", circle.Active, circle.Color.R, circle.Color.G, circle.Color.B,
                            circle.Color.A);
                    }
                    if (obj is Slider)
                    {
                        var slider = (Slider) obj;
                        return string.Format("{0} | {1} | {2}", slider.Value, slider.MinValue, slider.MaxValue);
                    }
                    if (obj is KeyBind)
                    {
                        var keybind = (KeyBind) obj;
                        return string.Format("{0} | {1} | {2}", keybind.Key, keybind.Active, KeyBindType.Toggle);
                    }
                    if (obj is StringList)
                    {
                        var stringList = (StringList) obj;
                        return string.Format(
                            "{0} | {1} | {2}", stringList.SelectedValue, stringList.SelectedIndex,
                            string.Join(",", stringList.SList));
                    }
                }
                else
                {
                    return "[Null]";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "[Error]";
            }
            return string.Format("[Unknown]{0}", obj != null ? " " + obj.GetType() : " Null");
        }
    }
}