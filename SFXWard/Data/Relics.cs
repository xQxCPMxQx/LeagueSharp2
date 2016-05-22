#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Relics.cs is part of SFXWard.

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
    public class Relics
    {
        public static List<RelicObject> Objects;

        static Relics()
        {
            try
            {
                Objects = new List<RelicObject>
                {
                    new RelicObject(
                        180f, 90f, new Vector3(7714.63f, 6740.41f, -69.78f), "HA_AP_healingBuff.troy",
                        Utility.Map.MapType.TwistedTreeline),
                    new RelicObject(
                        120f, 33f, new Vector3(3645.83f, 1491.24f, -174.62f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(10241.72f, 1518.42f, -173.45f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(6946.23f, 2861.19f, -170.78f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(4323.20f, 5495.17f, -170.43f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(9575.73f, 5505.62f, -174.26f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(1026.65f, 8283.56f, -175.79f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(4971.37f, 9319.01f, -171.66f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(8961.69f, 9331.06f, -171.00f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(12878.41f, 8299.77f, -174.73f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        120f, 33f, new Vector3(6950.65f, 12107.11f, -164.55f), "odin_heal_rune.troy",
                        Utility.Map.MapType.CrystalScar),
                    new RelicObject(
                        190f, 40f, new Vector3(4788.07f, 3946.24f, -178.31f), "HA_AP_healingBuff.troy",
                        Utility.Map.MapType.HowlingAbyss),
                    new RelicObject(
                        190f, 40f, new Vector3(5932.09f, 5198.93f, -178.31f), "HA_AP_healingBuff.troy",
                        Utility.Map.MapType.HowlingAbyss),
                    new RelicObject(
                        190f, 40f, new Vector3(7585.05f, 6791.29f, -178.31f), "HA_AP_healingBuff.troy",
                        Utility.Map.MapType.HowlingAbyss),
                    new RelicObject(
                        190f, 40f, new Vector3(8894.17f, 7884.20f, -178.31f), "HA_AP_healingBuff.troy",
                        Utility.Map.MapType.HowlingAbyss)
                };
            }
            catch (Exception ex)
            {
                Objects = new List<RelicObject>();
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public class RelicObject
        {
            public RelicObject(float spawnTime,
                float respawnTime,
                Vector3 position,
                string objectName,
                Utility.Map.MapType mapType)
            {
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                ObjectName = objectName;
                Position = position;
                MinimapPosition = Drawing.WorldToMinimap(Position);
                MapType = mapType;
            }

            public float SpawnTime { get; set; }
            public float RespawnTime { get; private set; }
            public string ObjectName { get; set; }
            public Vector3 Position { get; private set; }
            public Vector2 MinimapPosition { get; private set; }
            public Utility.Map.MapType MapType { get; set; }
        }
    }
}