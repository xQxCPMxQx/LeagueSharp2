#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace Marksman.Utils
{
    using Marksman.Champions;

    public class OnDraw
    {
        private static Menu menu;
        public static void Initialize()
        {
            menu = new Menu("Draws", "Draws");
            {
                menu.AddItem(new MenuItem("Draw.KillableEnemy", "Show Killable Enemy (HPBar Notification)").SetValue(true));
                menu.AddItem(new MenuItem("Draw.MinionLastHit", "Show Minion Last Hit").SetValue(new Circle(true, System.Drawing.Color.GreenYellow)));
                menu.AddItem(new MenuItem("Draw.MinionNearKill", "Show Minion Near Kill").SetValue(new Circle(true, System.Drawing.Color.Gray)));
                menu.AddItem(new MenuItem("Draw.JunglePosition", "Show Jungle Farm Position").SetValue(true));
                menu.AddItem(new MenuItem("Draw.DrawMinion", "Show Killable Enemy (Sprite)").SetValue(false));
                menu.AddItem(new MenuItem("Draw.DrawTarget", "Show Selected Target (Srpite)").SetValue(true));
                Program.Config.AddSubMenu(menu);
            } 

            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static Tuple<Obj_AI_Hero, int> KillableEnemyAa
        {
            get
            {
                var x = 0;
                var t = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null) + 400, TargetSelector.DamageType.Physical);
                {
                    if (t.IsValidTarget())
                    {
                        if (t.Health
                            < ObjectManager.Player.TotalAttackDamage
                            * (1 / ObjectManager.Player.AttackCastDelay > 1400 ? 8 : 4))
                        {
                            x = (int)Math.Ceiling(t.Health / ObjectManager.Player.TotalAttackDamage);
                        }
                        return new Tuple<Obj_AI_Hero, int>(t, x);
                    }

                }
                return new Tuple<Obj_AI_Hero, int>(t, x);
            }
        }

        public static void DrawJunglePosition()
        {
            if (Game.MapId == (GameMapId)11)
            {
                const float CircleRange = 100f;

                Render.Circle.DrawCircle(new Vector3(7461.018f, 3253.575f, 52.57141f),CircleRange,System.Drawing.Color.Blue); 
                // blue team :red
                Render.Circle.DrawCircle(new Vector3(3511.601f, 8745.617f, 52.57141f),CircleRange,System.Drawing.Color.Blue);
                // blue team :blue
                Render.Circle.DrawCircle(new Vector3(7462.053f, 2489.813f, 52.57141f),CircleRange,System.Drawing.Color.Blue);
                // blue team :golems
                Render.Circle.DrawCircle(new Vector3(3144.897f, 7106.449f, 51.89026f),CircleRange,System.Drawing.Color.Blue);
                // blue team :wolfs
                Render.Circle.DrawCircle(new Vector3(7770.341f, 5061.238f, 49.26587f),CircleRange,System.Drawing.Color.Blue);
                // blue team :wariaths

                Render.Circle.DrawCircle(new Vector3(10930.93f, 5405.83f, -68.72192f),CircleRange,System.Drawing.Color.Yellow);
                // Dragon

                Render.Circle.DrawCircle(new Vector3(7326.056f, 11643.01f, 50.21985f),CircleRange, System.Drawing.Color.Red);
                // red team :red
                Render.Circle.DrawCircle(new Vector3(11417.6f, 6216.028f, 51.00244f),CircleRange,System.Drawing.Color.Red);
                // red team :blue
                Render.Circle.DrawCircle(new Vector3(7368.408f, 12488.37f, 56.47668f),CircleRange,System.Drawing.Color.Red);
                // red team :golems
                Render.Circle.DrawCircle(new Vector3(10342.77f, 8896.083f, 51.72742f),CircleRange,System.Drawing.Color.Red);
                // red team :wolfs
                Render.Circle.DrawCircle(new Vector3(7001.741f, 9915.717f, 54.02466f),CircleRange,System.Drawing.Color.Red);
                // red team :wariaths                    
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("Draw.JunglePosition").GetValue<bool>())
            {
                DrawJunglePosition();
            }

            if (menu.Item("Draw.KillableEnemy").GetValue<bool>())
            {
                var t = KillableEnemyAa;
                if (t.Item1 != null && t.Item1.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 400)
                    && t.Item2 > 0)
                {
                    Utils.DrawText(
                        Utils.Text,
                        string.Format("{0}: {1} x AA Damage = Kill", t.Item1.ChampionName, t.Item2),
                        (int)t.Item1.HPBarPosition.X + 145,
                        (int)t.Item1.HPBarPosition.Y + 5,
                        SharpDX.Color.White);
                }
            }


            var drawMinionLastHit = menu.Item("Draw.MinionLastHit").GetValue<Circle>();
            var drawMinionNearKill = menu.Item("Draw.MinionNearKill").GetValue<Circle>();
            if (drawMinionLastHit.Active || drawMinionNearKill.Active)
            {
                var minions = MinionManager.GetMinions(
                    ObjectManager.Player.Position,
                    ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius + 300,
                    MinionTypes.All,
                    MinionTeam.Enemy,
                    MinionOrderTypes.MaxHealth);

                foreach (var xMinion in minions)
                {
                    if (drawMinionLastHit.Active
                        && ObjectManager.Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health)
                    {
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color);
                    }
                    else if (drawMinionNearKill.Active
                             && ObjectManager.Player.GetAutoAttackDamage(xMinion, true) * 2 >= xMinion.Health)
                    {
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionNearKill.Color);
                    }
                }
            }
        }

    }

}
