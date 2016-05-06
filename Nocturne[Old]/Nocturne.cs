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
using Geometry = Nocturne.Common.CommonGeometry;

#endregion

namespace Nocturne
{
    internal class Nocturne
    {

        
        public static Common.CommonItems Items;
        
        //private static readonly NocturneQ NocturneQ = new NocturneQ();
        //Menu
        
        public static void Initialize()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            PlayerSpells.Initialize();
            Modes.ModeConfig.Initialize();
            
            
            //Modes.ModeHarass.Initialize();
//            Modes.ModeLane.Initialize();
  //          Modes.ModeJungle.Initialize();
            Common.CommonItems.Initialize();
            
            Items = new Common.CommonItems();

            //Create the menu

            Game.OnUpdate += Game_OnUpdate;
            Game.PrintChat("<font color='#ff3232'>Successfully Loaded: </font><font color='#d4d4d4'><font color='#FFFFFF'>" +  Program.ChampionName + "</font>");

            Console.Clear();
        }

        public static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0d;

            if (ObjectManager.Player.HasPassive())
            {
                fComboDamage += ObjectManager.Player.TotalAttackDamage*1.2;
            }

            if (PlayerSpells.Q.IsReady())
            {
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);
            }

            if (PlayerSpells.E.IsReady())
            {
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);
            }

            if (PlayerSpells.R.IsReady())
            {
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.R);
                fComboDamage += ObjectManager.Player.TotalAttackDamage * 3;
            }

            if (CommonItems.Youmuu.IsReady())
            {
                fComboDamage += ObjectManager.Player.TotalAttackDamage * 3;
            }

            if (Common.CommonSummoner.IgniteSlot != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(Common.CommonSummoner.IgniteSlot) == SpellState.Ready)
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
            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                ExecuteCombo();
            }

            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
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