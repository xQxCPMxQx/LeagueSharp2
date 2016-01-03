using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;

namespace LeeSin
{
    public static class Utils
    {
        public static bool HasBlindMonkBuff(this Obj_AI_Base t)
        {
            return t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos");
        }
    }
    internal static class Combos
    {
        public static IEnumerable<Obj_AI_Base> QGetCollisionMinions(Vector3 source, Vector3 targetposition, float width, float range, CollisionableObjects[] collisionObjects)
        {
            PredictionInput input = new PredictionInput {From = source, Radius = width, Range = range};

            if (collisionObjects.Length > 0)
            {
                for (int i = 0; collisionObjects.Length != 0; i ++)
                {
                    input.CollisionObjects[i] = collisionObjects[i];
                }
            }
            else
            {
                input.CollisionObjects[0] = CollisionableObjects.Minions;
            }

            return
                Collision.GetCollision(new List<Vector3> {targetposition}, input).OrderBy(obj => obj.Distance(source)).ToList();
        }

        public static double CalculateDamage()
        {
            int level = ObjectManager.Player.Level;
            int[] stages =
            {
                20*level + 370,
                30*level + 330,
                40*level + 240,
                50*level + 100
            };
            return stages.Max();
        }
        public static void SmiteQCombo(Spell spell)
        {
            if (Program.QStage != Program.QCastStage.IsReady)
            {
                return;
            }

            var t = AssassinManager.GetTarget(spell.Range);
            if (!t.IsValidTarget())
            {
                return;
            }

            if (t.HasBlindMonkBuff())
            {
                return;
            }
            int smiteDamage = (int) CalculateDamage();
            

            //if (Program.Q.GetPrediction(t).CollisionObjects.Count == 1)
            //{
            //    Obj_AI_Base colObj = Program.Q.GetPrediction(t).CollisionObjects.FirstOrDefault();
            //    if (colObj != null && colObj.Health < smiteDamage && colObj.IsValidTarget(550f))
            //    {
            //        if (Program.SmiteDamageSlot != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(Program.SmiteDamageSlot) == SpellState.Ready)
            //        {
            //            spell.Cast(t.Position);
            //            ObjectManager.Player.Spellbook.CastSpell(Program.SmiteDamageSlot, colObj);
            //        }
            //    }
            //}

            //return;

            IEnumerable<Obj_AI_Base> xM = QGetCollisionMinions(ObjectManager.Player.Position, t.ServerPosition, spell.Width, spell.Range, new CollisionableObjects[(int) CollisionableObjects.Minions]);

            IEnumerable<Obj_AI_Base> objAiBases = xM as Obj_AI_Base[] ?? xM.ToArray();

            if (xM != null && objAiBases.Count() == 1)
            {
                Obj_AI_Base xxx = objAiBases.FirstOrDefault();

                if (xxx != null)
                {
                    if (Program.SmiteDamageSlot != SpellSlot.Unknown &&
                        ObjectManager.Player.Spellbook.CanUseSpell(Program.SmiteDamageSlot) == SpellState.Ready)
                    {
                        if (xxx.Health < smiteDamage && spell.IsReady() && xxx.Distance(ObjectManager.Player.Position) < 650)
                        {
                            spell.Cast(t.Position);
                            ObjectManager.Player.Spellbook.CastSpell(Program.SmiteDamageSlot, xxx);
                        }
                    }
                }
            }
        }
    }
}
