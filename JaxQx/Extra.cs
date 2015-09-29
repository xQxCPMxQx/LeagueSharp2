using System;
using System.Collections.Generic;
using LeagueSharp.Common;

namespace JaxQx
{
    using System.Linq;

    using LeagueSharp;

    internal class Extra
    {
        private static Menu menuExtra;


        public Extra()
        {
            menuExtra = new Menu("Extra", "Extra");
            menuExtra.AddItem(new MenuItem("Extra.DrawKillableEnemy", "Killable Enemy Notification").SetValue(true));
            menuExtra.AddItem(
                new MenuItem("Extra.DrawMinionLastHist", "Draw Minion Last Hit").SetValue(
                    new Circle(true, System.Drawing.Color.GreenYellow)));
            Program.Config.AddSubMenu(menuExtra);

            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawMinionLastHit = menuExtra.Item("Extra.DrawMinionLastHist").GetValue<Circle>();
            if (drawMinionLastHit.Active)
            {
                foreach (
                    var xMinion in
                        MinionManager.GetMinions(
                            Program.Player.Position,
                            Program.Player.AttackRange + Program.Player.BoundingRadius + 300,
                            MinionTypes.All,
                            MinionTeam.Enemy,
                            MinionOrderTypes.MaxHealth)
                            .Where(xMinion => Program.Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health))
                {
                    Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color);
                }
            }

            if (menuExtra.Item("Extra.DrawKillableEnemy").GetValue<bool>())
            {
                var t = KillableEnemyAA;
                if (t.Item1 != null && t.Item1.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 800) && t.Item2 > 0)
                {
                    Utils.DrawText(
                        Utils.Text,
                        string.Format("{0}: {1} x AA Damage = Kill", t.Item1.ChampionName, t.Item2),
                        (int)t.Item1.HPBarPosition.X + 65,
                        (int)t.Item1.HPBarPosition.Y + 5,
                        SharpDX.Color.White);
                }
            }
        }

        private static Tuple<Obj_AI_Hero, int> KillableEnemyAA
        {
            get
            {
                var x = 0;
                var t = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null) + 800, TargetSelector.DamageType.Physical);
                {
                    if (t.IsValidTarget())
                    {
                        if (t.Health
                            < ObjectManager.Player.TotalAttackDamage
                            * (1 / ObjectManager.Player.AttackCastDelay > 1500 ? 12 : 8))
                        {
                            x = (int)Math.Ceiling(t.Health / ObjectManager.Player.TotalAttackDamage);
                        }
                        return new Tuple<Obj_AI_Hero, int>(t, x);
                    }

                }
                return new Tuple<Obj_AI_Hero, int>(t, x);
            }
        }
    }
}