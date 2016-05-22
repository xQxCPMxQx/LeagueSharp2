#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ItemFlags.cs is part of SFXChallenger.

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

#endregion

namespace SFXChallenger.Enumerations
{
    [Flags]
    public enum ItemFlags
    {
        Offensive = 0,
        Defensive = 1 << 0,
        Supportive = 1 << 1,
        Flee = 1 << 2
    }
}