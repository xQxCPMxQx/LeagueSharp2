#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Items.cs is part of SFXUtility.

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
using SFXUtility.Library.Logger;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXUtility.Data
{
    internal class CustomItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public LeagueSharp.Common.Items.Item Item { get; set; }
        public bool Target { get; set; }
        public Damage.DamageItems Damage { get; set; }
        public float Range { get; set; }
        public float Delay { get; set; }
        public float Radius { get; set; }
        public float Speed { get; set; }
    }

    internal class Items
    {
        public static CustomItem Hydra;
        public static CustomItem BilgewaterCutlass;
        public static CustomItem BladeRuinedKing;
        public static CustomItem HextechGunblade;
        public static List<CustomItem> CustomItems;

        static Items()
        {
            try
            {
                // AOE damage, only melee
                Hydra = new CustomItem
                {
                    Item = ItemData.Ravenous_Hydra_Melee_Only.GetItem(),
                    Damage = Damage.DamageItems.Hydra,
                    Target = false,
                    Range = ItemData.Ravenous_Hydra_Melee_Only.GetItem().Range
                };

                // Slow + Damage
                BilgewaterCutlass = new CustomItem
                {
                    Item = ItemData.Bilgewater_Cutlass.GetItem(),
                    Damage = Damage.DamageItems.Bilgewater,
                    Target = true,
                    Range = ItemData.Bilgewater_Cutlass.GetItem().Range
                };

                // Slow + Damage
                BladeRuinedKing = new CustomItem
                {
                    Item = ItemData.Blade_of_the_Ruined_King.GetItem(),
                    Damage = Damage.DamageItems.Botrk,
                    Target = true,
                    Range = ItemData.Blade_of_the_Ruined_King.GetItem().Range
                };

                // Damage + Slow
                HextechGunblade = new CustomItem
                {
                    Item = ItemData.Hextech_Gunblade.GetItem(),
                    Damage = Damage.DamageItems.Hexgun,
                    Target = true,
                    Range = ItemData.Hextech_Gunblade.GetItem().Range
                };

                CustomItems = new List<CustomItem> { Hydra, BilgewaterCutlass, BladeRuinedKing, HextechGunblade };
            }
            catch (Exception ex)
            {
                CustomItems = new List<CustomItem>();
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float CalculateComboDamage(Obj_AI_Hero target)
        {
            try
            {
                return
                    (float)
                        CustomItems.Where(
                            i =>
                                i.Item.IsOwned() && i.Item.IsReady() &&
                                target.Distance(ObjectManager.Player.Position) <= i.Range)
                            .Sum(item => ObjectManager.Player.GetItemDamage(target, item.Damage));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public static void UseComboItems(Obj_AI_Hero target)
        {
            try
            {
                foreach (var item in
                    CustomItems.Where(
                        i =>
                            i.Item.IsOwned() && i.Item.IsReady() &&
                            target.Distance(ObjectManager.Player.Position) <= i.Range))
                {
                    if (item.Target)
                    {
                        item.Item.Cast(target);
                    }
                    else
                    {
                        item.Item.Cast();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}