#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Altars.cs is part of SFXWard.

 SFXWard is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXWard is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXWard. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SFXWard.Library.Logger;
using SharpDX;

#endregion

namespace SFXWard.Data
{
    internal class Altars
    {
        public static List<AltarObject> Objects;

        static Altars()
        {
            try
            {
                Objects = new List<AltarObject>
                {
                    new AltarObject(
                        180f, 90f, new Vector3(5335.43f, 6742.55f, -37.24f), "TT_LockComplete_Blue_L.troy",
                        "TT_LockComplete_Red_L.troy", Utility.Map.MapType.TwistedTreeline),
                    new AltarObject(
                        180f, 90f, new Vector3(10069.46f, 6744.67f, -37.30f), "TT_LockComplete_Blue_R.troy",
                        "TT_LockComplete_Red_R.troy", Utility.Map.MapType.TwistedTreeline)
                };
            }
            catch (Exception ex)
            {
                Objects = new List<AltarObject>();
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public class AltarObject
        {
            public AltarObject(float spawnTime,
                float respawnTime,
                Vector3 position,
                string objectNameAlly,
                string objectNameEnemy,
                Utility.Map.MapType mapType)
            {
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                ObjectNameAlly = objectNameAlly;
                ObjectNameEnemy = objectNameEnemy;
                Position = position;
                MinimapPosition = Drawing.WorldToMinimap(Position);
                MapType = mapType;
            }

            public float SpawnTime { get; set; }
            public float RespawnTime { get; private set; }
            public string ObjectNameAlly { get; set; }
            public string ObjectNameEnemy { get; set; }
            public Vector3 Position { get; private set; }
            public Vector2 MinimapPosition { get; private set; }
            public Utility.Map.MapType MapType { get; set; }
        }
    }
}