using System;
using LeagueSharp;

namespace KaiHelper
{
    class Champion
    {
        private static int _level;
        private static int _tick;
        public static readonly Obj_AI_Hero Player = ObjectManager.Player; 
        static Champion ()
        {
            Game.OnUpdate+=Game_OnUpdate;
        }
        
        private static void Game_OnUpdate(EventArgs args)
        {
            if (Environment.TickCount >= _tick)
            {
                _tick = Environment.TickCount + 500;
                int level = ObjectManager.Player.Level;
                if (level > _level)
                {
                    _level = level;
                    if (OnLevelUp != null) 
                        OnLevelUp(Player, new OnLevelUpEventAgrs() { NewLevel = _level });
                }
            }
        }
        
        internal delegate void DelLevelUp(Obj_AI_Hero champion, OnLevelUpEventAgrs agrs);

        public static event DelLevelUp OnLevelUp;
        
        public class OnLevelUpEventAgrs : EventArgs
        {
            public int NewLevel;
        }
    }
    
}
