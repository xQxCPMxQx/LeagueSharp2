using LeagueSharp;
using SharpDX;

namespace JaxQx
{
    using LeagueSharp.Common;

    using JaxQx.Properties;

    internal class Sprite
    {
        private static Vector2 DrawPosition
        {
            get
            {
                var drawStatus = Program.AssassinManager.Config.Item("Draw.Status").GetValue<StringList>().SelectedIndex;
                if (KillableEnemy == null || (drawStatus != 2 && drawStatus != 3))
                    return new Vector2(0f, 0f);

                return new Vector2(KillableEnemy.HPBarPosition.X + KillableEnemy.BoundingRadius / 2f,
                    KillableEnemy.HPBarPosition.Y - 70);
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
                var t = Program.AssassinManager.GetTarget(Program.E.Range);

                if (t.IsValidTarget())
                    return t;

                return null;
            }
        }

        internal static void Load()
        {
            new Render.Sprite(Resources.selectedchampion, new Vector2())
            {
                PositionUpdate = () => DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();
        }
    }
}