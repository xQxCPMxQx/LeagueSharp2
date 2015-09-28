#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace Pantheon
{
    internal class Utils
    {
        public enum MobTypes
        {
            All,
            BigBoys
        }

        public static bool In<T>(T source, params T[] list)
        {
            return list.Equals(source);
        }

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
    }
}
