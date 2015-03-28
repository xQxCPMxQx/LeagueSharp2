#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace Orianna
{
    internal class BallManager
    {
        internal static Vector3 CurrentBallPosition;
        internal static Vector3 CurrentBallDrawPosition;
        internal static bool IsBallMoving;
        internal static int QSpeed = 1200;
        internal static int ESpeed = 1700; 

        static BallManager()
        {
            Game.OnUpdate += Game_OnUpdate;
           // Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("orianaghostself", true))
            {
                CurrentBallPosition = ObjectManager.Player.ServerPosition;
                CurrentBallDrawPosition = ObjectManager.Player.Position;
                IsBallMoving = false;
                return;
            }

            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly && !ally.IsDead && ally.HasBuff("orianaghost", true)))
            {
                CurrentBallPosition = ally.ServerPosition;
                CurrentBallDrawPosition = ally.Position;
                IsBallMoving = false;
                return;
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            SpellSlot castedSlot = ObjectManager.Player.GetSpellSlot(args.SData.Name);
            if (!sender.IsMe) return;

            if(castedSlot == SpellSlot.Q)
            {
                IsBallMoving = true;
                Utility.DelayAction.Add((int)Math.Max(1, 1000 * (args.End.Distance(CurrentBallPosition) - Game.Ping - 0.1) / QSpeed), () =>
                {
                    CurrentBallPosition = args.End;
                    CurrentBallDrawPosition = args.End;
                    IsBallMoving = false;
                });
            }

            if(castedSlot == SpellSlot.E)
            {
                if(!args.Target.IsMe && args.Target.IsAlly)
                {
                    IsBallMoving = true;
                }
                if (args.Target.IsMe && CurrentBallPosition != ObjectManager.Player.ServerPosition)
                {
                    IsBallMoving = true;
                }
            }
        }
    }
}