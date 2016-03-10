using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Shen.Common
{
    internal class ItemManager
    {
        public static Dictionary<string, Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>> ItemDb;

        internal struct Tuple<TA, TB, TC> : IEquatable<Tuple<TA, TB, TC>>
        {
            private readonly TA item;
            private readonly TB itemType;
            private readonly TC targetingType;

            public Tuple(TA pItem, TB pItemType, TC pTargetingType)
            {
                this.item = pItem;
                this.itemType = pItemType;
                this.targetingType = pTargetingType;
            }

            public TA Item => this.item;

            public TB ItemType => this.itemType;

            public TC TargetingType => this.targetingType;

            public override int GetHashCode()
            {
                return this.item.GetHashCode() ^ this.itemType.GetHashCode() ^ this.targetingType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                return this.Equals((Tuple<TA, TB, TC>)obj);
            }

            public bool Equals(Tuple<TA, TB, TC> other)
            {
                return other.item.Equals(item) && other.itemType.Equals(this.itemType)
                       && other.targetingType.Equals(this.targetingType);
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

        public static void Initialize()
        {
            LoadItems();
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
        }
        
        private static void LoadItems()
        {
            ItemDb =
                new Dictionary<string, Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>>
                {
                    {
                        "Tiamat",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3077, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                    },
                    {
                        "Bilge",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3144, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Blade",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3153, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Hydra",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3074, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                    },
                    {
                        "Titanic Hydra Cleave",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3748, Orbwalking.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.OnTarget,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Randiun",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3143, 490f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Hextech",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3146, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Entropy",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3184, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Youmuu's Ghostblade",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3142, Orbwalking.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    },
                    {
                        "Sword of the Divine",
                        new Tuple<LeagueSharp.Common.Items.Item, EnumItemType, EnumItemTargettingType>(
                            new LeagueSharp.Common.Items.Item(3131, Orbwalking.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                    }
                };

        }
        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                foreach (var item in
                    ItemDb.Where(
                        i =>
                            i.Value.ItemType == EnumItemType.OnTarget
                            && i.Value.TargetingType == EnumItemTargettingType.EnemyHero && i.Value.Item.IsReady()))
                {
                    item.Value.Item.Cast();
                }
            }
        }
    }
}
