#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 HeroListManagerArgs.cs is part of SFXChallenger.

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

namespace SFXChallenger.Args
{
    public class HeroListManagerArgs
    {
        public HeroListManagerArgs(string uniqueId)
        {
            UniqueId = uniqueId;
            MenuTag = 0;
            EnabledButton = true;
            Enabled = true;
        }

        public string UniqueId { get; private set; }
        public bool IsWhitelist { get; set; }
        public bool Allies { get; set; }
        public bool Enemies { get; set; }
        public bool DefaultValue { get; set; }
        public bool DontSave { get; set; }
        public int MenuTag { get; set; }
        public bool EnabledButton { get; set; }
        public bool Enabled { get; set; }
    }
}