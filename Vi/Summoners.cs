using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Vi
{
    internal class Summoners
    {
        private enum SmiteType
        {

            SmitePurple , SmiteGrey, SmiteRed, SmiteBlue
        };

        private class Smite
        {
            public string Name
            {
                get;
                set;
            }
            public int Damage
            {
                get;
                set;
            }
            public int[] ItemId
            {
                get;
                set;
            }
            public SmiteType TypeList
            {
                get;
                set;
            }
        }

        private List<Smite> _potions;

        public Summoners()
        {
            _potions = new List<Smite>
            {
                new Smite
                {
                    Name = "s5_summonersmiteplayerganker",
                    Damage = 1,
                    ItemId = new[] {3706, 3710, 3709, 3708, 3707},
                    TypeList = SmiteType.SmiteBlue
                },
                new Smite
                {
                    Name = "s5_summonersmiteduel",
                    Damage = 1,
                    ItemId = new[] {3715, 3718, 3717, 3716, 3714},
                    TypeList = SmiteType.SmiteRed
                },
                new Smite
                {
                    Name = "s5_summonersmitequick",
                    Damage = 1,
                    ItemId = new[] {3711, 3722, 3721, 3720, 3719},
                    TypeList = SmiteType.SmiteGrey
                },
                new Smite
                {
                    Name = "itemsmiteaoe",
                    Damage = 1,
                    ItemId = new[] {3713, 3723, 3725, 3726},
                    TypeList = SmiteType.SmitePurple
                },
            };
            Load();
        }

        private void Load()
        {
            _potions = _potions.OrderBy(x => x.Priority).ToList();
            Program.MenuExtras.AddSubMenu(new Menu("Potion Manager", "Summoners"));

            Program.MenuExtras.SubMenu("Summoners").AddSubMenu(new Menu("Health", "Health"));
            Program.MenuExtras.SubMenu("Summoners").SubMenu("Health").AddItem(new MenuItem("HealthPotion", "Use Health Potion").SetValue(true));
            Program.MenuExtras.SubMenu("Summoners").SubMenu("Health").AddItem(new MenuItem("HealthPercent", "HP Trigger Percent").SetValue(new Slider(30)));

            Program.MenuExtras.SubMenu("Summoners").AddSubMenu(new Menu("Mana", "Mana"));
            Program.MenuExtras.SubMenu("Summoners").SubMenu("Mana").AddItem(new MenuItem("ManaPotion", "Use Mana Potion").SetValue(true));
            Program.MenuExtras.SubMenu("Summoners").SubMenu("Mana").AddItem(new MenuItem("ManaPercent", "MP Trigger Percent").SetValue(new Slider(30)));

            Game.OnGameUpdate += OnGameUpdate;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Utility.InShopRange()) return;
            try
            {
                if (Program.MenuExtras.Item("HealthPotion").GetValue<bool>())
                {
                    if (GetPlayerHealthPercentage() <= Program.MenuExtras.Item("HealthPercent").GetValue<Slider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (!IsBuffActive(PotionType.Health))
                            healthSlot.UseItem();
                    }
                }
                if (Program.MenuExtras.Item("ManaPotion").GetValue<bool>())
                {
                    if (GetPlayerManaPercentage() <= Program.MenuExtras.Item("ManaPercent").GetValue<Slider>().Value)
                    {
                        var manaSlot = GetPotionSlot(PotionType.Mana);
                        if (!IsBuffActive(PotionType.Mana))
                            manaSlot.UseItem();
                    }
                }
            }

            catch (Exception)
            {

            }
        }

        private InventorySlot GetPotionSlot(PotionType type)
        {
            return (from potion in _potions
                    where potion.TypeList.Contains(type)
                    from item in ObjectManager.Player.InventoryItems
                    where item.Id == potion.ItemId && item.Charges >= potion.MinCharges
                    select item).FirstOrDefault();
        }

        private bool IsBuffActive(PotionType type)
        {
            return (from potion in _potions
                    where potion.TypeList.Contains(type)
                    from buff in ObjectManager.Player.Buffs
                    where buff.Name == potion.Name && buff.IsActive
                    select potion).Any();
        }

        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }

        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }
    }
}