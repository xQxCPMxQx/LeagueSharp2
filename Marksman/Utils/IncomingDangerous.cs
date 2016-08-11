using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Champions;

namespace Marksman.Utils
{
    internal class IncomingDangerous
    {
        private static MenuItem menuItem;
        private static Obj_AI_Base incomingDangerous = null;
        private static string incBuffName = "";
        private static bool incDangerous = false;
        private static float incTime = 0;
        private static Spell ChampionSpell;
        public static void Initialize()
        {
            menuItem =
                new MenuItem("Activator.IncomingDangerous", "Incoming Dangerous").SetValue(
                    new StringList(new[] {"Off", "Warn with Ping", "Warn with Text", "Both"}, 3));
            Program.MenuActivator.AddItem(menuItem);

            ChampionSpell = GetSpell();
            CustomEvents.Game.OnGameLoad += args =>
            {
                Console.Clear();

            };
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
            GameObject.OnCreate += OnCreateObject;
        }

        public static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains("pantheon_base_r_indicator_red.troy"))
                Console.WriteLine(sender.Name + " : " + sender.Position.ToString());
            //Pantheon_Base_R_aoe_explosion.troy
        }


        private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            var IncomingDangerous = Program.MenuActivator.Item("Activator.IncomingDangerous").GetValue<StringList>().SelectedIndex;
            if (IncomingDangerous != 0)
            {
                BuffInstance aBuff =
                    (from fBuffs in
                         sender.Buffs.Where(
                             s => sender.IsEnemy && sender.Distance(ObjectManager.Player.Position) < 2500)
                     from b in new[]
                                   {
                                       // TODO: Add a ping warning and draw line to teleport position 
                                       "teleport_", "pantheon_grandskyfall_jump", "crowstorm", "gate",
                                   }
                     where args.Buff.Name.ToLower().Contains(b)
                     select fBuffs).FirstOrDefault();

                incomingDangerous = null;
                incDangerous = false;
                incBuffName = "";
                incTime = 0;

                if (aBuff != null)
                {
                    incBuffName = aBuff.Name;
                    if (IncomingDangerous == 1 || IncomingDangerous == 3)
                    {
                        Utils.MPing.Ping(sender.Position.To2D(), 3, PingCategory.Danger);
                    }

                    if (IncomingDangerous == 2 || IncomingDangerous == 3)
                    {
                        incDangerous = true;
                        incTime = Game.Time;
                    }
                }
            }

            //if (ObjectManager.Player.ChampionName == "Jinx" || ObjectManager.Player.ChampionName == "Caitlyn" || ObjectManager.Player.ChampionName == "Teemo")
            //{
            //    if (ChampionSpell.IsReady())
            //    {
            //        BuffInstance aBuff =
            //            (from fBuffs in
            //                sender.Buffs.Where(
            //                    s =>
            //                        sender.Team != ObjectManager.Player.Team
            //                        && sender.Distance(ObjectManager.Player.Position) < ChampionSpell.Range)
            //                from b in new[]
            //                {
            //                    "teleport_", /* Teleport */ "pantheon_grandskyfall_jump", /* Pantheon */ 
            //                    "crowstorm", /* FiddleScitck */
            //                    "zhonya", "katarinar", /* Katarita */
            //                    "MissFortuneBulletTime", /* MissFortune */
            //                    "gate", /* Twisted Fate */
            //                    "chronorevive" /* Zilean */
            //                }
            //                where args.Buff.Name.ToLower().Contains(b)
            //                select fBuffs).FirstOrDefault();

            //        if (aBuff != null)
            //        {
            //            ChampionSpell.Cast(sender.Position);
            //        }
            //    }
            //}
        }

        private static Spell GetSpell()
        {
            switch (ObjectManager.Player.ChampionName)
            {
                case "Jinx":
                    {
                        return new Spell(SpellSlot.E, 900f);
                    }
                case "Caitlyn":
                    {
                        return new Spell(SpellSlot.W, 820);
                    }
                case "Teemo":
                    {
                        return new Spell(SpellSlot.R, ObjectManager.Player.Level * 300);
                    }
            }
            return null;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (incDangerous)
            {
                var text = "";
                if (incBuffName.ToLower().Contains("teleport_"))
                {
                    text = "Danger: Enemy Teleport!";
                }
                if (incBuffName.ToLower().Contains("pantheon_grandskyfall_jump"))
                {
                    text = "Danger: Pantheon Ulti!";
                }
                if (incBuffName.ToLower().Contains("crowstorm"))
                {
                    text = "Danger: FiddleStick Ulti!";
                }
                if (incBuffName.ToLower().Contains("gate"))
                {
                    text = "Danger: TwistedFate Ulti!";
                }

                if (Game.Time < incTime + 8)
                {
                    Utils.DrawText(Utils.TextWarning, text, Drawing.Width*0.22f, Drawing.Height*0.44f, SharpDX.Color.White);
                    Utils.DrawText(Utils.Text, "You can Turn Off this message! Go to 'Marksman -> Activator -> Incoming Dangerous'", Drawing.Width*0.325f, Drawing.Height*0.52f, SharpDX.Color.White);
                }
            }

            var sender = incomingDangerous;
            if (sender != null)
            {
                Utils.DrawLine(ObjectManager.Player.Position, sender.Position, System.Drawing.Color.Red,
                    " !!!!! Danger !!!!!");
            }
        }
    }
}
