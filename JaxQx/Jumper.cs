using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace JaxQx
{
    class Jumper
    {
        public static Vector2 testSpellCast;
        public static Vector2 testSpellProj;

        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static Spell W;

        public static Obj_AI_Hero LockedTarget;
        public static float lastward = 0;
        public static float last = 0;

        public static int getJumpWardId()
        {
            int[] wardIds = { 3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043 };
            foreach (int id in wardIds)
            {
                if (Items.HasItem(id) && Items.CanUseItem(id))
                    return id;
            }
            return -1;
        }

        public static void moveTo(Vector2 Pos)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Pos.To3D());
        }

        public static void wardJump(Vector2 pos)
        {
            W = new Spell(SpellSlot.Q, 700);
            Vector2 posStart = pos;
            if (!W.IsReady())
                return;
            bool wardIs = false;
            if (!inDistance(pos, Player.ServerPosition.To2D(), W.Range+15))
            {
                pos = Player.ServerPosition.To2D() + Vector2.Normalize(pos - Player.ServerPosition.To2D())*600;
            }

            if(!W.IsReady() && W.ChargedSpellName == "")
                return;
            foreach (Obj_AI_Base ally in ObjectManager.Get<Obj_AI_Base>().Where(ally => ally.IsAlly
                && !(ally is Obj_AI_Turret) && inDistance(pos, ally.ServerPosition.To2D(), 200)))
            {
                    wardIs = true;
                moveTo(pos);
                if (inDistance(Player.ServerPosition.To2D(), ally.ServerPosition.To2D(), W.Range + ally.BoundingRadius))
                {
                    if (last < Environment.TickCount)
                    {
                        W.Cast(ally);
                        last = Environment.TickCount + 2000;
                    }
                    else return;
                }
                return;
            }
            Polygon pol;
            if ((pol = Program.map.getInWhichPolygon(pos)) != null)
            {
                if (inDistance(pol.getProjOnPolygon(pos), Player.ServerPosition.To2D(), W.Range + 15) && !wardIs && inDistance(pol.getProjOnPolygon(pos), pos, 200))
                {
                    putWard(pos);
                }
                
            }
            else if(!wardIs)
            {
                    putWard(pos);
            }

        }

        public static bool putWard(Vector2 pos)
        {
            int wardItem;
            if ((wardItem = getJumpWardId()) != -1)
            {
                foreach (var slot in Player.InventoryItems.Where(slot => slot.Id == (ItemId)wardItem))
                {
                    if (lastward < Environment.TickCount)
                    {
                        slot.UseItem(pos.To3D());
                        lastward = Environment.TickCount + 2000;
                        return true;
                    }
                    else
                        return false;
                }
            }
            return false;
        }


        public static bool inDistance(Vector2 pos1, Vector2 pos2, float distance)
        {
            float dist2 = Vector2.DistanceSquared(pos1, pos2);
            return (dist2 <= distance * distance) ? true : false;
        }
    }
}
