using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Common;

namespace Nocturne
{
    internal static class PlayerSpells
    {
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R, R2;

        public static void Initialize()
        {
            Q = new Spell(SpellSlot.Q, 1100f, TargetSelector.DamageType.Physical);
            {
                Q.SetSkillshot(0.25f, 100, 1400, false, SkillshotType.SkillshotLine);
            }

            W = new Spell(SpellSlot.W);
            {
                SpellList.Add(W);
            }

            E = new Spell(SpellSlot.E, 425);
            {
                E.SetTargetted(0.50f, 75f);
            }

            R = new Spell(SpellSlot.R);
            R2 = new Spell(SpellSlot.R, 2500);
            {
                R.SetTargetted(0.50f, 75f);
            }

            SpellList.AddRange(new[] { Q, W, E, R });

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            E.Range = E.Level * 10 + 415;

            R.Range = R.Level * 750 + 1750;
        }

        public static bool ModeCanCast(this Spell spell, Obj_AI_Base t)
        {
            return !t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(t) + 65) || !ObjectManager.Player.HasSheenBuff();
        }

        public static void ModeCast(this Spell spell, Obj_AI_Base t)
        {
            if (!t.IsValidTarget(spell.Range))
            {
                return;
            }

            
            if (spell.Slot == SpellSlot.Q)
            {
                if (spell.IsReady() && spell.ModeCanCast(t) && ObjectManager.Player.Distance(t.ServerPosition) <= spell.Range) 
                {
                    var qPrediction = spell.GetPrediction(t);
                    var hithere = qPrediction.CastPosition.Extend(ObjectManager.Player.Position, -140);
                    if (qPrediction.Hitchance >= HitChance.High)
                    {
                        spell.Cast(hithere);
                    }
                }
            }

            if (spell.Slot == SpellSlot.E)
            {
                if (E.IsReady() && E.ModeCanCast(t))
                {
                    E.CastOnUnit(t);
                }
            }
        }
    }
}
