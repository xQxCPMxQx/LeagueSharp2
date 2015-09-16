using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Mordekaiser.Events
{
    internal class Combo
    {
        private static bool isAttackingToTarget = false;
        public static float GhostAttackDelay = 1200;
        public Combo()
        {
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
        }
        private static Obj_AI_Hero GetTarget
        {
            get { return TargetSelector.GetTarget(Spells.R.Range, TargetSelector.DamageType.Physical); }
        }

        private static bool MordekaiserHaveSlave
        {
            get { return Utils.Player.Self.Spellbook.GetSpell(SpellSlot.R).Name == "mordekaisercotgguide"; }
        }

        public static Obj_AI_Base HowToTrainYourDragon
        {
            get
            {
                if (!MordekaiserHaveSlave)
                    return null;

                return
                    ObjectManager
                        .Get<Obj_AI_Base>(
                            ).FirstOrDefault(m => m.Distance(Utils.Player.Self.Position) < 15000 && !m.Name.Contains("inion") && m.IsAlly &&
                                m.HasBuff("mordekaisercotgpetbuff2"));
            }
        }


        private static void Game_OnUpdate(EventArgs args)
        {
            if (Utils.Player.Self.IsDead)
                return;

            if (Menu.MenuKeys.Item("Keys.Combo").GetValue<KeyBind>().Active)
            {
                ExecuteQ();
                ExecuteW();
                ExecuteE();
                ExecuteR();
                CastItems();

                var t = TargetSelector.GetTarget(4500, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (HowToTrainYourDragon != null)
                    {
                        var m = HowToTrainYourDragon;
                        if (!MordekaiserHaveSlave || !(Environment.TickCount >= GhostAttackDelay))
                        {
                            return;
                        }

                        var ghostOption = Menu.MenuGhost.Item("Ghost.Use").GetValue<StringList>().SelectedIndex;

                        switch (ghostOption)
                        {
                            case 1:
                                {
                                    t = TargetSelector.GetTarget(Utils.Player.AutoAttackRange * 2, TargetSelector.DamageType.Physical);
                                    Spells.R.Cast(t);
                                }
                                break;
                            case 2:
                                {
                                    t = TargetSelector.GetTarget(4500, TargetSelector.DamageType.Physical); 
                                    Spells.R.Cast(t);
                                }
                                break;
                        }
                        GhostAttackDelay = Environment.TickCount + m.AttackDelay * 1000;
                    }
                }
            }
        }

        private static void ExecuteQ()
        {
            if (!Spells.Q.IsReady())
                return;
            
            if (!Menu.MenuQ.Item("UseQ.Combo").GetValue<bool>())
            {
                return;
            }
            
            if (!isAttackingToTarget)
            {
                return;
            }

            var t = GetTarget;

            if (!t.IsValidTarget(Utils.Player.AutoAttackRange))
            {
                return;
            }

            Spells.Q.Cast(t);
        }

        private static void ExecuteW()
        {
            if (!Spells.W.IsReady() || Utils.Player.Self.Spellbook.GetSpell(SpellSlot.W).Name == "mordekaisercreepingdeath2")
                return;

            if (Utils.Player.Self.CountEnemiesInRange(Spells.WDamageRadius) > 0)
            {
                if (Menu.MenuW.Item("Selected" + Utils.Player.Self.ChampionName).GetValue<StringList>().SelectedIndex == 1)
                {
                    Spells.W.CastOnUnit(Utils.Player.Self);
                }
            }
            else
            {
                Spells.W.CastOnUnit(Utils.Player.Self);
            }

            var ghost = Utils.HowToTrainYourDragon;
            if (ghost != null)
            {
                if (ghost.CountEnemiesInRange(Spells.WDamageRadius) == 0)
                    return;

                if (Menu.MenuW.Item("SelectedGhost").GetValue<StringList>().SelectedIndex == 1)
                {
                    Spells.W.CastOnUnit(ghost);
                }
            }

            foreach (var ally in HeroManager.Allies.Where(
                a => !a.IsDead && !a.IsMe && a.Position.Distance(Utils.Player.Self.Position) < Spells.W.Range)
                .Where(ally => ally.CountEnemiesInRange(Spells.WDamageRadius) > 0)
                .Where(ally => Menu.MenuW.Item("Selected" + ally.ChampionName).GetValue<StringList>().SelectedIndex == 1)
                )
            {
                Spells.W.CastOnUnit(ally);
            }

        }

        private static void ExecuteE()
        {
            if (!Spells.E.IsReady())
            {
                return;
            }

            if (!Menu.MenuE.Item("UseE.Combo").GetValue<bool>())
            {
                return;
            }
            
            var t = GetTarget;

            if (!t.IsValidTarget(Spells.E.Range))
            {
                return;
            }

            Spells.E.Cast(t);
        }

        private static void ExecuteR()
        {
            if (!Menu.MenuR.Item("UseR.Active").GetValue<bool>())
                return;

            if (!Spells.R.IsReady()) 
                return;

            var t = TargetSelector.GetTarget(Spells.R.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget()
                && t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.R) * (100 / (100 + t.SpellBlock))) Spells.R.CastOnUnit(t);
        }

        private static void CastItems()
        {
            var t = TargetSelector.GetTarget(750, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            foreach (var item in Items.ItemDb)
            {
                if (item.Value.ItemType == Items.EnumItemType.AoE &&
                    item.Value.TargetingType == Items.EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady())
                        item.Value.Item.Cast();
                }
                if (item.Value.ItemType == Items.EnumItemType.Targeted &&
                    item.Value.TargetingType == Items.EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady())
                        item.Value.Item.Cast(t);
                }
            }
        }
        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Menu.MenuKeys.Item("Keys.Combo").GetValue<KeyBind>().Active)
            {
                var hero = args.Target as Obj_AI_Hero;
                isAttackingToTarget = hero != null;
            }
        }
    }
}
