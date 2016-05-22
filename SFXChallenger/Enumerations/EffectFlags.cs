#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 EffectFlags.cs is part of SFXChallenger.

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
    public enum EffectFlags
    {
        Damage = 0,
        AttackSpeed = 1 << 0,
        AttackSlow = 1 << 1,
        MovementSpeed = 1 << 2,
        MovementSlow = 1 << 3,
        Heal = 1 << 4,
        Shield = 1 << 5,
        RemoveStun = 1 << 6
    }
}