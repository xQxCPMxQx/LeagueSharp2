#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Geometry = Nocturne.Common.Geometry;

#endregion

namespace Nocturne
{
    internal class Nocturne
    {

        
        public static Common.ItemManager Items;
        
        //private static readonly NocturneQ nocturneQ = new NocturneQ();
        //Menu
        
        public static void Initialize()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            PlayerSpells.Initialize();
            PlayerMenu.Initialize();
            
            Modes.ModeCombo.Initialize();
            Modes.ModeHarass.Initialize();
//            Modes.ModeLane.Initialize();
  //          Modes.ModeJungle.Initialize();
            Common.ItemManager.Initialize();
            //Common.Items.In
            
            Items = new Common.ItemManager();

            //Create the menu

            Game.OnUpdate += Game_OnUpdate;
            Game.PrintChat($"{Program.ChampionName} Loaded");
            Console.Clear();
        }

      

     


        public static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0d;

            if (PlayerSpells.Q.IsReady())
            {
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);
            }

            if (PlayerSpells.W.IsReady())
            {
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);
            }

            if (PlayerSpells.E.IsReady())
            {
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);
            }

            if (Common.SummonerManager.IgniteSlot != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.IgniteSlot) == SpellState.Ready)
            {
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);
            }

            if (LeagueSharp.Common.Items.CanUseItem(3128))
            {
                fComboDamage += ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);
            }

            return (float)fComboDamage;
        }


        private static void Game_OnUpdate(EventArgs args)
        {

            foreach (var buff in ObjectManager.Player.Buffs.Where(b => b.DisplayName.Contains("usk")))
            {
                //Blue Buff: CrestoftheAncientGolem
                //Q Buff: NocturneDuskbringer
              //  Console.WriteLine(buff.DisplayName + " : " + buff.StartTime + " : " + buff.EndTime);
            }
            //Console.WriteLine(@"_____________________________________________");
            if (PlayerMenu.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                ExecuteCombo();
            }

            if (PlayerMenu.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                ExecuteLane();
                
            }
        }

        private static void ExecuteCombo()
        {
            var t = TargetSelector.GetTarget(PlayerSpells.Q.Range, TargetSelector.DamageType.Physical);
            if (t == null)
            {
                return;
            }
        }

        private static void ExecuteLane()
        {

        }
    }
}