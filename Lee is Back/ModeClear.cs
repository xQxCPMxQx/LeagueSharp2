using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LeagueSharp;
using LeagueSharp.Common;

namespace LeeSin
{
    internal static class ModeClear
    {
        public static Spell Q => Program.Q;
        public static Spell W => Program.W;
        public static Spell E => Program.E;

        public static Program.QCastStage QState => Program.QStage;
        public static Program.WCastStage WState => Program.WStage;
        public static Program.ECastStage EState => Program.EStage;

        private static Menu LocalMenu => Program.MenuJungle;

        public static class Jungle
        {
            public static void Active()
            {
                var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                    MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (!mobs.Any())
                {
                    return;
                }

                var jungleMob = mobs[0];
                var bigBoys = JungleUtils.GetMobs(Q.Range, JungleUtils.MobTypes.BigBoys);
                var useQ = LocalMenu.Item("Jungle.UseQ").GetValue<StringList>().SelectedIndex;

                bool canCastQ = useQ != 0;

                if (bigBoys != null)
                {
                    Console.WriteLine(bigBoys.SkinName);
                }
                if (useQ == 2)
                {
                    canCastQ = bigBoys != null;
                }
                else if (useQ == 1)
                {
                    canCastQ = true;
                }
                else if (ObjectManager.Player.Health < 100)
                {
                    canCastQ = true;
                }

                if (canCastQ)
                {
                    if (QState == Program.QCastStage.IsReady)
                    {
                        if (jungleMob.SkinName == "Sru_Crab")
                        {
                            Q.Cast(jungleMob.Position);
                        }
                        else
                        {
                            Q.Cast(jungleMob);
                        }
                    }
                }

                if (QState == Program.QCastStage.IsCasted && Environment.TickCount > Program.QCastTime + 2700)
                {
                    Q.Cast();
                }

                if (QState == Program.QCastStage.IsCasted &&
                    (jungleMob.HasBuff("BlindMonkQOne") || jungleMob.HasBuff("blindmonkqonechaos")))
                {
                    if (jungleMob.Health < Q.GetDamage(jungleMob))
                    {
                        Q.Cast();
                    }
                    else if (!jungleMob.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 165))
                    {
                        Q.Cast();
                    }
                }

                if (jungleMob.Health < Q.GetDamage(jungleMob) && QState == Program.QCastStage.IsReady)
                {
                    Q.Cast();
                    return;
                }

                if (LocalMenu.Item("Jungle.UseW").GetValue<bool>() || ObjectManager.Player.Health < 100)
                {
                    if (!Program.HavePassiveBuff && WState == Program.WCastStage.IsReady &&
                        jungleMob.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                    {
                        W.CastOnUnit(ObjectManager.Player);
                        return;
                    }

                    if (WState == Program.WCastStage.IsCasted && Environment.TickCount > Program.WCastTime + 2500)
                    {
                        W.Cast();
                    }
                }

                if (LocalMenu.Item("Jungle.UseE").GetValue<bool>() || ObjectManager.Player.Health < 100)
                {
                    if (!Program.HavePassiveBuff && EState == Program.ECastStage.IsReady &&
                        jungleMob.IsValidTarget(E.Range))
                    {
                        E.Cast();
                        return;
                    }
                }

                if (LocalMenu.Item("Jungle.UseItems").GetValue<bool>())
                {
                    foreach (var item in from item in GameItems.ItemDb
                        where
                            item.Value.ItemType == GameItems.EnumItemType.AoE &&
                            item.Value.TargetingType == GameItems.EnumItemTargettingType.EnemyObjects
                        let iMinions = mobs
                        where
                            item.Value.Item.IsReady() &&
                            iMinions[0].IsValidTarget(Orbwalking.GetRealAutoAttackRange(null))
                        select item)
                    {
                        item.Value.Item.Cast();
                    }
                }
            }
        }

        public static class Lane
        {
            public static void Active()
            {
            }
        }
    }
}
