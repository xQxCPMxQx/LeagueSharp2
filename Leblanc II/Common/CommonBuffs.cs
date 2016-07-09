using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace Leblanc.Common
{

    public static class CommonBuffs
    {
        public static List<BuffDatabase> BuffDb = new List<BuffDatabase>();

        public class BuffDatabase
        {
            public string BuffName;

            public BuffDatabase() { }

            public BuffDatabase(string buffName)
            {
                BuffName = buffName;
            }
        }

        public static void Init()
        {
            BuffDb.Add(new BuffDatabase
            {
                BuffName = "JudicatorIntervention"
            });

            BuffDb.Add(new BuffDatabase
            {
                BuffName = "Undying Rage"
            });
        }

        public static bool HasImmortalBuff(this Obj_AI_Base obj)
        {
            return (from b in obj.Buffs join b1 in BuffDb on b.DisplayName equals b1.BuffName select new {b, b1}).Distinct().Any();
        }

        public static bool HasMarkedWithQ(this Obj_AI_Base obj)
        {
            return obj.HasBuff("leblancchaosorb");
        }

        public static bool HasSheenBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.Name.ToLower() == "sheen");
        }

        public static bool LeblancHaveFrenziedStrikes
        {
            get { return ObjectManager.Player.Buffs.Any(buff => buff.DisplayName == "LeblancFrenziedStrikes"); }
        }

        public static bool LeblancHaveRagnarok
        {
            get { return ObjectManager.Player.Buffs.Any(buff => buff.DisplayName == "LeblancRagnarok"); }
        }

        public static bool LeblancHasAttackSpeedBuff
        {
            get
            {
                return ObjectManager.Player.Buffs.Any(buff => buff.DisplayName == "SpectralFury");
            }
        }

        public static bool HasSoulShackle(this Obj_AI_Hero t)
        {
            return t.Buffs.Any(buff => buff.DisplayName.Equals("Leblancshacklebeam", StringComparison.InvariantCultureIgnoreCase)); 
        }


        public static bool CanKillableWith(this Obj_AI_Base t, Spell spell)
        {
            return t.Health < spell.GetDamage(t) - 5;
        }

        public static bool CanStun(this Obj_AI_Base t)
        {
            float targetHealth = Champion.PlayerSpells.Q.IsReady() && !t.IsValidTarget(Champion.PlayerSpells.E.Range)
                ? t.Health + Champion.PlayerSpells.Q.GetDamage(t)
                : t.Health;
            return targetHealth / t.MaxHealth * 100 > ObjectManager.Player.Health / ObjectManager.Player.MaxHealth * 100;

            //return t.HealthPercent > ObjectManager.Player.HealthPercent;
        }


        public static bool HasPassive(this Obj_AI_Base obj)
        {
            return obj.PassiveCooldownEndTime - (Game.Time - 15.5) <= 0;
        }

        public static bool HasBuffInst(this Obj_AI_Base obj, string buffName)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == buffName);
        }

        public static bool HasBlueBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "CrestoftheAncientGolem");
        }

        public static bool HasRedBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "BlessingoftheLizardElder");
        }
    }
}