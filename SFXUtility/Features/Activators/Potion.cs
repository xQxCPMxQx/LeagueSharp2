#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Potion.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Logger;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXUtility.Features.Activators
{
    internal class Potion : Child<Activators>
    {
        private readonly List<PotionStruct> _potions = new List<PotionStruct>
        {
            new PotionStruct("ItemCrystalFlask", (ItemId) ItemData.Refillable_Potion.Id, 1, new[] { PotionType.Health }),
            new PotionStruct(
                "ItemCrystalFlaskJungle", (ItemId) ItemData.Hunters_Potion.Id, 1,
                new[] { PotionType.Health, PotionType.Mana }),
            new PotionStruct(
                "ItemDarkCrystalFlask", (ItemId) ItemData.Corrupting_Potion.Id, 1,
                new[] { PotionType.Health, PotionType.Mana }),
            new PotionStruct("RegenerationPotion", (ItemId) ItemData.Health_Potion.Id, 0, new[] { PotionType.Health }),
            new PotionStruct(
                "ItemMiniRegenPotion", (ItemId) ItemData.Total_Biscuit_of_Rejuvenation.Id, 0,
                new[] { PotionType.Health }),
            new PotionStruct(
                "ItemMiniRegenPotion", (ItemId) ItemData.Total_Biscuit_of_Rejuvenation2.Id, 0,
                new[] { PotionType.Health })
        };

        public Potion(Activators parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Potion"; }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var healthMenu = new Menu("Health", Name + "Health");
                healthMenu.AddItem(new MenuItem(healthMenu.Name + "Percent", "Health Percent").SetValue(new Slider(60)));
                healthMenu.AddItem(new MenuItem(healthMenu.Name + "Enabled", "Enabled").SetValue(false));

                var manaMenu = new Menu("Mana", Name + "Mana");
                manaMenu.AddItem(new MenuItem(manaMenu.Name + "Percent", "Mana Percent").SetValue(new Slider(60)));
                manaMenu.AddItem(new MenuItem(manaMenu.Name + "Enabled", "Enabled").SetValue(false));

                Menu.AddSubMenu(healthMenu);
                Menu.AddSubMenu(manaMenu);

                Menu.AddItem(
                    new MenuItem(Name + "MaxEnemyDistance", "Max Enemy Distance").SetValue(new Slider(1000, 0, 1500)));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
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
            return
                _potions.Where(potion => potion.TypeList.Contains(type))
                    .Any(
                        potion =>
                            ObjectManager.Player.Buffs.Any(
                                b => b.Name.Equals(potion.BuffName, StringComparison.OrdinalIgnoreCase)));
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.IsDead || ObjectManager.Player.InFountain() ||
                    ObjectManager.Player.Buffs.Any(
                        b =>
                            b.Name.Contains("Recall", StringComparison.OrdinalIgnoreCase) ||
                            b.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var enemyDist = Menu.Item(Name + "MaxEnemyDistance").GetValue<Slider>().Value;
                if (enemyDist != 0 &&
                    !GameObjects.EnemyHeroes.Any(e => e.Position.Distance(ObjectManager.Player.Position) <= enemyDist))
                {
                    return;
                }

                if (Menu.Item(Name + "HealthEnabled").GetValue<bool>())
                {
                    if (ObjectManager.Player.HealthPercent <= Menu.Item(Name + "HealthPercent").GetValue<Slider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (healthSlot != null && !IsBuffActive(PotionType.Health))
                        {
                            ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                        }
                    }
                }

                if (Menu.Item(Name + "ManaEnabled").GetValue<bool>())
                {
                    if (ObjectManager.Player.ManaPercent <= Menu.Item(Name + "ManaPercent").GetValue<Slider>().Value)
                    {
                        var manaSlot = GetPotionSlot(PotionType.Mana);
                        if (manaSlot != null && !IsBuffActive(PotionType.Mana))
                        {
                            ObjectManager.Player.Spellbook.CastSpell(manaSlot.SpellSlot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private enum PotionType
        {
            Health,
            Mana
        }

        private struct PotionStruct
        {
            public readonly string BuffName;
            public readonly ItemId ItemId;
            public readonly int MinCharges;
            public readonly PotionType[] TypeList;

            public PotionStruct(string buffName, ItemId itemId, int minCharges, PotionType[] typeList)
            {
                BuffName = buffName;
                ItemId = itemId;
                MinCharges = minCharges;
                TypeList = typeList;
            }
        }
    }
}