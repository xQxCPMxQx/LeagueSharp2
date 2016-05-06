using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Champion;

namespace Nocturne.Common
{
    internal class CommonItems
    {
        public static Items.Item Youmuu = new Items.Item(3142, 225f);
        public static Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>> ItemDb;

        public struct Tuple<TA, TB, TC> : IEquatable<Tuple<TA, TB, TC>>
        {
            private readonly TA item;
            private readonly TB itemType;
            private readonly TC targetingType;

            public Tuple(TA pItem, TB pItemType, TC pTargetingType)
            {
                item = pItem;
                itemType = pItemType;
                targetingType = pTargetingType;
            }

            public TA Item => item;
            public TB ItemType => itemType;
            public TC TargetingType => targetingType;

            public override int GetHashCode()
            {
                return item.GetHashCode() ^ itemType.GetHashCode() ^ targetingType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                return Equals((Tuple<TA, TB, TC>) obj);
            }

            public bool Equals(Tuple<TA, TB, TC> other)
            {
                return other.item.Equals(item) && other.itemType.Equals(itemType) &&
                       other.targetingType.Equals(targetingType);
            }
        }

        public enum EnumItemType
        {
            OnTarget,
            Targeted,
            AoE
        }

        public enum EnumItemTargettingType
        {
            Ally,
            EnemyHero,
            EnemyObjects
        }

        internal enum Mobs
        {
            Blue = 1,
            Red = 2,
            Dragon = 1,
            Baron = 2,
            All = 3
        }

