#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using static LeagueSharp.Common.Packet;
using Collision = LeagueSharp.Common.Collision;

#endregion

namespace Marksman.Utils
{
    using System.Security.Cryptography;

    using Marksman.Champions;

    internal class Utils
    {
        public static string Tab => "    ";

        public static Font Text, TextBig, SmallText, TextWarning;

        static Utils()
        {
            /*
            var device = new Device(new Direct3D(), 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters(800, 800) { PresentationInterval = PresentInterval.One });
            FontDescription fontDescription = new FontDescription()
            {
                Height = 72,
                Italic = false,
                CharacterSet = FontCharacterSet.Ansi,
                FaceName = "Arial",
                MipLevels = 0,
                OutputPrecision = FontPrecision.TrueType,
                PitchAndFamily = FontPitchAndFamily.Default,
                Quality = FontQuality.ClearType,
                Weight = FontWeight.Bold
            };

            FontX = new Font(device, fontDescription);
            */
            TextBig = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 25,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default
                });

            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Draft
                });

            SmallText = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 13,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default
                });

            TextWarning = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Malgun Gothic",
                    Height = 60,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default
                });

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            Text.Dispose();
            TextBig.Dispose();
            SmallText.Dispose();
            TextWarning.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            Text.OnResetDevice();
            TextBig.OnResetDevice();
            SmallText.OnResetDevice();
            TextWarning.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            Text.OnLostDevice();
            TextBig.OnLostDevice();
            SmallText.OnLostDevice();
            TextWarning.OnLostDevice();
        }

        public class MPing
        {
            private static Vector2 PingLocation;

            private static int LastPingT = 0;

            public static void Ping(Vector2 position, int pingCount = 4,
                PingCategory pingCategory = PingCategory.Fallback)
            {
                if (LeagueSharp.Common.Utils.TickCount - LastPingT < 30*1000)
                {
                    return;
                }

                LastPingT = LeagueSharp.Common.Utils.TickCount;
                PingLocation = position;
                SimplePing();

                for (int i = 1; i <= pingCount; i++)
                {
                    Utility.DelayAction.Add(i*400, (() =>
                    {
                        Game.ShowPing(pingCategory, PingLocation, true);
                    }));
                }
                /*                
                Utility.DelayAction.Add(150, SimplePing);
                Utility.DelayAction.Add(300, SimplePing);
                Utility.DelayAction.Add(400, SimplePing);
                Utility.DelayAction.Add(800, SimplePing);
                */
            }

            private static void SimplePing(PingCategory pingCategory = PingCategory.Fallback)
            {
                S2C.Ping.Encoded(new S2C.Ping.Struct(PingLocation.X, PingLocation.Y, 0, 0, PingType.Fallback)).Process();
                Game.ShowPing(pingCategory, PingLocation, true);

            }
        }

        public enum MobTypes
        {
            All,
            BigBoys
        }

        public static bool In<T>(T source, params T[] list)
        {
            return list.Equals(source);
        }

        public static bool IsFollowing(Obj_AI_Base t)
        {
            if (!t.IsFacing(ObjectManager.Player)
                && ObjectManager.Player.Position.Distance(t.Path[0])
                < ObjectManager.Player.Position.Distance(t.Position))
            {
                return true;
            }
            return false;
        }

        public static bool IsRunning(Obj_AI_Base t)
        {
            if (!t.IsFacing(ObjectManager.Player)
                && (t.Path.Count() >= 1
                    && ObjectManager.Player.Position.Distance(t.Path[0])
                    > ObjectManager.Player.Position.Distance(t.Position)))
            {
                return true;
            }
            return false;
        }

        private static readonly string[] BetterWithEvade =
        {
            "Corki", "Ezreal", "Graves", "Lucian", "Sivir", "Tristana",
            "Caitlyn", "Vayne"
        };

        public static Obj_AI_Base GetMobs(float spellRange, MobTypes mobTypes = MobTypes.All, int minMobCount = 1)
        {
            List<Obj_AI_Base> mobs = MinionManager.GetMinions(
                spellRange + 200,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs == null) return null;

            if (mobTypes == MobTypes.BigBoys)
            {
                Obj_AI_Base oMob = (from fMobs in mobs
                    from fBigBoys in
                        new[]
                        {
                            "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red",
                            "SRU_Krug", "SRU_Dragon", "SRU_Baron", "Sru_Crab"
                        }
                    where fBigBoys == fMobs.SkinName
                    select fMobs).FirstOrDefault();

                if (oMob != null)
                {
                    if (oMob.IsValidTarget(spellRange))
                    {
                        return oMob;
                    }
                }
            }
            else if (mobs.Count >= minMobCount)
            {
                return mobs[0];
            }

            return null;
        }

        public static void PrintMessage(string message)
        {
            Game.PrintChat(
                "<font color='#ff3232'>Marksman: </font><font color='#d4d4d4'><font color='#FFFFFF'>" + message
                + "</font>");
            //Notifications.AddNotification("Marksman: " + message, 4000);
        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor,
            bool shadow = false)
        {
            if (shadow)
            {
                vFont.DrawText(null, vText, (int) vPosX + 2, (int) vPosY + 2, SharpDX.Color.Black);
            }
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }


        public static void DrawLine(Vector3 from, Vector3 to, System.Drawing.Color color, String text = "")
        {
            var a = new Geometry.Polygon.Line(from, to);
            a.Draw(color, 3);

            Vector3[] x = new[] {from, to};
            var aX = Drawing.WorldToScreen(new Vector3(CenterOfVectors(x).X, CenterOfVectors(x).Y, CenterOfVectors(x).Z));
            Drawing.DrawText(aX.X - 15, aX.Y - 15, System.Drawing.Color.White, text);
        }

        public static int GetEnemyPriority(string championName)
        {
            string[] lowPriority =
            {
                "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen",
                "Gnar", "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami",
                "Nasus", "Nautilus", "Nunu", "Olaf", "Rammus", "Renekton", "Sejuani", "Shen",
                "Shyvana", "Singed", "Sion", "Skarner", "Sona", "Soraka", "Tahm", "Taric", "Thresh",
                "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
            };

            string[] mediumPriority =
            {
                "Aatrox", "Akali", "Darius", "Diana", "Ekko", "Elise", "Evelynn", "Fiddlesticks",
                "Fiora", "Fizz", "Galio", "Gangplank", "Gragas", "Heimerdinger", "Vi", "Jax",
                "Jayce", "Kassadin", "Kayle", "Kha'Zix", "Lee Sin", "Lissandra", "Maokai",
                "Mordekaiser", "Morgana", "Nocturne", "Nidalee", "Pantheon", "Poppy", "RekSai",
                "Rengar", "Riven", "Rumble", "Ryze", "Shaco", "Swain", "Trundle", "Tryndamere",
                "Udyr", "Urgot", "Vladimir", "Vi", "XinZhao", "Yasuo", "Zilean"
            };

            string[] highPriority =
            {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia",
                "Corki", "Draven", "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus",
                "Katarina", "Kennen", "Kindred", "KogMaw", "Leblanc", "Lucian", "Lux", "Malzahar",
                "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon", "Teemo",
                "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz",
                "Viktor", "Xerath", "Zed", "Ziggs"
            };

            if (lowPriority.Contains(championName))
            {
                return 0;
            }
            if (mediumPriority.Contains(championName))
            {
                return 1;
            }
            if (highPriority.Contains(championName))
            {
                return 2;
            }
            return 1;
        }

        public static Vector3 CenterOfVectors(Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            if (vectors == null || vectors.Length == 0)
                return sum;

            sum = vectors.Aggregate(sum, (current, vec) => current + vec);
            return sum/vectors.Length;
        }
    }

    public static class KillableTarget
    {
        public static bool IsAttackableTarget(this Obj_AI_Hero target)
        {
            return !target.HasUndyingBuff() && !target.HasSpellShield() && !target.IsInvulnerable;
        }

        public static bool CanUseSpell(this Obj_AI_Hero unit)
        {
            return unit != null && (unit.HasBuff("kindredrnodeathbuff") && unit.HealthPercent <= 10);

        }

        public static bool IsKillableTarget(this Obj_AI_Hero target, SpellSlot spell)
        {
            var totalHealth = target.TotalShieldHealth();
            if (target.HasUndyingBuff() || target.HasSpellShield() || target.IsInvulnerable)
            {
                return false;
            }

            if (target.ChampionName == "Blitzcrank" && !target.HasBuff("BlitzcrankManaBarrierCD")
                && !target.HasBuff("ManaBarrier"))
            {
                totalHealth += target.Mana/2;
            }
            return (ObjectManager.Player.GetSpellDamage(target, spell) >= totalHealth);
        }

        public static float TotalShieldHealth(this Obj_AI_Base target)
        {
            return target.Health + target.AllShield + target.PhysicalShield + target.MagicalShield;
        }

        public static bool HasSpellShield(this Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuffOfType(BuffType.SpellImmunity);
        }

        public static bool HasUndyingBuff(this Obj_AI_Hero target)
        {
            if (
                target.Buffs.Any(
                    b =>
                        b.IsValid
                        && (b.Name == "ChronoShift" /* Zilean R */
                            || b.Name == "FioraW" /* Fiora Riposte */
                            || b.Name == "BardRStasis" /* Bard ult */
                            || b.Name == "JudicatorIntervention" /* Kayle R */
                            || b.Name == "UndyingRage" /* Tryndamere R */)))
            {
                return true;
            }

            if (target.ChampionName == "Poppy")
            {
                if (
                    HeroManager.Allies.Any(
                        o =>
                            !o.IsMe
                            && o.Buffs.Any(
                                b =>
                                    b.Caster.NetworkId == target.NetworkId && b.IsValid &&
                                    b.DisplayName == "PoppyDITarget")))
                {
                    return true;
                }
            }

            return target.IsInvulnerable;
        }
    }

    public enum FarmMode
    {
        LaneClear,
        JungleClear
    }

    public enum MinionType
    {
        All,
        BigMobs
    }

    public enum MinionGroup
    {
        Alone,
        Lane,
        Circular
    }

    public enum GenericType
    {
        MinionGroup,
        Position
    }

    public interface IValue<T>
    {
        T GetValue(Spell spell, FarmMode farmMode, GameObjectTeam minionTeam, MinionType minionType = MinionType.All);
    }

    public class SomeClass : IValue<Vector2>, IValue<IEnumerable<Obj_AI_Base>>
    {
        IEnumerable<Obj_AI_Base> IValue<IEnumerable<Obj_AI_Base>>.GetValue(Spell spell, FarmMode farmMode,
            GameObjectTeam minionTeam, MinionType minionType)
        {
            IEnumerable<Obj_AI_Base> list = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(spell.Range));
            IEnumerable<Obj_AI_Base> mobs;

            if (farmMode == FarmMode.JungleClear)
            {
                mobs = list.Where(w => w.Team == minionTeam);
                if (minionType == MinionType.BigMobs)
                {
                    IEnumerable<Obj_AI_Base> oMob = (from fMobs in mobs
                        from fBigBoys in
                            new[]
                            {
                                "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red",
                                "SRU_Krug", "SRU_Dragon", "SRU_Baron", "Sru_Crab"
                            }
                        where fBigBoys == fMobs.SkinName
                        select fMobs).AsEnumerable();

                    mobs = oMob;
                }
            }
            else
            {
                mobs = list;
            }
            return mobs;
        }

        Vector2 IValue<Vector2>.GetValue(Spell spell, FarmMode farmMode, GameObjectTeam minionTeam,
            MinionType minionType)
        {
            return new Vector2(0, 0);
        }
    }


    public static class MarksmanMinionManager
    {
        public static Vector2 GetMobPosition => new Vector2(0, 0);

        public static int GetMinionCountsInRange(this Spell spell)
            => MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range).Count;

        public static bool GetMinionTotalAaCont(this Spell spell, int minionCount = 1)
        {
            var totalAa =
                ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(spell.Range)).Sum(mob => (int) mob.Health);

            totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
            return totalAa >= minionCount;
        }

        public static bool GetMinionTotalAaCont(float range, int minionCount = 1)
        {
            var totalAa =
                ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(range)).Sum(mob => (int) mob.Health);

            totalAa = (int) (totalAa/ObjectManager.Player.TotalAttackDamage);
            return totalAa >= minionCount;
        }

        public static Vector2 GetCircularFarmMinions(this Spell spell, int minionCount = 1)
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range);
            MinionManager.FarmLocation location = spell.GetCircularFarmLocation(minions, spell.Width*0.75f);
            if (location.MinionsHit >= minionCount && spell.IsInRange(location.Position.To3D()))
            {
                return location.Position;
            }

            return new Vector2(0, 0);
        }

        private static List<Obj_AI_Base> GetCollisionMinions(this Spell spell, Obj_AI_Hero source,
            Vector3 targetposition)
        {
            var input = new PredictionInput
            {
                Unit = source,
                Radius = spell.Width,
                Delay = spell.Delay,
                Speed = spell.Speed,
            };

            input.CollisionObjects[0] = CollisionableObjects.Minions;

            return
                Collision.GetCollision(new List<Vector3> {targetposition}, input)
                    .OrderBy(obj => obj.Distance(source, false))
                    .ToList();
        }

        public static Obj_AI_Base GetLineCollisionMinions(this Spell spell, int minionCount = 1)
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spell.Range);
            foreach (var minion in minions.Where(x => x.Health <= spell.GetDamage(x)))
            {
                int killableMinionCount = 0;
                foreach (
                    Obj_AI_Base colminion in
                        spell.GetCollisionMinions(ObjectManager.Player,
                            ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, spell.Range)))
                {
                    if (colminion.Health <= spell.GetDamage(colminion))
                    {
                        killableMinionCount++;
                    }
                    else break;
                }

                if (killableMinionCount >= minionCount)
                {
                    return minion;
                }
            }
            return null;
        }

        public static Vector2 GetLineFarmMinions(this Spell spell, int minionCount)
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range);
            MinionManager.FarmLocation location = spell.GetLineFarmLocation(minions);
            if (location.MinionsHit >= minionCount && spell.IsInRange(location.Position.To3D()))
            {
                return location.Position;
            }

            return new Vector2(0, 0);
        }


        public static IEnumerable<Obj_AI_Base> GetMobGroup(this Spell spell,
            FarmMode farmMode,
            GameObjectTeam minionTeam,
            MinionType minionType = MinionType.All,
            MinionGroup minionGroup = MinionGroup.Alone,
            int minionCount = 1)
        {
            IEnumerable<Obj_AI_Base> list =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(m => m.IsValidTarget(spell.Range) && m.Team == GameObjectTeam.Neutral);

            if (minionType == MinionType.BigMobs)
            {

                IEnumerable<Obj_AI_Base> oMob = (from fMobs in list
                    from fBigBoys in
                        new[]
                        {
                            "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red",
                            "SRU_Krug", "SRU_Dragon", "SRU_Baron", "Sru_Crab"
                        }
                    where fBigBoys == fMobs.SkinName
                    select fMobs).AsEnumerable();
                list = oMob;
            }
            return list;
        }


    }

    public static class CGlobal
    {
        public enum FarmMode
        {
            LaneClear,
            JungleClear
        }

        public enum MinionType
        {
            All,
            BigMobs
        }

        public enum MinionGroup
        {
            Alone,
            Lane,
            Circular
        }

        public enum GenericType
        {
            MinionGroup,
            Position
        }

        public static IEnumerable<Obj_AI_Base> GetMins(this Spell spell, FarmMode farmMode, GameObjectTeam minionTeam,
            MinionType minionType = MinionType.All, MinionGroup minionGroup = MinionGroup.Alone, int minionCount = 1)
        {
            IEnumerable<Obj_AI_Base> list = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(spell.Range));
            IEnumerable<Obj_AI_Base> mobs;

            if (farmMode == FarmMode.JungleClear)
            {
                mobs = list.Where(w => w.Team == minionTeam);
                if (minionType == MinionType.BigMobs)
                {

                    IEnumerable<Obj_AI_Base> oMob = (from fMobs in mobs
                        from fBigBoys in
                            new[]
                            {
                                "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red",
                                "SRU_Krug", "SRU_Dragon", "SRU_Baron", "Sru_Crab"
                            }
                        where fBigBoys == fMobs.SkinName
                        select fMobs).AsEnumerable();

                    mobs = oMob;
                }
            }
            else
            {
                mobs = list;
            }

            var objAiBases = mobs as IList<Obj_AI_Base> ?? mobs.ToList();
            List<Obj_AI_Base> m1 = objAiBases.ToList();

            var locLine = spell.GetLineFarmLocation(m1);
            if (locLine.MinionsHit >= 3 && spell.IsInRange(locLine.Position.To3D()))
            {
                spell.Cast(locLine.Position);

            }

            var locCircular = spell.GetCircularFarmLocation(m1, spell.Width);
            if (locCircular.MinionsHit >= minionCount && spell.IsInRange(locCircular.Position.To3D()))
            {
                spell.Cast(locCircular.Position);
            }

            return null;
        }

        public static SharpDX.Color MenuColor(this Spell spell)
        {
            switch (spell.Slot)
            {
                case SpellSlot.Q:
                    return SharpDX.Color.Aqua;
                case SpellSlot.W:
                    return SharpDX.Color.Bisque;
                case SpellSlot.E:
                    return SharpDX.Color.OrangeRed;
                case SpellSlot.R:
                    return SharpDX.Color.Yellow;
            }
            return SharpDX.Color.Wheat;
        }

        public static float CommonComboDamage(this Obj_AI_Hero t)
        {
            var fComboDamage = 0d;

            if (ObjectManager.Player.GetSpellSlot("summonerdot") != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(ObjectManager.Player.GetSpellSlot("summonerdot"))
                == SpellState.Ready && ObjectManager.Player.Distance(t) < 550)
            {
                fComboDamage += (float) ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);
            }

            if (Items.CanUseItem(3144) && ObjectManager.Player.Distance(t) < 550)
            {
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);
            }

            if (Items.CanUseItem(3153) && ObjectManager.Player.Distance(t) < 550)
            {
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);
            }
            return (float) fComboDamage;
        }

        public static bool IsUnderAllyTurret(this Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Where<Obj_AI_Turret>(turret =>
            {
                if (turret == null || !turret.IsValid || turret.Health <= 0f)
                {
                    return false;
                }
                if (!turret.IsEnemy)
                {
                    return true;
                }
                return false;
            })
                .Any<Obj_AI_Turret>(
                    turret =>
                        Vector2.Distance(unit.Position.To2D(), turret.Position.To2D()) < 925f && turret.IsAlly);
        }

        public static bool HasKindredUltiBuff(this Obj_AI_Hero unit)
        {
            return unit != null && (unit.HasBuff("kindredrnodeathbuff") && unit.HealthPercent <= 10);
        }

        public static bool IsPositionSafe(this Spell spell, Vector2 position)
            // use underTurret and .Extend for this please
        {
            var myPos = ObjectManager.Player.Position.To2D();
            var newPos = (position - myPos);
            newPos.Normalize();

            var checkPos = position + newPos*(spell.Range - Vector2.Distance(position, myPos));
            var enemy = HeroManager.Enemies.Find(e => e.Distance(checkPos) < 350);
            return enemy == null;
        }
    }

    public static class Jungle
    {
        public enum GameObjectTeam
        {
            Unknown = 0,
            Order = 100,
            Chaos = 200,
            Neutral = 300,
        }

        public enum DrawOption
        {
            Off = 0,

            CloseToMobs = 1,

            CloseToMobsAndJungleClearActive = 2
        }

        //private static Dictionary<Vector3, System.Drawing.Color> junglePositions;

        private static Dictionary<Vector2, GameObjectTeam> mobTeams;

        //public static void DrawJunglePosition(int drawOption)
        //{
        //    if (drawOption == (int)DrawOption.Off)
        //    {
        //        return;
        //    }

        //    junglePositions = new Dictionary<Vector3, System.Drawing.Color>();
        //    if (Game.MapId == (GameMapId)11)
        //    {
        //        const float CircleRange = 115f;

        //        junglePositions.Add(new Vector3(7461.018f, 3253.575f, 52.57141f), System.Drawing.Color.Blue);
        //        // blue team :red;
        //        junglePositions.Add(new Vector3(3511.601f, 8745.617f, 52.57141f), System.Drawing.Color.Blue);
        //        // blue team :blue
        //        junglePositions.Add(new Vector3(7462.053f, 2489.813f, 52.57141f), System.Drawing.Color.Blue);
        //        // blue team :golems
        //        junglePositions.Add(new Vector3(3144.897f, 7106.449f, 51.89026f), System.Drawing.Color.Blue);
        //        // blue team :wolfs
        //        junglePositions.Add(new Vector3(7770.341f, 5061.238f, 49.26587f), System.Drawing.Color.Blue);
        //        // blue team :wariaths

        //        junglePositions.Add(new Vector3(10930.93f, 5405.83f, -68.72192f), System.Drawing.Color.Yellow);
        //        // Dragon

        //        junglePositions.Add(new Vector3(7326.056f, 11643.01f, 50.21985f), System.Drawing.Color.Red);
        //        // red team :red
        //        junglePositions.Add(new Vector3(11417.6f, 6216.028f, 51.00244f), System.Drawing.Color.Red);
        //        // red team :blue
        //        junglePositions.Add(new Vector3(7368.408f, 12488.37f, 56.47668f), System.Drawing.Color.Red);
        //        // red team :golems
        //        junglePositions.Add(new Vector3(10342.77f, 8896.083f, 51.72742f), System.Drawing.Color.Red);
        //        // red team :wolfs
        //        junglePositions.Add(new Vector3(7001.741f, 9915.717f, 54.02466f), System.Drawing.Color.Red);
        //        // red team :wariaths                    

        //        foreach (var hp in junglePositions)
        //        {
        //            switch (drawOption)
        //            {
        //                case (int)DrawOption.CloseToMobs:
        //                    if (ObjectManager.Player.Distance(hp.Key)
        //                        <= Orbwalking.GetRealAutoAttackRange(null) + 65)
        //                    {
        //                        Render.Circle.DrawCircle(hp.Key, CircleRange, hp.Value);
        //                    }
        //                    break;
        //                case (int)DrawOption.CloseToMobsAndJungleClearActive:
        //                    if (ObjectManager.Player.Distance(hp.Key)
        //                        <= Orbwalking.GetRealAutoAttackRange(null) + 65 && Program.CClass.JungleClearActive)
        //                    {
        //                        Render.Circle.DrawCircle(hp.Key, CircleRange, hp.Value);
        //                    }
        //                    break;
        //            }
        //        }
        //    }
        //}

        public static GameObjectTeam Team(this Obj_AI_Base mob)
        {
            mobTeams = new Dictionary<Vector2, GameObjectTeam>();
            if (Game.MapId == (GameMapId) 11)
            {
                mobTeams.Add(new Vector2(7756f, 4118f), GameObjectTeam.Order); // blue team :red;
                mobTeams.Add(new Vector2(3824f, 7906f), GameObjectTeam.Order); // blue team :blue
                mobTeams.Add(new Vector2(8356f, 2660f), GameObjectTeam.Order); // blue team :golems
                mobTeams.Add(new Vector2(3860f, 6440f), GameObjectTeam.Order); // blue team :wolfs
                mobTeams.Add(new Vector2(6982f, 5468f), GameObjectTeam.Order); // blue team :wariaths
                mobTeams.Add(new Vector2(2166f, 8348f), GameObjectTeam.Order); // blue team :Frog jQuery

                mobTeams.Add(new Vector2(4768, 10252), GameObjectTeam.Neutral); // Baron
                mobTeams.Add(new Vector2(10060, 4530), GameObjectTeam.Neutral); // Dragon

                mobTeams.Add(new Vector2(7274f, 11018f), GameObjectTeam.Chaos); // Red team :red;
                mobTeams.Add(new Vector2(11182f, 6844f), GameObjectTeam.Chaos); // Red team :Blue
                mobTeams.Add(new Vector2(6450f, 12302f), GameObjectTeam.Chaos); // Red team :golems
                mobTeams.Add(new Vector2(11152f, 8440f), GameObjectTeam.Chaos); // Red team :wolfs
                mobTeams.Add(new Vector2(7830f, 9526f), GameObjectTeam.Chaos); // Red team :wariaths
                mobTeams.Add(new Vector2(12568, 6274), GameObjectTeam.Chaos); // Red team : Frog jQuery

                return
                    mobTeams.Where(
                        hp => mob.Distance(hp.Key) <= (Orbwalking.GetRealAutoAttackRange(null)*2))
                        .Select(hp => hp.Value)
                        .FirstOrDefault();
            }
            return GameObjectTeam.Unknown;
        }
    }

}
