using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Champion;
using Nocturne.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Nocturne.Modes
{
    internal static class ModeLane
    {
        public static Menu MenuLocal { get; private set; }

        public static void Init(Menu mainMenu)
        {
            MenuLocal = new Menu("Lane", "Lane");
            {

                string[] strQ = new string[6];
                {
                    strQ[0] = "Off";
                    strQ[1] = "Just for out of AA range";
                    for (var i = 2; i < 6; i++)
                    {
                        strQ[i] = "Minion Count >= " + i;
                    }
                    MenuLocal.AddItem(new MenuItem("Lane.UseQ", "Q:").SetValue(new StringList(strQ, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor());
                    MenuLocal.AddItem(new MenuItem("Lane.UseQ.Mode", "Q: Cast Mode:").SetValue(new StringList(new[] {"Cast for Hit Minions", "Cast for Kill Minions"}, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor());
                }
                
                MenuLocal.AddItem(new MenuItem("Lane.Item", "Items:").SetValue(new StringList(new[] { "Off", "On" }, 1))).SetFontStyle(FontStyle.Regular, Colors.ColorItems);
            }
            mainMenu.AddSubMenu(MenuLocal);

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && ModeConfig.MenuFarm.Item("Farm.Enable").GetValue<KeyBind>().Active)
            {
                Execute(); 
            }
        }

        static List<Obj_AI_Base> Q_GetCollisionMinions(Obj_AI_Hero source, Vector3 targetposition)
        {
            var input = new PredictionInput
            {
                Unit = source,
                Radius = PlayerSpells.Q.Width,
                Delay = PlayerSpells.Q.Delay,
                Speed = PlayerSpells.Q.Speed,
            };

            input.CollisionObjects[0] = CollisionableObjects.Minions;

            return Collision.GetCollision(new List<Vector3> { targetposition }, input).OrderBy(obj => obj.Distance(source)).ToList();
        }

        private static void Execute()
        {
            if (ObjectManager.Player.ManaPercent < CommonManaManager.LaneMinManaPercent)
            {
                return;
            }

            var laneUseQ = MenuLocal.Item("Lane.UseQ").GetValue<StringList>().SelectedIndex;
            if (laneUseQ == 0)
            {
                return;
            }

            var minionQ = MinionManager.GetMinions(PlayerSpells.Q.Range - 30, MinionTypes.All, MinionTeam.NotAlly);

            if (MenuLocal.Item("Lane.UseQ.Mode").GetValue<StringList>().SelectedIndex == 1 )
            {
                minionQ = minionQ.FindAll(m => m.Health < PlayerSpells.Q.GetDamage(m));
            }

            if (minionQ.Count <= 0)
            {
                return;
            }

            MinionManager.FarmLocation pos = PlayerSpells.Q.GetLineFarmLocation(minionQ);

            if (PlayerSpells.Q.IsReady() && pos.MinionsHit >= laneUseQ)
            {
                PlayerSpells.Q.Cast(pos.Position);
            }
        }
    }
}
