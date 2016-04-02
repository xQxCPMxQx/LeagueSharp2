#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Reset.cs is part of SFXUtility.

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
using System.IO;
using System.Linq;

#endregion

namespace SFXUtility.Library
{
    internal class Reset
    {
        public static void Force(string project, DateTime maxAge, Action onResetAction = null)
        {
            try
            {
                var baseDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
                if (baseDir != null && baseDir.Exists)
                {
                    var lsharpDir = Directory.GetParent(baseDir.FullName);
                    if (lsharpDir != null && lsharpDir.Exists)
                    {
                        var didDelete = false;
                        var menuConfigDir = Path.Combine(lsharpDir.FullName, "MenuConfig");
                        if (Directory.Exists(menuConfigDir))
                        {
                            var file = GetFile(menuConfigDir, project);
                            if (file != null)
                            {
                                if (file.CreationTime.ToUniversalTime() <= maxAge.ToUniversalTime())
                                {
                                    try
                                    {
                                        if (onResetAction != null)
                                        {
                                            onResetAction();
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored
                                    }

                                    didDelete = true;
                                    File.Delete(file.FullName);
                                }
                            }
                        }
                        if (didDelete)
                        {
                            var repoDir = Path.Combine(lsharpDir.FullName, "Repositories");
                            if (Directory.Exists(repoDir))
                            {
                                var file = GetFile(repoDir, string.Format("{0}.csproj", project));
                                if (file != null)
                                {
                                    var projDir = Directory.GetParent(file.FullName);
                                    if (projDir != null && projDir.Exists)
                                    {
                                        RecursiveDelete(projDir);
                                    }
                                }
                            }

                            // ReSharper disable once LocalizableElement
                            Console.WriteLine("{0}: Config & Repository reseted.", project);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void RecursiveDelete(DirectoryInfo baseDir)
        {
            try
            {
                if (baseDir == null || !baseDir.Exists)
                {
                    return;
                }
                foreach (var dir in baseDir.EnumerateDirectories())
                {
                    RecursiveDelete(dir);
                }
                baseDir.Delete(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static FileInfo GetFile(string dir, string value)
        {
            return new DirectoryInfo(dir).GetFiles("*" + value + "*.*", SearchOption.AllDirectories).FirstOrDefault();
        }
    }
}