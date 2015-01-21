using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace ShowMySupportItems
{
    class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        public static Menu Config;
        private static int talentStack;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.IsDead) return;

            Game.PrintChat("<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>Show My Support's Item Stacks </font> <font color='#70DBDB'> Loaded!</font>");
            
            Config = new Menu("xQx | Show Stacks", "ShowSupportStacks", true);

            Config.AddItem(new MenuItem("ShowRelic", "Show Relic Shield").SetValue(new Circle(true, Color.GreenYellow)));
            Config.AddItem(new MenuItem("MenuX", " "));
            Config.AddItem(new MenuItem("StackRange", "Stack Control Range").SetValue(new Slider(1000, 2000)));
            Config.AddItem(new MenuItem("DrawStackRange", "Draw Control Range").SetValue(new Circle(true, Color.FromArgb(255, 176, 186, 160))));
            Config.AddToMainMenu();
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var stackRange = Config.Item("StackRange").GetValue<Slider>().Value;
            var drawControlRange = Config.Item("DrawStackRange").GetValue<Circle>();
            if (drawControlRange.Active)
            {
                Render.Circle.DrawCircle(Player.Position, stackRange, drawControlRange.Color);
            }
            
            var drawStack = Config.Item("ShowRelic").GetValue<Circle>();
            if (drawStack.Active)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly && !ally.IsMe &&
                                                                                    ObjectManager.Player.Distance(ally) < stackRange))
                {
                    foreach (var buff in ally.Buffs.Where(buff => buff.Name.Contains("talent")))
                    {
                        talentStack = buff.Count;
                    }
                    if (talentStack == 0)
                        return;
                        
                    Vector2 hpBarPosition = ally.HPBarPosition;
                    hpBarPosition.X += 20;
                    hpBarPosition.Y -= 20;
                    Drawing.DrawText(hpBarPosition.X, hpBarPosition.Y, drawStack.Color, "Relic Stack: " + talentStack.ToString());
                    Render.Circle.DrawCircle(ally.Position, 100f, drawStack.Color);
                }

                
            }
        }
    }
}