        public static void Load()
        {
            ItemDb =
                new Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>>
                {
                    {
                        "Tiamat",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3077, 450f),
                            EnumItemType.AoE, EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Bilge",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3144, 450f),
                            EnumItemType.Targeted, EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Blade",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3153, 450f),
                            EnumItemType.Targeted, EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Hydra",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3074, 450f),
                            EnumItemType.AoE, EnumItemTargettingType.EnemyObjects)
                    },
                    {
                        "Titanic Hydra Cleave",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3748, Orbwalking.GetRealAutoAttackRange(null) + 65), EnumItemType.OnTarget,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Randiun",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3143, 490f),
                            EnumItemType.AoE, EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Hextech",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3146, 750f),
                            EnumItemType.Targeted, EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Entropy",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(new Items.Item(3184, 750f),
                            EnumItemType.Targeted, EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Youmuu's Ghostblade",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3142, Orbwalking.GetRealAutoAttackRange(null) + 65), EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Sword of the Divine",
                        new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3131, Orbwalking.GetRealAutoAttackRange(null) + 65), EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    }
                };
        }

        public static void Init()
        {
            Load();
            Game.OnUpdate += GameOnOnUpdate;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
        }

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero && Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                foreach (
                    var item in
                        ItemDb.Where(
                            i =>
                                i.Value.ItemType == EnumItemType.OnTarget
                                && i.Value.TargetingType == EnumItemTargettingType.EnemyHero
                                && i.Value.Item.IsReady())
                    )
                {
                    item.Value.Item.Cast();
                }

                foreach (
                  var item in
                      ItemDb.Where(
                          i =>
                              i.Value.ItemType == EnumItemType.Targeted
                              && i.Value.TargetingType == EnumItemTargettingType.EnemyHero
                              && i.Value.Item.IsReady())
                  )
                {
                    item.Value.Item.Cast();
                }
            }
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            ExecuteComboMode();
            ExecuteLaneMode();
            ExecuteJungleMode();
        }

        private static void ExecuteComboMode()
        {
            if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                
                var t = CommonTargetSelector.GetTarget(PlayerSpells.Q.Range);
                if (!t.IsValidTarget())
                {
                    return;
                }

                foreach (
                    var item in
                        ItemDb.Where(
                            item =>
                                item.Value.ItemType == EnumItemType.AoE &&
                                (item.Value.TargetingType == EnumItemTargettingType.EnemyObjects || item.Value.TargetingType == EnumItemTargettingType.EnemyHero))
                            .Where(item => t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()))
                {
                    item.Value.Item.Cast();
                }

                foreach (
             var item in
                 ItemDb.Where(
                     item =>
                         item.Value.ItemType == EnumItemType.Targeted &&
                         (item.Value.TargetingType == EnumItemTargettingType.EnemyHero))
                     .Where(item => t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()))
                {
                    item.Value.Item.Cast(t);
                }

            }
        }

        private static void ExecuteLaneMode()
        {
            if (Modes.ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
            {
                return;
            }

            if (Modes.ModeLane.MenuLocal.Item("Lane.Item").GetValue<StringList>().SelectedIndex == 1 ||
                ObjectManager.Player.UnderAllyTurret())
            {
                foreach (var item in from item in ItemDb
                    where
                        item.Value.ItemType == EnumItemType.AoE
                        && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                    let iMinions =
                        MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                            Orbwalking.GetRealAutoAttackRange(null) + 65, MinionTypes.All, MinionTeam.Enemy,
                            MinionOrderTypes.MaxHealth)
                    where
                        item.Value.Item.IsReady() && iMinions.Count() >= 3
                    select item)
                {
                    item.Value.Item.Cast();
                }
            }
        }

        private static void ExecuteJungleMode()
        {
            if (Modes.ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
            {
                return;
            }

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Orbwalking.GetRealAutoAttackRange(null) + 65, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
            {
                return;
            }

            var mob = mobs[0];

            if (Modes.ModeJungle.MenuLocal.Item("Jungle.Item").GetValue<StringList>().SelectedIndex == 1)
            {
                foreach (var item in from item in ItemDb
                    where
                        item.Value.ItemType == EnumItemType.AoE
                        && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                    let iMinions =
                        MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                            Orbwalking.GetRealAutoAttackRange(null) + 65, MinionTypes.All, MinionTeam.Neutral,
                            MinionOrderTypes.MaxHealth)
                    where
                        item.Value.Item.IsReady() &&
                        (iMinions.Count() >= 2 || CommonManaManager.GetMobType(iMinions[0]) == CommonManaManager.MobTypes.Blue ||
                         CommonManaManager.GetMobType(iMinions[0]) == CommonManaManager.MobTypes.Red ||
                         CommonManaManager.GetMobType(iMinions[0]) == CommonManaManager.MobTypes.Baron ||
                         CommonManaManager.GetMobType(iMinions[0]) == CommonManaManager.MobTypes.Dragon)
                    select item)
                {
                    item.Value.Item.Cast();
                }
            }


            if (Youmuu.IsReady() && mob.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
            {
                var youmuuBaron = Modes.ModeJungle.MenuLocal.Item("Jungle.Youmuu.BaronDragon").GetValue<StringList>().SelectedIndex;
                var youmuuRed = Modes.ModeJungle.MenuLocal.Item("Jungle.Youmuu.BlueRed").GetValue<StringList>().SelectedIndex;

                if (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Dragon && (youmuuBaron == 1 || youmuuBaron == 3))
                {
                    Youmuu.Cast();
                }

                if (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Baron && (youmuuBaron == 2 || youmuuBaron == 3))
                {
                    Youmuu.Cast();
                }

                if (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Red && (youmuuRed == 1 || youmuuRed == 3))
                {
                    Youmuu.Cast();
                }

                if (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Blue && (youmuuRed == 2 || youmuuRed == 3))
                {
                    Youmuu.Cast();
                }

                //if ((CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Dragon && (youmuuBaron == 1 || youmuuBaron == 3))
                //    ||
                //    (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Baron && (youmuuBaron == 2 || youmuuBaron == 3))
                //    || (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Red && (youmuuRed == 1 || youmuuBaron == 3))
                //    ||
                //    (CommonManaManager.GetMobType(mob) == CommonManaManager.MobTypes.Blue && (youmuuRed == 2 || youmuuBaron == 3)))
                //{
                //    Youmuu.Cast();
                //}
            }
        }
    }
}