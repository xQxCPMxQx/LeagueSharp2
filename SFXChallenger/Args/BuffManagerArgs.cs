#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 BuffManagerArgs.cs is part of SFXChallenger.

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
using LeagueSharp;
using SharpDX;

#endregion

namespace SFXChallenger.Args
{
    public class BuffManagerArgs : EventArgs
    {
        public BuffManagerArgs(string uniqueId, Obj_AI_Hero hero, Vector3 position, float endTime)
        {
            UniqueId = uniqueId;
            Hero = hero;
            Position = position;
            EndTime = endTime;
        }

        public float EndTime { get; set; }
        public string UniqueId { get; set; }
        public Obj_AI_Hero Hero { get; private set; }
        public Vector3 Position { get; private set; }
    }
}