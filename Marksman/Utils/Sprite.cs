using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Properties;
using SharpDX;

// credit legacy

namespace Marksman
{
    internal class Sprite
    {
        private static Vector2 DrawPosition
        {
            get
            {
                if (KillableEnemy == null)
                    return new Vector2(0f, 0f);

                if (!Program.CClass.Config.Item("Draw.DrawTarget").GetValue<bool>())
                    return new Vector2(0f, 0f);

                return new Vector2(KillableEnemy.HPBarPosition.X + KillableEnemy.BoundingRadius/2f,
                    KillableEnemy.HPBarPosition.Y - 50);
            }
        }

        private static Vector2 DrawMinionPosition
        {
            get
            {
                if (KillableMinion == null)
                    return new Vector2(0f, 0f);

                if (!Program.CClass.Config.Item("Draw.DrawMinion").GetValue<bool>())
                    return new Vector2(0f, 0f);

                return new Vector2(
                    Drawing.WorldToScreen(KillableMinion.Position).X - KillableMinion.BoundingRadius/2f,
                    Drawing.WorldToScreen(KillableMinion.Position).Y - KillableMinion.BoundingRadius/0.5f);
            }
        }

        private static bool DrawSprite
        {
            get { return true; }
        }

        private static Obj_AI_Minion KillableMinion
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .OrderBy(hero => hero.Health)
                        .FirstOrDefault(
                            hero =>
                                hero.IsValidTarget(ObjectManager.Player.AttackRange +
                                                   ObjectManager.Player.BoundingRadius + 300) &&
                                hero.Health <= ObjectManager.Player.GetAutoAttackDamage(hero, true)*2);
            }
        }

        private static Obj_AI_Hero KillableEnemy
        {
            get
            {
                var t = TargetSelector.SelectedTarget;
                ;
                if (!t.IsValidTarget())
                    t = TargetSelector.GetTarget(1100, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget() && ObjectManager.Player.Distance(t) < 1200)
                    return t;

                return t;
            }
        }

        internal static void Load()
        {
            new Render.Sprite(Resources.selectedminion, new Vector2())
            {
                PositionUpdate = () => DrawMinionPosition, //DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();


            new Render.Sprite(Resources.selectedchampion, new Vector2())
            {
                PositionUpdate = () => DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();
        }
    }
}