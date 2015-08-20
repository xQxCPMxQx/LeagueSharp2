using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace JaxQx
{
    internal class Jumper
    {
        public static Vector2 testSpellCast;
        public static Vector2 testSpellProj;
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static Spell Q;
        public static float lastward;
        public static float last;

        public static int GetJumpWardId()
        {
            int[] wardIds = {3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043};
            foreach (var id in wardIds.Where(id => Items.HasItem(id) && Items.CanUseItem(id)))
            {
                return id;
            }
            return -1;
        }

        public static void MoveTo(Vector2 Pos)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Pos.To3D());
        }

        public static void WardJump(Vector2 pos)
        {
            Q = new Spell(SpellSlot.Q, 700);
            if (!Q.IsReady())
                return;

            var wardIs = false;
            if (!InDistance(pos, Player.ServerPosition.To2D(), Q.Range))
            {
                pos = Player.ServerPosition.To2D() + Vector2.Normalize(pos - Player.ServerPosition.To2D())*600;
            }

            if (!Q.IsReady())
                return;

            foreach (var ally in ObjectManager.Get<Obj_AI_Base>().Where(ally => ally.IsAlly
                                                                                && !(ally is Obj_AI_Turret) &&
                                                                                InDistance(pos,
                                                                                    ally.ServerPosition.To2D(), 200)))
            {
                wardIs = true;
                MoveTo(pos);
                if (!InDistance(Player.ServerPosition.To2D(), ally.ServerPosition.To2D(), Q.Range + ally.BoundingRadius))
                    return;

                if (last < Environment.TickCount)
                {
                    Q.Cast(ally);
                    last = Environment.TickCount + 2000;
                }
                else return;

                return;
            }
            Polygon pol;
            if ((pol = Program.map.GetInWhichPolygon(pos)) != null)
            {
                if (InDistance(pol.GetProjOnPolygon(pos), Player.ServerPosition.To2D(), Q.Range) && !wardIs &&
                    InDistance(pol.GetProjOnPolygon(pos), pos, 250))
                {
                    PutWard(pos);
                }
            }
            else if (!wardIs)
            {
                PutWard(pos);
            }
        }

        public static bool PutWard(Vector2 pos)
        {
            int wardItem;
            if ((wardItem = GetJumpWardId()) == -1)
                return false;

            foreach (var slot in Player.InventoryItems.Where(slot => slot.Id == (ItemId) wardItem))
            {
                if (!(lastward < Environment.TickCount))
                    return false;

                ObjectManager.Player.Spellbook.CastSpell(slot.SpellSlot, pos.To3D());
                lastward = Environment.TickCount + 2000;
                return true;
            }
            return false;
        }

        public static bool InDistance(Vector2 pos1, Vector2 pos2, float distance)
        {
            var dist2 = Vector2.DistanceSquared(pos1, pos2);
            return (dist2 <= distance*distance);
        }
    }
}