using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace Nocturne
{
    internal static class PlayerMana
    {
        public static Spell Q => PlayerSpells.Q;
        public static Menu MenuMana => PlayerMenu.MenuMana;

        public enum MobTypes
        {
            None,
            Small,
            Big,
            BaronDragon
        }

        public enum GameObjectTeam
        {
            Unknown = 0,
            Order = 100,
            Chaos = 200,
            Neutral = 300,
        }

        private static Dictionary<Vector2, GameObjectTeam> mobTeams;

        public static GameObjectTeam Team(this Obj_AI_Base mob, float range)
        {
            mobTeams = new Dictionary<Vector2, GameObjectTeam>();
            if (Game.MapId == (GameMapId)11)
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

                return mobTeams.Where(hp => mob.Distance(hp.Key) <= (range)).Select(hp => hp.Value).FirstOrDefault();
            }
            return GameObjectTeam.Unknown;
        }

        public static MobTypes GetMobType(Obj_AI_Base mobs)
        {
            if (mobs == null)
            {
                return MobTypes.None;
            }

            // Return Baron + Dragon + RiftHerald
            Obj_AI_Base bMob = (
                from fBigBoys in new[]
                {
                    "SRU_Baron", "SRU_Dragon", "SRU_RiftHerald"
                }
                where fBigBoys == mobs.SkinName
                select mobs).FirstOrDefault();

            if (bMob != null)
            {
                return MobTypes.BaronDragon;
            }

            // Return Big Mob
            Obj_AI_Base oMob = (
                from fBigBoys in
                    new[]
                    {
                        "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "Sru_Crab"
                    }
                where fBigBoys == mobs.SkinName
                select mobs).FirstOrDefault();

            if (oMob != null)
            {
                return MobTypes.Big;
            }

            // Return Other Small
            return MobTypes.Small;
        }

        public static float HarassMinManaPercent
            =>
                MenuMana.Item("MinMana.Enable").GetValue<KeyBind>().Active
                    ? MenuMana.Item("MinMana.Harass").GetValue<Slider>().Value
                    : 0f;

        public static float ToggleMinManaPercent
            =>
                MenuMana.Item("MinMana.Enable").GetValue<KeyBind>().Active
                    ? MenuMana.Item("MinMana.Toggle").GetValue<Slider>().Value
                    : 0f;

        public static float LaneMinManaPercent
        {
            get
            {
                if (MenuMana.Item("MinMana.Enable").GetValue<KeyBind>().Active)
                {
                    return HeroManager.Enemies.Find(e => e.IsValidTarget(2000) && !e.IsZombie) == null
                        ? MenuMana.Item("MinMana.Lane.Alone").GetValue<Slider>().Value
                        : MenuMana.Item("MinMana.Lane.Enemy").GetValue<Slider>().Value;
                }

                return 0f;
            }
        }

        public static float JungleMinManaPercent
        {
            get
            {
                List<Obj_AI_Base> mobs = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range,
                    MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Count <= 0) return 0f;

                // Enable / Disable Min Mana
                if (!MenuMana.Item("MinMana.Enable").GetValue<KeyBind>().Active)
                {
                    return 0f;
                }

                // Don't Control Min Mana 
                if (MenuMana.Item("ManaMin.Jungle.DontCheck").GetValue<bool>() &&
                    mobs[0].Team(Q.Range) != (GameObjectTeam)ObjectManager.Player.Team &&
                    (mobs[0].SkinName == "SRU_Blue" || mobs[0].SkinName == "SRU_Red"))
                {
                    return 0f;
                }

                // Return Min Mana Baron / Dragon
                if (GetMobType(mobs[0]) == MobTypes.BaronDragon)
                {
                    return MenuMana.Item("ManaMin.Jungle.BigBoys").GetValue<Slider>().Value;
                }

                // Return Min Mana Ally Big / Small
                if (mobs[0].Team(Q.Range) == (GameObjectTeam)ObjectManager.Player.Team)
                {
                    return GetMobType(mobs[0]) == MobTypes.Big
                        ? MenuMana.Item("ManaMin.Jungle.AllyBig").GetValue<Slider>().Value
                        : MenuMana.Item("ManaMin.Jungle.AllySmall").GetValue<Slider>().Value;
                }

                // Return Min Mana Enemy Big / Small
                if (mobs[0].Team(Q.Range) != (GameObjectTeam)ObjectManager.Player.Team)
                {
                    return GetMobType(mobs[0]) == MobTypes.Big
                        ? MenuMana.Item("ManaMin.Jungle.EnemyBig").GetValue<Slider>().Value
                        : MenuMana.Item("ManaMin.Jungle.EnemySmall").GetValue<Slider>().Value;
                }

                return 0f;
            }
        }
    }
}
