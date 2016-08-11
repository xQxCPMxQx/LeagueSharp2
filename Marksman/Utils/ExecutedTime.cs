using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Marksman.Utils
{
    internal class ExecutedTime
    {
        private static MenuItem menuItem;
        public static float time = 0;
        public static void Initialize()
        {
            menuItem =
                new MenuItem("Activator.ExecuteTime", "Show Executed Time").SetValue(new KeyBind("N".ToCharArray()[0],KeyBindType.Toggle));

            Program.MenuActivator.AddItem(menuItem);

            Game.OnUpdate += args =>
            {
                foreach (var b in ObjectManager.Player.Buffs)
                {
                    if (b.Caster.IsEnemy && b.EndTime > time)
                    {
                        time = Game.Time;
                    }
                }
            };

            //Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
            //{
            //    if (sender != null && sender.IsEnemy && args.Target.IsMe && sender is Obj_AI_Hero)
            //    {
            //        time = Game.Time;
            //    }
            //};
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!menuItem.GetValue<KeyBind>().Active)
            {
                return;
            }
            
            var executedTime = Game.Time - time;
            string text;
            var t = 13 - executedTime < 0 ? 0 : 13 - executedTime;
            text = executedTime > 13 ? "Executed: Ready" : string.Format("Remaining: 0:{0:D2}", (int) t);
            var heropos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            Drawing.DrawText(heropos.X, heropos.Y + 20, System.Drawing.Color.Red, text);
            //Drawing.DrawText(heropos.X, heropos.Y + 50, System.Drawing.Color.Red, "Length: " + ObjectManager.Player.Path.Length + " Count: " + ObjectManager.Player.Path.Count());
        }
    }
}
