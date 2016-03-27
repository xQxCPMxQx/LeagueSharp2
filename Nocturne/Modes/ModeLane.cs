using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Nocturne.Modes
{
    internal static class ModeLane
    {
        public static Menu LocalMenu { get; private set; }

        public static void Initialize(Menu mainMenu)
        {
            LocalMenu = new Menu("Lane", "Lane");
            {
                LocalMenu.AddItem(new MenuItem("Lane.Enable", ":: Quick Enable/Disable Lane Clear ").SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, ObjectManager.Player.ChampionName + " : " + "Farm Lane", Colors.ColorPermaShow);
                LocalMenu.AddItem(new MenuItem("Lane.UnderTurret", ":: Auto Enable Lane Mode Under Ally Turret ").SetValue(true));

                string[] strQ = new string[6];
                {
                    strQ[0] = "Off";
                    strQ[1] = "Just for out of AA range";
                    for (var i = 2; i < 6; i++)
                    {
                        strQ[i] = "Minion Count >= " + i;
                    }
                    LocalMenu.AddItem(new MenuItem("Lane.UseQ", "Q:").SetValue(new StringList(strQ, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor());
                    LocalMenu.AddItem(new MenuItem("Lane.UseQ.Mode", "Q: Cast Mode:").SetValue(new StringList(new[] {"Cast for Hit Minions", "Cast for Kill Minions"}, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor());
                }
                
                LocalMenu.AddItem(new MenuItem("Lane.Item", "Items:").SetValue(new StringList(new[] { "Off", "On" }, 1))).SetFontStyle(FontStyle.Regular, Colors.ColorItems);
            }
            mainMenu.AddSubMenu(LocalMenu);

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
            {
                return;
            }

            Execute();
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
            if (!LocalMenu.Item("Lane.Enable").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (ObjectManager.Player.ManaPercent < CommonManaManager.LaneMinManaPercent)
            {
                return;
            }

            var laneUseQ = LocalMenu.Item("Lane.UseQ").GetValue<StringList>().SelectedIndex;
            if (laneUseQ == 0)
            {
                return;
            }

            var minionQ = MinionManager.GetMinions(PlayerSpells.Q.Range - 30, MinionTypes.All, MinionTeam.NotAlly);

            if (LocalMenu.Item("Lane.UseQ.Mode").GetValue<StringList>().SelectedIndex == 1 )
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
