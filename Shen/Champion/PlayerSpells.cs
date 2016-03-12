using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Shen.Champion
{
    internal static class PlayerSpells
    {
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R;

        public static void Initialize()
        {
            Q = new Spell(SpellSlot.Q, 340f);
            Q.SetTargetted(0.15f, float.MaxValue);
            SpellList.Add(Q);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 480f);
            E.SetSkillshot(0.20f, 80f, float.MaxValue, false, SkillshotType.SkillshotLine);
            SpellList.Add(E);

            R = new Spell(SpellSlot.R);
        }

        public static void CastE(Obj_AI_Base t)
        {
            var ePrediction = Shen.Champion.PlayerSpells.E.GetPrediction(t);
            var hithere = ePrediction.CastPosition.Extend(ObjectManager.Player.Position, -140);
            if (ePrediction.Hitchance >= HitChance.High)
            {
                Shen.Champion.PlayerSpells.E.Cast(hithere);
            }
        }
    }


}
