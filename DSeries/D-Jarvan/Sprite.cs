using System;
using System.Collections.Generic;
using System.Linq;
using D_Jarvan.Properties;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

// credit legacy

namespace D_Jarvan
{
    internal class Sprite
    {

        private static Vector2 DrawPosition
        {
            get
            {
                if (KillableEnemy == null ||
                    !Program.Config.Item("DrawSprite").GetValue<bool>())
                    return new Vector2(0f, 0f);

                return new Vector2(KillableEnemy.HPBarPosition.X + KillableEnemy.BoundingRadius / 2f,
                    KillableEnemy.HPBarPosition.Y - 50);
            }
        }

        private static bool DrawSprite
        {
            get { return true; }
        }

        private static Obj_AI_Hero KillableEnemy
        {
            get
            {
                var t = Program.GetTarget(Program.E.Range);

                if (t.IsValidTarget())
                    return t;

                return null;
            }
        }

        internal static void Load()
        {
            Notifications.AddNotification(String.Format("{0} Loaded", "sssss"), 4000);
            new Render.Sprite(Resources.selectedchampion, new Vector2())
            {
                PositionUpdate = () => DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();
        }
    }
}