#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ItemManager.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Enumerations;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.SFXTargetSelector.Others;
using ItemData = LeagueSharp.Common.Data.ItemData;
using Orbwalking = SFXChallenger.SFXTargetSelector.Orbwalking;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Managers
{
    public class CustomItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Items.Item Item { get; set; }
        public ItemFlags Flags { get; set; }
        public CombatFlags CombatFlags { get; set; }
        public CastType CastType { get; set; }
        public EffectFlags EffectFlags { get; set; }
        public Damage.DamageItems Damage { get; set; }
        public float Range { get; set; }
        public float Delay { get; set; }
        public float Radius { get; set; }
        public float Speed { get; set; }
    }

    public class ItemManager
    {
        private static Menu _menu;
        private static ItemFlags _itemFlags;
        public static CustomItem Youmuu;
        public static CustomItem Hydra;
        public static CustomItem BilgewaterCutlass;
        public static CustomItem BladeRuinedKing;
        public static CustomItem HextechGunblade;
        public static CustomItem MikaelsCrucible;
        public static CustomItem LocketIronSolari;
        public static CustomItem FrostQueensClaim;
        public static CustomItem TalismanOfAscension;
        public static CustomItem FaceOfTheMountain;
        public static CustomItem Sightstone;
        public static CustomItem RubySightstone;
        public static CustomItem EyeOfTheWatchers;
        public static CustomItem EyeOfTheEquinox;
        public static CustomItem EyeOfTheOasis;
        public static CustomItem TrackersKnife;
        public static List<CustomItem> Items;

        static ItemManager()
        {
            try
            {
                // Speed + Atk Speed
                Youmuu = new CustomItem
                {
                    Name = "youmuus-ghostblade",
                    DisplayName = "Youmuu's Ghostblade",
                    Item = ItemData.Youmuus_Ghostblade.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.AttackSpeed | EffectFlags.MovementSpeed,
                    CastType = CastType.None,
                    Range =
                        ObjectManager.Player.IsMelee
                            ? ObjectManager.Player.AttackRange * 3
                            : Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)
                };

                // AOE damage, only melee
                Hydra = new CustomItem
                {
                    Name = "hydra",
                    DisplayName = "Hydra",
                    Item = ItemData.Ravenous_Hydra_Melee_Only.GetItem(),
                    Flags = ItemFlags.Offensive,
                    CombatFlags = CombatFlags.Melee,
                    EffectFlags = EffectFlags.Damage,
                    CastType = CastType.None,
                    Damage = Damage.DamageItems.Hydra,
                    Range = ItemData.Ravenous_Hydra_Melee_Only.GetItem().Range
                };

                // Slow + Damage
                BilgewaterCutlass = new CustomItem
                {
                    Name = "bilgewater-cutlass",
                    DisplayName = "Bilgewater Cutlass",
                    Item = ItemData.Bilgewater_Cutlass.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Damage | EffectFlags.MovementSlow,
                    CastType = CastType.Target,
                    Damage = Damage.DamageItems.Bilgewater,
                    Range = ItemData.Bilgewater_Cutlass.GetItem().Range
                };

                // Slow + Damage
                BladeRuinedKing = new CustomItem
                {
                    Name = "blade-ruined-king",
                    DisplayName = "Blade of the Ruined King",
                    Item = ItemData.Blade_of_the_Ruined_King.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Damage | EffectFlags.MovementSlow,
                    CastType = CastType.Target,
                    Damage = Damage.DamageItems.Botrk,
                    Range = ItemData.Blade_of_the_Ruined_King.GetItem().Range
                };

                // Damage + Slow
                HextechGunblade = new CustomItem
                {
                    Name = "hextech-gunblade",
                    DisplayName = "Hextech Gunblade",
                    Item = ItemData.Hextech_Gunblade.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Damage | EffectFlags.MovementSlow,
                    CastType = CastType.Target,
                    Damage = Damage.DamageItems.Hexgun,
                    Range = ItemData.Hextech_Gunblade.GetItem().Range
                };

                // AOE Shield
                LocketIronSolari = new CustomItem
                {
                    Name = "locket-iron-solari",
                    DisplayName = "Locket of the Iron Solari",
                    Item = ItemData.Locket_of_the_Iron_Solari.GetItem(),
                    Flags = ItemFlags.Supportive | ItemFlags.Defensive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Shield,
                    CastType = CastType.None,
                    Range = ItemData.Locket_of_the_Iron_Solari.GetItem().Range
                };

                // Remove stun + heal
                MikaelsCrucible = new CustomItem
                {
                    Name = "mikaels-crucible",
                    DisplayName = "Mikael's Crucible",
                    Item = ItemData.Mikaels_Crucible.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.RemoveStun | EffectFlags.Heal,
                    CastType = CastType.Target,
                    Range = ItemData.Mikaels_Crucible.GetItem().Range
                };


                // Slow
                FrostQueensClaim = new CustomItem
                {
                    Name = "frost-queens-claim",
                    DisplayName = "Frost Queen's Claim",
                    Item = ItemData.Frost_Queens_Claim.GetItem(),
                    Flags = ItemFlags.Supportive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.MovementSlow,
                    CastType = CastType.None,
                    Range = ItemData.Frost_Queens_Claim.GetItem().Range
                };

                // Speed
                TalismanOfAscension = new CustomItem
                {
                    Name = "talisman-of-ascension",
                    DisplayName = "Talisman of Ascension",
                    Item = ItemData.Talisman_of_Ascension.GetItem(),
                    Flags = ItemFlags.Supportive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.MovementSpeed,
                    CastType = CastType.None,
                    Range = ItemData.Talisman_of_Ascension.GetItem().Range
                };

                // Shield
                FaceOfTheMountain = new CustomItem
                {
                    Name = "face-of-the-mountain",
                    DisplayName = "Face of the Mountain",
                    Item = ItemData.Face_of_the_Mountain.GetItem(),
                    Flags = ItemFlags.Supportive | ItemFlags.Flee | ItemFlags.Defensive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Shield,
                    CastType = CastType.Self,
                    Range = ItemData.Face_of_the_Mountain.GetItem().Range
                };

                // Place wards
                Sightstone = new CustomItem
                {
                    Name = "sightstone",
                    DisplayName = "Sightstone",
                    Item = ItemData.Sightstone.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Sightstone.GetItem().Range
                };

                // Place wards
                RubySightstone = new CustomItem
                {
                    Name = "ruby-sightstone",
                    DisplayName = "Ruby Sightstone",
                    Item = ItemData.Ruby_Sightstone.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Ruby_Sightstone.GetItem().Range
                };

                // Place wards
                EyeOfTheWatchers = new CustomItem
                {
                    Name = "eye-of-the-watchers",
                    DisplayName = "Eye of the Watchers",
                    Item = ItemData.Eye_of_the_Watchers.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Eye_of_the_Watchers.GetItem().Range
                };

                // Place wards
                EyeOfTheEquinox = new CustomItem
                {
                    Name = "eye-of-the-equinox",
                    DisplayName = "Eye of the Equinox",
                    Item = ItemData.Eye_of_the_Equinox.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Eye_of_the_Equinox.GetItem().Range
                };

                // Place wards
                EyeOfTheOasis = new CustomItem
                {
                    Name = "eye-of-the-oasis",
                    DisplayName = "Eye of the Oasis",
                    Item = ItemData.Eye_of_the_Oasis.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Eye_of_the_Oasis.GetItem().Range
                };

                // Place wards
                TrackersKnife = new CustomItem
                {
                    Name = "trackers-knife",
                    DisplayName = "Tracker's Knife",
                    Item = ItemData.Trackers_Knife.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Trackers_Knife.GetItem().Range
                };

                Items = new List<CustomItem>
                {
                    Youmuu,
                    Hydra,
                    BilgewaterCutlass,
                    BladeRuinedKing,
                    HextechGunblade,
                    MikaelsCrucible,
                    LocketIronSolari,
                    FrostQueensClaim,
                    TalismanOfAscension,
                    FaceOfTheMountain,
                    Sightstone,
                    RubySightstone,
                    EyeOfTheWatchers,
                    EyeOfTheEquinox,
                    EyeOfTheOasis,
                    TrackersKnife
                };

                MaxRange = Items.Max(s => s.Range);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float MaxRange { get; set; }

        public static void AddToMenu(Menu menu, ItemFlags itemFlags)
        {
            try
            {
                _menu = menu;
                _itemFlags = itemFlags;

                foreach (var item in
                    Items.Where(
                        i =>
                            i.CombatFlags.HasFlag(ObjectManager.Player.IsMelee ? CombatFlags.Melee : CombatFlags.Ranged) &&
                            ((i.Flags & _itemFlags) != 0)))
                {
                    if (item.Flags.HasFlag(ItemFlags.Offensive) || item.Flags.HasFlag(ItemFlags.Flee))
                    {
                        var itemMenu = _menu.AddSubMenu(new Menu(item.DisplayName, _menu.Name + "." + item.Name));

                        itemMenu.AddItem(
                            new MenuItem(itemMenu.Name + ".min-enemies-range", "Min. Enemies in Range").SetValue(
                                new Slider(1, 0, 5)));
                        itemMenu.AddItem(
                            new MenuItem(itemMenu.Name + ".player-health-below", "Player Health % <=").SetValue(
                                new Slider(100)));
                        itemMenu.AddItem(
                            new MenuItem(itemMenu.Name + ".player-health-above", "Player Health % >=").SetValue(
                                new Slider(0)));
                        itemMenu.AddItem(
                            new MenuItem(itemMenu.Name + ".target-health-below", "Target Health % <=").SetValue(
                                new Slider(90)));
                        itemMenu.AddItem(
                            new MenuItem(itemMenu.Name + ".target-health-above", "Target Health % >=").SetValue(
                                new Slider(0)));

                        if (item.Flags.HasFlag(ItemFlags.Flee))
                        {
                            itemMenu.AddItem(new MenuItem(itemMenu.Name + ".flee", "Use Flee").SetValue(true));
                        }
                        if (item.Flags.HasFlag(ItemFlags.Offensive))
                        {
                            itemMenu.AddItem(new MenuItem(itemMenu.Name + ".combo", "Use Combo").SetValue(true));
                        }
                    }
                }

                var muramanaMenu = _menu.AddSubMenu(new Menu("Muramana", _menu.Name + ".muramana"));
                muramanaMenu.AddItem(
                    new MenuItem(muramanaMenu.Name + ".min-enemies-range", "Min. Enemies in Range").SetValue(
                        new Slider(1, 0, 5)));
                muramanaMenu.AddItem(
                    new MenuItem(muramanaMenu.Name + ".player-mana-above", "Player Mana % >=").SetValue(new Slider(30)));
                muramanaMenu.AddItem(
                    new MenuItem(muramanaMenu.Name + ".player-health-below", "Player Health % <=").SetValue(
                        new Slider(100)));
                muramanaMenu.AddItem(
                    new MenuItem(muramanaMenu.Name + ".player-health-above", "Player Health % >=").SetValue(
                        new Slider(0)));
                muramanaMenu.AddItem(
                    new MenuItem(muramanaMenu.Name + ".target-health-below", "Target Health % <=").SetValue(
                        new Slider(100)));
                muramanaMenu.AddItem(
                    new MenuItem(muramanaMenu.Name + ".target-health-above", "Target Health % >=").SetValue(
                        new Slider(0)));

                muramanaMenu.AddItem(new MenuItem(muramanaMenu.Name + ".combo", "Use Combo").SetValue(true));

                menu.AddItem(new MenuItem(menu.Name + ".enabled", "Enabled").SetValue(false));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float CalculateComboDamage(Obj_AI_Hero target, bool rangeCheck = true)
        {
            if (target == null)
            {
                return 0;
            }
            if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
            {
                return CalculateLichBaneDamage(target);
            }
            try
            {
                var distance = target.Distance(ObjectManager.Player.Position, true);
                if (rangeCheck && distance >= Math.Pow(MaxRange, 2))
                {
                    return 0f;
                }
                return
                    (float)
                        Items.Where(
                            i =>
                                i.EffectFlags.HasFlag(EffectFlags.Damage) && ((i.Flags & _itemFlags) != 0) &&
                                _menu.Item(_menu.Name + "." + i.Name + ".combo").GetValue<bool>() && i.Item.IsOwned() &&
                                i.Item.IsReady() &&
                                (!rangeCheck ||
                                 distance <= Math.Pow(i.Range, 2) &&
                                 ObjectManager.Player.CountEnemiesInRange(i.Range) >=
                                 _menu.Item(_menu.Name + "." + i.Name + ".min-enemies-range").GetValue<Slider>().Value))
                            .Sum(item => ObjectManager.Player.GetItemDamage(target, item.Damage)) +
                    CalculateLichBaneDamage(target);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public static void UseComboItems(Obj_AI_Hero target, bool killSteal = false)
        {
            if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
            {
                return;
            }
            try
            {
                var distance = target == null ? 0 : target.Distance(ObjectManager.Player.Position, true);
                if (distance >= Math.Pow(MaxRange, 2))
                {
                    return;
                }

                foreach (var item in
                    Items.Where(
                        i =>
                            ((i.Flags & _itemFlags) != 0) &&
                            _menu.Item(_menu.Name + "." + i.Name + ".combo").GetValue<bool>() && i.Item.IsOwned() &&
                            i.Item.IsReady() && distance <= Math.Pow(i.Range, 2) &&
                            (killSteal ||
                             ObjectManager.Player.CountEnemiesInRange(i.Range) >=
                             _menu.Item(_menu.Name + "." + i.Name + ".min-enemies-range").GetValue<Slider>().Value &&
                             ObjectManager.Player.HealthPercent <=
                             _menu.Item(_menu.Name + "." + i.Name + ".player-health-below").GetValue<Slider>().Value &&
                             ObjectManager.Player.HealthPercent >=
                             _menu.Item(_menu.Name + "." + i.Name + ".player-health-above").GetValue<Slider>().Value &&
                             (target == null ||
                              target.HealthPercent <=
                              _menu.Item(_menu.Name + "." + i.Name + ".target-health-below").GetValue<Slider>().Value &&
                              target.HealthPercent >=
                              _menu.Item(_menu.Name + "." + i.Name + ".target-health-above").GetValue<Slider>().Value)))
                    )
                {
                    switch (item.CastType)
                    {
                        case CastType.Target:
                            item.Item.Cast(target);
                            break;
                        case CastType.Self:
                            item.Item.Cast(ObjectManager.Player);
                            break;
                        case CastType.None:
                            item.Item.Cast();
                            break;
                        case CastType.Position:
                            var prediction = Prediction.GetPrediction(target, item.Delay, item.Radius, item.Speed);
                            if (prediction.Hitchance >= HitChance.Medium)
                            {
                                item.Item.Cast(prediction.CastPosition);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void Muramana(Obj_AI_Hero target, bool activate, float overrideRange = -1f)
        {
            try
            {
                var muramana = ObjectManager.Player.GetSpellSlot("Muramana");
                if (muramana == SpellSlot.Unknown || !muramana.IsReady())
                {
                    return;
                }
                var hasBuff = ObjectManager.Player.HasBuff("Muramana");
                if ((activate && !hasBuff &&
                     (_menu == null ||
                      _menu.Item(_menu.Name + ".muramana.combo").GetValue<bool>() &&
                      ObjectManager.Player.CountEnemiesInRange(
                          overrideRange > 0 ? overrideRange : Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) >=
                      _menu.Item(_menu.Name + ".muramana.min-enemies-range").GetValue<Slider>().Value &&
                      ObjectManager.Player.ManaPercent >=
                      _menu.Item(_menu.Name + ".muramana.player-mana-above").GetValue<Slider>().Value &&
                      ObjectManager.Player.HealthPercent <=
                      _menu.Item(_menu.Name + ".muramana.player-health-below").GetValue<Slider>().Value &&
                      ObjectManager.Player.HealthPercent >=
                      _menu.Item(_menu.Name + ".muramana.player-health-above").GetValue<Slider>().Value &&
                      (target == null ||
                       target.HealthPercent <=
                       _menu.Item(_menu.Name + ".muramana.target-health-below").GetValue<Slider>().Value &&
                       target.HealthPercent >=
                       _menu.Item(_menu.Name + ".muramana.target-health-above").GetValue<Slider>().Value))) ||
                    !activate && hasBuff)
                {
                    ObjectManager.Player.Spellbook.CastSpell(muramana);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float CalculateLichBaneDamage(Obj_AI_Hero target)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }
                var lichBane = ItemData.Lich_Bane.GetItem();
                if (lichBane.IsOwned(ObjectManager.Player))
                {
                    return
                        (float)
                            (ObjectManager.Player.CalcDamage(
                                target, Damage.DamageType.Physical, ObjectManager.Player.BaseAttackDamage * 0.75f) +
                             ObjectManager.Player.CalcDamage(
                                 target, Damage.DamageType.Magical, ObjectManager.Player.FlatMagicDamageMod * 0.5f));
                }
                var sheen = ItemData.Sheen.GetItem();
                if (sheen.IsOwned() && sheen.IsReady())
                {
                    return
                        (float)
                            ObjectManager.Player.CalcDamage(
                                target, Damage.DamageType.Physical, ObjectManager.Player.BaseAttackDamage);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        public static void UseFleeItems()
        {
            if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
            {
                return;
            }

            try
            {
                foreach (var item in
                    Items.Where(
                        i =>
                            i.Flags.HasFlag(ItemFlags.Flee) &&
                            _menu.Item(_menu.Name + "." + i.Name + ".flee").GetValue<bool>() && i.Item.IsOwned() &&
                            i.Item.IsReady() && i.Item.IsOwned() && i.Item.IsReady() &&
                            ObjectManager.Player.CountEnemiesInRange(i.Range) >=
                            _menu.Item(_menu.Name + "." + i.Name + ".min-enemies-range").GetValue<Slider>().Value &&
                            ObjectManager.Player.HealthPercent <=
                            _menu.Item(_menu.Name + "." + i.Name + ".player-health-below").GetValue<Slider>().Value &&
                            ObjectManager.Player.HealthPercent >=
                            _menu.Item(_menu.Name + "." + i.Name + ".player-health-above").GetValue<Slider>().Value))
                {
                    if (item.CastType != CastType.None)
                    {
                        var lItem = item;
                        var localItem = item;
                        foreach (var enemy in
                            GameObjects.EnemyHeroes.Where(
                                t =>
                                    t.IsValidTarget() && !Invulnerable.Check(t) &&
                                    t.HealthPercent <=
                                    _menu.Item(_menu.Name + "." + lItem.Name + ".target-health-below")
                                        .GetValue<Slider>()
                                        .Value &&
                                    t.HealthPercent >=
                                    _menu.Item(_menu.Name + "." + lItem.Name + ".target-health-above")
                                        .GetValue<Slider>()
                                        .Value)
                                .OrderByDescending(
                                    t =>
                                        t.Position.Distance(ObjectManager.Player.Position, true) <
                                        Math.Pow(localItem.Range, 2)))
                        {
                            if (!Utils.IsImmobile(enemy) && !Utils.IsSlowed(enemy))
                            {
                                switch (localItem.CastType)
                                {
                                    case CastType.Target:
                                        localItem.Item.Cast(enemy);
                                        break;
                                    case CastType.Self:
                                        localItem.Item.Cast(ObjectManager.Player);
                                        break;
                                    case CastType.Position:
                                        var prediction = Prediction.GetPrediction(
                                            enemy, localItem.Delay, localItem.Radius, localItem.Speed);
                                        if (prediction.Hitchance >= HitChance.Medium)
                                        {
                                            localItem.Item.Cast(prediction.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (ObjectManager.Player.CountEnemiesInRange(item.Range) >
                            _menu.Item(_menu.Name + "." + item.Name + ".min-enemies-range").GetValue<Slider>().Value)
                        {
                            item.Item.Cast();
                        }
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