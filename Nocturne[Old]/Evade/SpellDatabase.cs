// Copyright 2014 - 2014 Esk0r
// SpellDatabase.cs is part of Evade.
// 
// Evade is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Evade is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Evade. If not, see <http://www.gnu.org/licenses/>.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;

#endregion

namespace Nocturne.Evade
{
    public static class SpellDatabase
    {
        public static List<SpellData> Spells = new List<SpellData>();

        static SpellDatabase()
        {
            //Add spells to the database 

            #region Aatrox

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Aatrox",
                    SpellName = "AatroxQ",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 600,
                    Range = 650,
                    Radius = 250,
                    MissileSpeed = 2000,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "",
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Aatrox",
                    SpellName = "AatroxE",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1075,
                    Radius = 35,
                    MissileSpeed = 1250,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = false,
                    MissileSpellName = "AatroxEConeMissile",
                });

            #endregion Aatrox

            #region Ahri

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Ahri",
                    SpellName = "AhriSeduce",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1000,
                    Radius = 60,
                    MissileSpeed = 1550,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "AhriSeduceMissile",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall}
                });

            #endregion Ahri

            #region Amumu

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Amumu",
                    SpellName = "BandageToss",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 90,
                    MissileSpeed = 2000,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "SadMummyBandageToss",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall}
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Amumu",
                    SpellName = "CurseoftheSadMummy",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 0,
                    Radius = 550,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = false,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "",
                });

            #endregion Amumu

            #region Anivia

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Anivia",
                    SpellName = "FlashFrost",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 110,
                    MissileSpeed = 850,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "FlashFrostSpell",
                    CanBeRemoved = true,
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall}
                });

            #endregion Anivia

            #region Annie

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Annie",
                    SpellName = "InfernalGuardian",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 600,
                    Radius = 251,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "",
                });

            #endregion Annie

            #region Ashe

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Ashe",
                    SpellName = "EnchantedCrystalArrow",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 20000,
                    Radius = 130,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "EnchantedCrystalArrow",
                    CanBeRemoved = true,
                    CollisionObjects = new[] {CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall}
                });

            #endregion Ashe

            #region Bard

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Bard",
                    SpellName = "BardQ",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 950,
                    Radius = 60,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "BardQMissile",
                    CanBeRemoved = true,
                    CollisionObjects = new[] {CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall}
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Bard",
                    SpellName = "BardR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 500,
                    Range = 3400,
                    Radius = 350,
                    MissileSpeed = 2100,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = false,
                    MissileSpellName = "BardR",
                });

            #endregion

            #region Blatzcrank

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Blitzcrank",
                    SpellName = "RocketGrab",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1050,
                    Radius = 70,
                    MissileSpeed = 1800,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 4,
                    IsDangerous = true,
                    MissileSpellName = "RocketGrabMissile",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall}
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Blitzcrank",
                    SpellName = "StaticField",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 0,
                    Radius = 600,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = false,
                    DangerValue = 5,
                    IsDangerous = false,
                    MissileSpellName = "",
                });

            #endregion Blatzcrink

            #region Brand

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Brand",
                    SpellName = "BrandBlaze",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 60,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "BrandBlazeMissile",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall}
                });

            #endregion Brand

            #region Braum

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Braum",
                    SpellName = "BraumRWrapper",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 500,
                    Range = 1200,
                    Radius = 115,
                    MissileSpeed = 1400,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 4,
                    IsDangerous = true,
                    MissileSpellName = "braumrmissile",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall}
                });

            #endregion Braum

            #region Cassiopeia

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Cassiopeia",
                    SpellName = "CassiopeiaPetrifyingGaze",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCone,
                    Delay = 600,
                    Range = 825,
                    Radius = 80,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = false,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "CassiopeiaPetrifyingGaze",
                });

            #endregion Cassiopeia

            #region Chogath

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Chogath",
                    SpellName = "Rupture",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 1200,
                    Range = 950,
                    Radius = 250,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = false,
                    MissileSpellName = "Rupture",
                });

            #endregion Chogath

            #region Corki

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Corki",
                    SpellName = "MissileBarrage2",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 200,
                    Range = 1500,
                    Radius = 40,
                    MissileSpeed = 2000,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "MissileBarrageMissile2",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall}
                });

            #endregion Corki

            #region Darius

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Darius",
                    SpellName = "DariusAxeGrabCone",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotCone,
                    Delay = 250,
                    Range = 550,
                    Radius = 80,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = false,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "DariusAxeGrabCone",
                });

            #endregion Darius

            #region Diana

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Diana",
                    SpellName = "DianaArc",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 895,
                    Radius = 195,
                    MissileSpeed = 1400,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "DianaArcArc",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall}
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Diana",
                    SpellName = "DianaArcArc",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotArc,
                    Delay = 250,
                    Range = 895,
                    Radius = 195,
                    DontCross = true,
                    MissileSpeed = 1400,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "DianaArcArc",
                    TakeClosestPath = true,
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall}
                });

            #endregion Diana

            #region Draven

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Draven",
                    SpellName = "DravenRCast",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 400,
                    Range = 20000,
                    Radius = 160,
                    MissileSpeed = 2000,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "DravenR",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall}
                });

            #endregion Draven

            #region Elise

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Elise",
                    SpellName = "EliseHumanE",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 55,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 4,
                    IsDangerous = true,
                    MissileSpellName = "EliseHumanE",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall}
                });

            #endregion Elise

            #region Evelynn

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Evelynn",
                    SpellName = "EvelynnR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 650,
                    Radius = 350,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "EvelynnR",
                });

            #endregion Evelynn

            #region Ezreal

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Ezreal",
                    SpellName = "EzrealTrueshotBarrage",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 1000,
                    Range = 20000,
                    Radius = 160,
                    MissileSpeed = 2000,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "EzrealTrueshotBarrage",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall},
                    Id = 245,
                });

            #endregion Ezreal

            #region Fizz

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Fizz",
                    SpellName = "FizzMarinerDoom",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1300,
                    Radius = 120,
                    MissileSpeed = 1350,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "FizzMarinerDoomMissile",
                    CollisionObjects = new[] {CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                    CanBeRemoved = true,
                });

            #endregion Fizz

            #region Galio

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Galio",
                    SpellName = "GalioIdolOfDurand",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 0,
                    Radius = 550,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = false,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "",
                });

            #endregion Galio

            #region Gnar

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Gnar",
                    SpellName = "GnarR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 0,
                    Radius = 500,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = false,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "",
                });

            #endregion

            #region Gragas

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Gragas",
                    SpellName = "GragasR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 1050,
                    Radius = 375,
                    MissileSpeed = 1800,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "GragasRBoom",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall},
                });

            #endregion Gragas

            #region Graves

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Graves",
                    SpellName = "GravesChargeShot",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 100,
                    MissileSpeed = 2100,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "GravesChargeShotShot",
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Graves

            #region JarvanIV

            Spells.Add(
                new SpellData
                {
                    ChampionName = "JarvanIV",
                    SpellName = "JarvanIVDemacianStandard",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 500,
                    Range = 860,
                    Radius = 175,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "JarvanIVDemacianStandard",
                });

            #endregion JarvanIV

            #region Jayce

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Jayce",
                    SpellName = "JayceQAccel",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1300,
                    Radius = 70,
                    MissileSpeed = 2350,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "JayceShockBlastWallMis",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Jayce

            #region Jinx

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Jinx",
                    SpellName = "JinxR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 600,
                    Range = 20000,
                    Radius = 140,
                    MissileSpeed = 1700,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "JinxR",
                    CanBeRemoved = true,
                    CollisionObjects = new[] {CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                });

            #endregion Jinx

            #region Karma

            //TODO: add the circle at the end.
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Karma",
                    SpellName = "KarmaQMantra",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 950,
                    Radius = 80,
                    MissileSpeed = 1700,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "KarmaQMissileMantra",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Karma

            #region Kassadin

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Kassadin",
                    SpellName = "RiftWalk",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 250,
                    Range = 450,
                    Radius = 270,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "RiftWalk",
                });

            #endregion Kassadin

            #region Nocturne

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nocturne",
                    SpellName = "NocturneSlide",
                    Slot = SpellSlot.W,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 0,
                    Range = 600,
                    Radius = 220,
                    MissileSpeed = 1450,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "NocturneSlide",
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nocturne",
                    SpellName = "NocturneSlideM",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 0,
                    Range = 600,
                    Radius = 220,
                    MissileSpeed = 1450,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "NocturneSlideM",
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nocturne",
                    SpellName = "NocturneSoulShackle",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 950,
                    Radius = 70,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "NocturneSoulShackle",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nocturne",
                    SpellName = "NocturneSoulShackleM",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 950,
                    Radius = 70,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "NocturneSoulShackleM",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Nocturne

            #region Leona

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Leona",
                    SpellName = "LeonaSolarFlare",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 1000,
                    Range = 1200,
                    Radius = 300,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "LeonaSolarFlare",
                });

            #endregion Leona

            #region Lucian

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Lucian",
                    SpellName = "LucianRMis",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 500,
                    Range = 1400,
                    Radius = 110,
                    MissileSpeed = 2800,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "lucianrmissileoffhand",
                    ExtraMissileNames = new[] {"lucianrmissile"},
                    DontCheckForDuplicates = true,
                    DisabledByDefault = true,
                });

            #endregion Lucian

            #region Lux

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Lux",
                    SpellName = "LuxLightBinding",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1300,
                    Radius = 70,
                    MissileSpeed = 1200,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "LuxLightBindingMis",
                    //CanBeRemoved = true,
                    //CollisionObjects = new[] { CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall, },
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Lux",
                    SpellName = "LuxMaliceCannon",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotLine,
                    Delay = 1000,
                    Range = 3500,
                    Radius = 190,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "LuxMaliceCannon",
                });

            #endregion Lux

            #region Malphite

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Malphite",
                    SpellName = "UFSlash",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 0,
                    Range = 1000,
                    Radius = 270,
                    MissileSpeed = 1500,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "UFSlash",
                });

            #endregion Malphite

            #region Morgana

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Morgana",
                    SpellName = "DarkBindingMissile",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1300,
                    Radius = 80,
                    MissileSpeed = 1200,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "DarkBindingMissile",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Morgana

            #region Nami

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nami",
                    SpellName = "NamiR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 500,
                    Range = 2750,
                    Radius = 260,
                    MissileSpeed = 850,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "NamiRMissile",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall},
                });

            #endregion Nami


            #region Nidalee

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nidalee",
                    SpellName = "JavelinToss",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1500,
                    Radius = 40,
                    MissileSpeed = 1300,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "JavelinToss",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Nidalee

            #region Orianna

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Orianna",
                    SpellName = "OrianaDetonateCommand-",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 700,
                    Range = 0,
                    Radius = 410,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "OrianaDetonateCommand-",
                    FromObject = "yomu_ring_",
                    SourceObjectName = "r_vacuumindicator", //Orianna_Base_R_VacuumIndicator.troy
                });

            #endregion Orianna


            #region Rengar

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Rengar",
                    SpellName = "RengarE",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1000,
                    Radius = 70,
                    MissileSpeed = 1500,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "RengarEFinal",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Champions, CollisionObjectTypes.Minion, CollisionObjectTypes.YasuoWall},
                });

            #endregion Rengar

            #region Riven

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Riven",
                    SpellName = "rivenizunablade",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 125,
                    MissileSpeed = 1600,
                    FixedRange = false,
                    AddHitbox = false,
                    DangerValue = 5,
                    IsDangerous = true,
                    MultipleNumber = 3,
                    MultipleAngle = 15*(float) Math.PI/180,
                    MissileSpellName = "RivenLightsaberMissile",
                    ExtraMissileNames = new[] {"RivenLightsaberMissileSide"}
                });

            #endregion Riven

            #region Rumble

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Rumble",
                    SpellName = "RumbleCarpetBombM",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 400,
                    MissileDelayed = true,
                    Range = 1200,
                    Radius = 200,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 4,
                    IsDangerous = false,
                    MissileSpellName = "RumbleCarpetBombMissile",
                    CanBeRemoved = false,
                    CollisionObjects = new CollisionObjectTypes[] {},
                });

            #endregion Rumble

            #region Sejuani

            //TODO: fix?
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Sejuani",
                    SpellName = "SejuaniGlacialPrisonStart",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1100,
                    Radius = 110,
                    MissileSpeed = 1600,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "sejuaniglacialprison",
                    CanBeRemoved = true,
                    CollisionObjects = new[] {CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                });

            #endregion Sejuani

            #region Sion

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Sion",
                    SpellName = "SionR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 500,
                    Range = 800,
                    Radius = 120,
                    MissileSpeed = 1000,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    CollisionObjects =
                        new[] {CollisionObjectTypes.Champions},
                });

            #endregion Sion

            #region Shen

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Shen",
                    SpellName = "ShenShadowDash",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 0,
                    Range = 650,
                    Radius = 50,
                    MissileSpeed = 1600,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "ShenShadowDash",
                    ExtraRange = 200,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Minion, CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                });

            #endregion Shen

            #region Shyvana

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Shyvana",
                    SpellName = "ShyvanaTransformCast",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1000,
                    Radius = 150,
                    MissileSpeed = 1500,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "ShyvanaTransformCast",
                    ExtraRange = 200,
                });

            #endregion Shyvana

            #region Sona

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Sona",
                    SpellName = "SonaR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1000,
                    Radius = 140,
                    MissileSpeed = 2400,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 5,
                    IsDangerous = true,
                    MissileSpellName = "SonaR",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall},
                });

            #endregion Sona

            #region Swain

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Swain",
                    SpellName = "SwainShadowGrasp",
                    Slot = SpellSlot.W,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 1100,
                    Range = 900,
                    Radius = 180,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "SwainShadowGrasp",
                });

            #endregion Swain

            #region Syndra

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Syndra",
                    SpellName = "syndrae5",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 300,
                    Range = 950,
                    Radius = 90,
                    MissileSpeed = 1601,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "syndrae5",
                    DisableFowDetection = true,
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Syndra",
                    SpellName = "SyndraE",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 300,
                    Range = 950,
                    Radius = 90,
                    MissileSpeed = 1601,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    DisableFowDetection = true,
                    MissileSpellName = "SyndraE",
                });

            #endregion Syndra

            #region Thresh

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Thresh",
                    SpellName = "ThreshQ",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 500,
                    Range = 1100,
                    Radius = 70,
                    MissileSpeed = 1900,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "ThreshQMissile",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Minion, CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                });

            #endregion Thresh


            #region Varus

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Varus",
                    SpellName = "VarusR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1200,
                    Radius = 120,
                    MissileSpeed = 1950,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "VarusRMissile",
                    CanBeRemoved = true,
                    CollisionObjects = new[] {CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                });

            #endregion Varus

            #region Vi

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Vi",
                    SpellName = "Vi-q",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 250,
                    Range = 1000,
                    Radius = 90,
                    MissileSpeed = 1500,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "ViQMissile",
                });

            #endregion Vi

            #region Xerath

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Xerath",
                    SpellName = "XerathMageSpear",
                    Slot = SpellSlot.E,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 200,
                    Range = 1150,
                    Radius = 60,
                    MissileSpeed = 1400,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = true,
                    MissileSpellName = "XerathMageSpearMissile",
                    CanBeRemoved = true,
                    CollisionObjects =
                        new[]
                        {CollisionObjectTypes.Minion, CollisionObjectTypes.Champions, CollisionObjectTypes.YasuoWall},
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Xerath",
                    SpellName = "xerathrmissilewrapper",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 700,
                    Range = 5600,
                    Radius = 120,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "xerathrmissilewrapper",
                });

            #endregion Xerath

            #region Yasuo 

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Yasuo",
                    SpellName = "yasuoq2",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotLine,
                    Delay = 400,
                    Range = 550,
                    Radius = 20,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = true,
                    MissileSpellName = "yasuoq2",
                    Invert = true,
                });

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Yasuo",
                    SpellName = "yasuoq3w",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotMissileLine,
                    Delay = 500,
                    Range = 1150,
                    Radius = 90,
                    MissileSpeed = 1500,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 3,
                    IsDangerous = true,
                    MissileSpellName = "yasuoq3w",
                    CollisionObjects = new[] {CollisionObjectTypes.YasuoWall}
                });

            #endregion Yasuo

            #region Zac

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Zac",
                    SpellName = "ZacQ",
                    Slot = SpellSlot.Q,
                    Type = SkillShotType.SkillshotLine,
                    Delay = 500,
                    Range = 550,
                    Radius = 120,
                    MissileSpeed = int.MaxValue,
                    FixedRange = true,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "ZacQ",
                });

            #endregion Zac

            #region Ziggs

            Spells.Add(
                new SpellData
                {
                    ChampionName = "Ziggs",
                    SpellName = "ZiggsR",
                    Slot = SpellSlot.R,
                    Type = SkillShotType.SkillshotCircle,
                    Delay = 0,
                    Range = 5300,
                    Radius = 500,
                    MissileSpeed = int.MaxValue,
                    FixedRange = false,
                    AddHitbox = true,
                    DangerValue = 2,
                    IsDangerous = false,
                    MissileSpellName = "ZiggsR",
                    DisableFowDetection = true,
                });

            #endregion Ziggs

            //Console.WriteLine("Added " + Spells.Count + " spells.");
        }

        public static SpellData GetBySourceObjectName(string objectName)
        {
            objectName = objectName.ToLowerInvariant();
            return
                Spells.Where(spellData => spellData.SourceObjectName.Length != 0)
                    .FirstOrDefault(spellData => objectName.Contains(spellData.SourceObjectName));
        }

        public static SpellData GetByName(string spellName)
        {
            spellName = spellName.ToLower();
            return
                Spells.FirstOrDefault(
                    spellData =>
                        spellData.SpellName.ToLower() == spellName || spellData.ExtraSpellNames.Contains(spellName));
        }

        public static SpellData GetByMissileName(string missileSpellName)
        {
            missileSpellName = missileSpellName.ToLower();
            return
                Spells.FirstOrDefault(
                    spellData =>
                        spellData.MissileSpellName != null && spellData.MissileSpellName.ToLower() == missileSpellName ||
                        spellData.ExtraMissileNames.Contains(missileSpellName));
        }

        public static SpellData GetBySpeed(string championName, int speed, int id = -1)
        {
            return
                Spells.FirstOrDefault(
                    spellData =>
                        spellData.ChampionName == championName && spellData.MissileSpeed == speed &&
                        (spellData.Id == -1 || id == spellData.Id));
        }
    }
}