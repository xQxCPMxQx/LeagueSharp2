using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace LeeSin
{
    internal static class ModeHarass
    {
        public static Spell Q => Program.Q;
        public static Spell W => Program.W;

        public static Program.QCastStage QState => Program.QStage;
        public static Program.WCastStage WState => Program.WStage;

        public static void HitAndRun()
        {
            var t = AssassinManager.GetTarget(Q.Range);

            if (!t.IsValidTarget())
            {
                return;
            }

            if (WState != Program.WCastStage.IsReady)
            {
                return;
            }

            if (QState == Program.QCastStage.IsReady)
            {
                Q.Cast(t);
            }

            Obj_AI_Base obj =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(
                        o =>
                            o.IsAlly && !o.IsDead && !o.IsMe &&
                            !(o.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                            o.Distance(t.Position) < W.Range - 10
                    )
                    .OrderByDescending(o => o.Distance(t.Position)).FirstOrDefault();

            if (obj == null)
            {
                return;
            }

            if (t.HasBlindMonkBuff() && QState == Program.QCastStage.IsCasted)
            {
                Q.Cast();
            }

            if (ObjectManager.Player.Distance(t.Position) < 50 && QState == Program.QCastStage.NotReady)
            {
                W.CastOnUnit(obj);
            }
        }
    }
}
