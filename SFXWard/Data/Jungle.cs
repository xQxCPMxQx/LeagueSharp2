#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Jungle.cs is part of SFXWard.

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
    public class Jungle
    {
        public static List<Camp> Camps;

        static Jungle()
        {
            try
            {
                Camps = new List<Camp>
                {
                    // Order: Blue
                    new Camp(
                        100, 300, new Vector3(3800.99f, 7883.53f, 52.18f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Blue1.1.1", true), new Mob("SRU_BlueMini1.1.2"),
                                new Mob("SRU_BlueMini21.1.3")
                            }), true, Utility.Map.MapType.SummonersRift,
                        GameObjectTeam.Order),
                    //Order: Wolves
                    new Camp(
                        100, 100, new Vector3(3849.95f, 6504.36f, 52.46f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Murkwolf2.1.1", true), new Mob("SRU_MurkwolfMini2.1.2"),
                                new Mob("SRU_MurkwolfMini2.1.3")
                            }), false, Utility.Map.MapType.SummonersRift,
                        GameObjectTeam.Order),
                    //Order: Chicken
                    new Camp(
                        100, 100, new Vector3(6943.41f, 5422.61f, 52.62f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Razorbeak3.1.1", true), new Mob("SRU_RazorbeakMini3.1.2"),
                                new Mob("SRU_RazorbeakMini3.1.3"), new Mob("SRU_RazorbeakMini3.1.4")
                            }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Order),
                    //Order: Red
                    new Camp(
                        100, 300, new Vector3(7813.07f, 4051.33f, 53.81f),
                        new List<Mob>(
                            new[]
                            { new Mob("SRU_Red4.1.1", true), new Mob("SRU_RedMini4.1.2"), new Mob("SRU_RedMini4.1.3") }),
                        true, Utility.Map.MapType.SummonersRift, GameObjectTeam.Order),
                    //Order: Krug
                    new Camp(
                        100, 100, new Vector3(8370.58f, 2718.15f, 51.09f),
                        new List<Mob>(new[] { new Mob("SRU_Krug5.1.2", true), new Mob("SRU_KrugMini5.1.1") }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Order),
                    //Order: Gromp
                    new Camp(
                        100, 100, new Vector3(2164.34f, 8383.02f, 51.78f),
                        new List<Mob>(new[] { new Mob("SRU_Gromp13.1.1", true) }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Order),
                    //Chaos: Blue
                    new Camp(
                        100, 300, new Vector3(10984.11f, 6960.31f, 51.72f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Blue7.1.1", true), new Mob("SRU_BlueMini7.1.2"),
                                new Mob("SRU_BlueMini27.1.3")
                            }), true, Utility.Map.MapType.SummonersRift,
                        GameObjectTeam.Chaos),
                    //Chaos: Wolves
                    new Camp(
                        100, 100, new Vector3(10983.83f, 8328.73f, 62.22f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Murkwolf8.1.1", true), new Mob("SRU_MurkwolfMini8.1.2"),
                                new Mob("SRU_MurkwolfMini8.1.3")
                            }), false, Utility.Map.MapType.SummonersRift,
                        GameObjectTeam.Chaos),
                    //Chaos: Chicken
                    new Camp(
                        100, 100, new Vector3(7852.38f, 9562.62f, 52.30f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Razorbeak9.1.1", true), new Mob("SRU_RazorbeakMini9.1.2"),
                                new Mob("SRU_RazorbeakMini9.1.3"), new Mob("SRU_RazorbeakMini9.1.4")
                            }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Chaos),
                    //Chaos: Red
                    new Camp(
                        100, 300, new Vector3(7139.29f, 10779.34f, 56.38f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("SRU_Red10.1.1", true), new Mob("SRU_RedMini10.1.2"), new Mob("SRU_RedMini10.1.3")
                            }), true, Utility.Map.MapType.SummonersRift, GameObjectTeam.Chaos),
                    //Chaos: Krug
                    new Camp(
                        100, 100, new Vector3(6476.17f, 12142.51f, 56.48f),
                        new List<Mob>(new[] { new Mob("SRU_Krug11.1.2", true), new Mob("SRU_KrugMini11.1.1") }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Chaos),
                    //Chaos: Gromp
                    new Camp(
                        100, 100, new Vector3(12671.83f, 6306.60f, 51.71f),
                        new List<Mob>(new[] { new Mob("SRU_Gromp14.1.1", true) }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Chaos),
                    //Neutral: Dragon
                    new Camp(
                        150, 360, new Vector3(9813.83f, 4360.19f, -71.24f),
                        new List<Mob>(new[] { new Mob("SRU_Dragon6.1.1", true) }), true,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Neutral),
                    //Neutral: Rift Herald
                    new Camp(
                        240, 300, new Vector3(4993.14f, 10491.92f, -71.24f),
                        new List<Mob>(new[] { new Mob("SRU_RiftHerald", true) }), true,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Neutral),
                    //Neutral: Baron
                    new Camp(
                        1200, 420, new Vector3(4993.14f, 10491.92f, -71.24f),
                        new List<Mob>(new[] { new Mob("SRU_Baron12.1.1", true) }), true,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Neutral),
                    //Dragon: Crab
                    new Camp(
                        150, 180, new Vector3(10647.70f, 5144.68f, -62.81f),
                        new List<Mob>(new[] { new Mob("SRU_Crab15.1.1", true) }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Neutral),
                    //Baron: Crab
                    new Camp(
                        150, 180, new Vector3(4285.04f, 9597.52f, -67.60f),
                        new List<Mob>(new[] { new Mob("SRU_Crab16.1.1", true) }), false,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Neutral),
                    //Order: Wraiths
                    new Camp(
                        95, 75, new Vector3(4373.14f, 5842.84f, -107.14f),
                        new List<Mob>(
                            new[]
                            {
                                new Mob("TT_NWraith1.1.1", true), new Mob("TT_NWraith21.1.2"), new Mob("TT_NWraith21.1.3")
                            }), false, Utility.Map.MapType.TwistedTreeline, GameObjectTeam.Order),
                    //Order: Golems
                    new Camp(
                        95, 75, new Vector3(5106.94f, 7985.90f, -108.38f),
                        new List<Mob>(new[] { new Mob("TT_NGolem2.1.1", true), new Mob("TT_NGolem22.1.2") }), false,
                        Utility.Map.MapType.TwistedTreeline, GameObjectTeam.Order),
                    //Order: Wolves
                    new Camp(
                        95, 75, new Vector3(6078.15f, 6094.45f, -98.63f),
                        new List<Mob>(
                            new[]
                            { new Mob("TT_NWolf3.1.1", true), new Mob("TT_NWolf23.1.2"), new Mob("TT_NWolf23.1.3") }),
                        false, Utility.Map.MapType.TwistedTreeline, GameObjectTeam.Order),
                    //Chaos: Wraiths
                    new Camp(
                        95, 75, new Vector3(11025.95f, 5805.61f, -107.19f),
                        new List<Mob>(
                            new List<Mob>(
                                new[]
                                {
                                    new Mob("TT_NWraith4.1.1", true), new Mob("TT_NWraith24.1.2"),
                                    new Mob("TT_NWraith24.1.3")
                                })), false, Utility.Map.MapType.TwistedTreeline,
                        GameObjectTeam.Chaos),
                    //Chaos: Golems
                    new Camp(
                        95, 75, new Vector3(10276.81f, 8037.54f, -108.92f),
                        new List<Mob>(new[] { new Mob("TT_NGolem5.1.1", true), new Mob("TT_NGolem25.1.2") }), false,
                        Utility.Map.MapType.TwistedTreeline, GameObjectTeam.Chaos),
                    //Chaos: Wolves
                    new Camp(
                        95, 75, new Vector3(9294.02f, 6085.41f, -96.70f),
                        new List<Mob>(
                            new List<Mob>(
                                new[]
                                { new Mob("TT_NWolf6.1.1", true), new Mob("TT_NWolf26.1.2"), new Mob("TT_NWolf26.1.3") })),
                        false, Utility.Map.MapType.TwistedTreeline, GameObjectTeam.Chaos),
                    //Neutral: Vilemaw
                    new Camp(
                        600, 360, new Vector3(7738.30f, 10079.78f, -61.60f),
                        new List<Mob>(new List<Mob>(new[] { new Mob("TT_Spiderboss8.1.1", true) })), true,
                        Utility.Map.MapType.SummonersRift, GameObjectTeam.Neutral)
                };
            }
            catch (Exception ex)
            {
                Camps = new List<Camp>();
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public class Camp
        {
            public Camp(float spawnTime,
                float respawnTime,
                Vector3 position,
                List<Mob> mobs,
                bool isBig,
                Utility.Map.MapType mapType,
                GameObjectTeam team)
            {
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                Position = position;
                MinimapPosition = Drawing.WorldToMinimap(Position);
                Mobs = mobs;
                IsBig = isBig;
                MapType = mapType;
                Team = team;
            }

            public float SpawnTime { get; set; }
            public float RespawnTime { get; private set; }
            public Vector3 Position { get; private set; }
            public Vector2 MinimapPosition { get; private set; }
            public List<Mob> Mobs { get; private set; }
            public bool IsBig { get; set; }
            public Utility.Map.MapType MapType { get; set; }
            public GameObjectTeam Team { get; set; }
        }

        public class Mob
        {
            public Mob(string name, bool isBig = false)
            {
                Name = name;
                IsBig = isBig;
            }

            public string Name { get; private set; }
            public bool IsBig { get; set; }
        }
    }
}