#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Trinket.cs is part of SFXUtility.

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
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Logger;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXUtility.Features.Events
{
    internal class Trinket : Child<Events>
    {
        private const float CheckInterval = 333f;
        private float _lastCheck = Environment.TickCount;

        public Trinket(Events parent) : base(parent)
        {
            OnLoad();
        }

        protected override List<Utility.Map.MapType> BlacklistedMaps
        {
            get { return new List<Utility.Map.MapType> { Utility.Map.MapType.CrystalScar }; }
        }

        public override string Name
        {
            get { return "Trinket"; }
        }

        protected override void OnEnable()
        {
            LeagueSharp.Game.OnUpdate += OnGameUpdate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            LeagueSharp.Game.OnUpdate -= OnGameUpdate;
            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

                Menu.Name = Menu.Name + ObjectManager.Player.ChampionName;
                var levelMenu = new Menu("Level", Menu.Name + "Level");
                levelMenu.AddItem(
                    new MenuItem(levelMenu.Name + "WardingTotem", "Warding Totem @ Level").SetValue(
                        new Slider(1, 1, 18)));
                levelMenu.AddItem(
                    new MenuItem(levelMenu.Name + "SweepingLens", "Sweeping Lens @ Level").SetValue(
                        new Slider(6, 1, 18)));
                levelMenu.AddItem(new MenuItem(levelMenu.Name + "WardingTotemBuy", "Buy Warding Totem").SetValue(false));
                levelMenu.AddItem(new MenuItem(levelMenu.Name + "SweepingLensBuy", "Buy Sweeping Lens").SetValue(false));
                levelMenu.AddItem(new MenuItem(levelMenu.Name + "Enabled", "Enabled").SetValue(false));

                var eventsMenu = new Menu("Events", Menu.Name + "Events");
                eventsMenu.AddItem(new MenuItem(eventsMenu.Name + "Sightstone", "Sightstone").SetValue(false));
                eventsMenu.AddItem(new MenuItem(eventsMenu.Name + "TrackersKnife", "Tracker's Knife").SetValue(false));

                eventsMenu.AddItem(
                    new MenuItem(eventsMenu.Name + "BuyTrinket", "Buy Trinket").SetValue(
                        new StringList(new[] { "Yellow", "Red" })));
                eventsMenu.AddItem(new MenuItem(eventsMenu.Name + "Enabled", "Enabled").SetValue(false));

                Menu.AddSubMenu(levelMenu);
                Menu.AddSubMenu(eventsMenu);

                Menu.AddItem(new MenuItem(Menu.Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (_lastCheck + CheckInterval > Environment.TickCount)
                {
                    return;
                }

                _lastCheck = Environment.TickCount;

                if (ObjectManager.Player.IsDead || ObjectManager.Player.InShop())
                {
                    if (ItemData.Farsight_Alteration.GetItem().IsOwned() ||
                        ItemData.Oracle_Alteration.GetItem().IsOwned())
                    {
                        return;
                    }

                    var hasYellow = ItemData.Warding_Totem_Trinket.GetItem().IsOwned();
                    var hasRed = ItemData.Sweeping_Lens_Trinket.GetItem().IsOwned();

                    if (Menu.Item(Menu.Name + "EventsEnabled").GetValue<bool>())
                    {
                        bool hasTrinket;
                        var trinketId = (ItemId) 0;
                        switch (Menu.Item(Menu.Name + "EventsBuyTrinket").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                hasTrinket = hasYellow;
                                trinketId = ItemId.Warding_Totem_Trinket;
                                break;

                            case 1:
                                hasTrinket = hasRed;
                                trinketId = ItemId.Sweeping_Lens_Trinket;
                                break;

                            default:
                                hasTrinket = true;
                                break;
                        }

                        if (Menu.Item(Menu.Name + "EventsSightstone").GetValue<bool>() &&
                            (ItemData.Sightstone.GetItem().IsOwned() || ItemData.Ruby_Sightstone.GetItem().IsOwned()) ||
                            Menu.Item(Menu.Name + "EventsTrackersKnife").GetValue<bool>() &&
                            ItemData.Trackers_Knife.GetItem().IsOwned())
                        {
                            if (!hasTrinket)
                            {
                                SwitchTrinket(trinketId);
                            }
                            return;
                        }
                    }

                    if (Menu.Item(Menu.Name + "LevelEnabled").GetValue<bool>())
                    {
                        var tsList = new List<TrinketStruct>
                        {
                            new TrinketStruct(
                                ItemId.Warding_Totem_Trinket, hasYellow,
                                Menu.Item(Menu.Name + "LevelWardingTotemBuy").GetValue<bool>(),
                                Menu.Item(Menu.Name + "LevelWardingTotem").GetValue<Slider>().Value),
                            new TrinketStruct(
                                ItemId.Sweeping_Lens_Trinket, hasRed,
                                Menu.Item(Menu.Name + "LevelSweepingLensBuy").GetValue<bool>(),
                                Menu.Item(Menu.Name + "LevelSweepingLens").GetValue<Slider>().Value)
                        };
                        tsList = tsList.OrderBy(ts => ts.Level).ToList();

                        for (int i = 0, l = tsList.Count; i < l; i++)
                        {
                            if (ObjectManager.Player.Level >= tsList[i].Level)
                            {
                                var hasHigher = false;
                                if (i != l - 1)
                                {
                                    for (var j = i + 1; j < l; j++)
                                    {
                                        if (ObjectManager.Player.Level >= tsList[j].Level && tsList[j].Buy)
                                        {
                                            hasHigher = true;
                                        }
                                    }
                                }
                                if (!hasHigher && tsList[i].Buy && !tsList[i].HasItem)
                                {
                                    SwitchTrinket(tsList[i].ItemId);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void SwitchTrinket(ItemId itemId)
        {
            try
            {
                if ((int) itemId <= 0)
                {
                    return;
                }
                var iItem =
                    ObjectManager.Player.InventoryItems.FirstOrDefault(
                        slot =>
                            slot.IsValidSlot() &&
                            slot.IData.SpellName.Contains("Trinket", StringComparison.OrdinalIgnoreCase) ||
                            slot.IData.DisplayName.Contains("Trinket", StringComparison.OrdinalIgnoreCase));
                if (iItem != null)
                {
                    ObjectManager.Player.SellItem(iItem.Slot);
                }
                ObjectManager.Player.BuyItem(itemId);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private struct TrinketStruct
        {
            public readonly bool Buy;
            public readonly bool HasItem;
            public readonly ItemId ItemId;
            public readonly int Level;

            public TrinketStruct(ItemId itemId, bool hasItem, bool buy, int level)
            {
                ItemId = itemId;
                HasItem = hasItem;
                Buy = buy;
                Level = level;
            }
        }
    }
}