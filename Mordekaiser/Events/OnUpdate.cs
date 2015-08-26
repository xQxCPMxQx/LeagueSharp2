using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Mordekaiser.Events
{
    internal class OnUpdate
    {
        private static float wHitRange = 450f;

        public OnUpdate()
        {
            Game.OnUpdate += Game_OnUpdate;
        }

        private static Obj_AI_Hero Player
        {
            get
            {
                return Utils.Player.Self;
            }
        }

        private static Spell W
        {
            get
            {
                return Spells.W;
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            //if (Utils.HowToTrainYourDragon != null || !(Environment.TickCount >= Combo.GhostAttackDelay))
            //{
            //    var ghost = Utils.HowToTrainYourDragon;
            //    if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && ghost.Distance(Utils.Player.Self.Position) > Utils.Player.AutoAttackRange)
            //    {
            //        Spells.R.Cast(Utils.Player.Self.Position);
            //        Combo.GhostAttackDelay = Environment.TickCount;
            //    }
             
            //    if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Utils.Player.Self.CountEnemiesInRange(2500) == 0 && ghost.Distance(Utils.Player.Self.Position) > Utils.Player.AutoAttackRange * 2)
            //    {
            //        Spells.R.Cast(Utils.Player.Self.Position);
            //        Combo.GhostAttackDelay = Environment.TickCount;
            //    }
            //}

            ExecuteLogicW();

        }

        private static void ExecuteLogicW()
        {
            if (!W.IsReady() || Player.Spellbook.GetSpell(SpellSlot.W).Name == "mordekaisercreepingdeath2")
                return;

            if (Player.CountEnemiesInRange(wHitRange) > 0)
            {
                if (Menu.MenuW.Item("Selected" + Player.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                {
                    W.CastOnUnit(Utils.Player.Self);
                }
            }

            var ghost = Utils.HowToTrainYourDragon;
            if (ghost != null)
            {
                if (ghost.CountEnemiesInRange(wHitRange) == 0)
                    return;

                if (Menu.MenuW.Item("SelectedGhost").GetValue<StringList>().SelectedIndex == 2)
                {
                    W.CastOnUnit(ghost);
                }
            }

            foreach (var ally in HeroManager.Allies.Where(
                a => !a.IsDead && !a.IsMe && a.Position.Distance(Player.Position) < W.Range)
                .Where(ally => ally.CountEnemiesInRange(wHitRange) > 0)
                .Where(ally => Menu.MenuW.Item("Selected" + ally.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                )
            {
                W.CastOnUnit(ally);
            }

            //if (Menu.MenuKeys.Item("Keys.Lane").GetValue<KeyBind>().Active)
            //{
            //    if (!Menu.MenuW.Item("UseW.Lane").GetValue<bool>())
            //        return;

            //    var minionsW = MinionManager.GetMinions(Player.Position, wHitRange);
            //    if (minionsW.Count > 1)
            //    {
            //        W.CastOnUnit(Utils.Player.Self);
            //    }
            //}

            //if (Menu.MenuKeys.Item("Keys.Jungle").GetValue<KeyBind>().Active)
            //{
            //    if (!Menu.MenuW.Item("UseW.Jungle").GetValue<bool>())
            //        return;

            //    var minionsW = MinionManager.GetMinions(Player.Position, wHitRange, MinionTypes.All, MinionTeam.Neutral);
            //    if (minionsW.Count > 0)
            //    {
            //        W.CastOnUnit(Utils.Player.Self);
            //    }
            //}
        }

    }
}
