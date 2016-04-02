#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AutoLeveler.cs is part of SFXUtility.

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
using SFXUtility.Library.Logger;

#endregion

namespace SFXUtility.Features.Events
{
    internal class AutoLeveler : Child<Events>
    {
        private const float CheckInterval = 300f;
        private readonly Random random = new Random();
        private bool _delayed;
        private SpellDataInst _e;
        private float _lastCheck = Environment.TickCount;
        private SpellDataInst _q;
        private SpellDataInst _r;
        private SpellDataInst _w;

        public AutoLeveler(Events parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Auto Leveler"; }
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

        private List<SpellInfoStruct> GetOrderedPriorityList()
        {
            return GetPriorityList().OrderByDescending(x => x.Value).ToList();
        }

        private List<SpellInfoStruct> GetPriorityList()
        {
            return
                new List<SpellInfoStruct>
                {
                    new SpellInfoStruct(SpellSlot.Q, Menu.Item(Menu.Name + "Q").GetValue<Slider>().Value),
                    new SpellInfoStruct(SpellSlot.W, Menu.Item(Menu.Name + "W").GetValue<Slider>().Value),
                    new SpellInfoStruct(SpellSlot.E, Menu.Item(Menu.Name + "E").GetValue<Slider>().Value),
                    new SpellInfoStruct(SpellSlot.R, 5)
                }.ToList();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name + ObjectManager.Player.ChampionName);

                var earlyMenu = new Menu("Early", Menu.Name + "Early");
                earlyMenu.AddItem(
                    new MenuItem(earlyMenu.Name + "1", "1: ").SetValue(
                        new StringList(new[] { "None", "Priority", "Q", "W", "E" })));
                earlyMenu.AddItem(
                    new MenuItem(earlyMenu.Name + "2", "2: ").SetValue(
                        new StringList(new[] { "None", "Priority", "Q", "W", "E" }, 2)));
                earlyMenu.AddItem(
                    new MenuItem(earlyMenu.Name + "3", "3: ").SetValue(
                        new StringList(new[] { "None", "Priority", "Q", "W", "E" }, 4)));
                earlyMenu.AddItem(
                    new MenuItem(earlyMenu.Name + "4", "4: ").SetValue(
                        new StringList(new[] { "None", "Priority", "Q", "W", "E" }, 2)));
                earlyMenu.AddItem(
                    new MenuItem(earlyMenu.Name + "5", "5: ").SetValue(
                        new StringList(new[] { "None", "Priority", "Q", "W", "E" }, 3)));

                Menu.AddSubMenu(earlyMenu);

                Menu.AddItem(new MenuItem(Menu.Name + "Q", "Q").SetValue(new Slider(3, 3, 1)));
                Menu.AddItem(new MenuItem(Menu.Name + "W", "W").SetValue(new Slider(1, 3, 1)));
                Menu.AddItem(new MenuItem(Menu.Name + "E", "E").SetValue(new Slider(2, 3, 1)));

                Menu.AddItem(new MenuItem(Menu.Name + "OnlyR", "Only R").SetValue(false));

                Menu.AddItem(new MenuItem(Menu.Name + "Delay", "Delay").SetValue(new Slider(150, 0, 1000)));

                Menu.AddItem(new MenuItem(Menu.Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                _q = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
                _w = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
                _e = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
                _r = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);

                base.OnInitialize();
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

                var availablePoints = ObjectManager.Player.Level - (_q.Level + _w.Level + _e.Level + GetRLevel());

                if (availablePoints > 0)
                {
                    OnUnitLevelUp(
                        ObjectManager.Player,
                        new CustomEvents.Unit.OnLevelUpEventArgs
                        {
                            NewLevel = ObjectManager.Player.Level,
                            RemainingPoints = availablePoints
                        });
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnUnitLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            try
            {
                if (!sender.IsValid || !sender.IsMe)
                {
                    return;
                }

                var availablePoints = args.RemainingPoints;
                var delay = Menu.Item(Menu.Name + "Delay").GetValue<Slider>().Value;
                var pLevel = _q.Level + _w.Level + _e.Level + GetRLevel() + 1;

                if (pLevel <= 5)
                {
                    var index = Menu.Item(Menu.Name + "Early" + pLevel).GetValue<StringList>().SelectedIndex;
                    switch (index)
                    {
                        case 0:
                            return;
                        case 1:
                            break;
                        case 2:
                            if (_q.Level >= MaxSpellLevel(SpellSlot.Q, pLevel))
                            {
                                break;
                            }
                            LevelUp(SpellSlot.Q, delay);
                            return;
                        case 3:
                            if (_w.Level >= MaxSpellLevel(SpellSlot.W, pLevel))
                            {
                                break;
                            }
                            LevelUp(SpellSlot.W, delay);
                            return;
                        case 4:
                            if (_e.Level >= MaxSpellLevel(SpellSlot.E, pLevel))
                            {
                                break;
                            }
                            LevelUp(SpellSlot.E, delay);
                            return;
                    }
                }

                foreach (var pItem in GetOrderedPriorityList())
                {
                    if (availablePoints <= 0)
                    {
                        return;
                    }
                    var pointsToLevelSlot = MaxSpellLevel(pItem.Slot, args.NewLevel) -
                                            ObjectManager.Player.Spellbook.GetSpell(pItem.Slot).Level;
                    pointsToLevelSlot = pointsToLevelSlot > availablePoints ? availablePoints : pointsToLevelSlot;

                    for (var i = 0; pointsToLevelSlot > i; i++)
                    {
                        LevelUp(pItem.Slot, delay);
                        availablePoints--;
                    }
                    if (pItem.Slot == SpellSlot.R && Menu.Item(Menu.Name + "OnlyR").GetValue<bool>())
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void LevelUp(SpellSlot slot, int delay)
        {
            if (!_delayed)
            {
                delay = random.Next((int) (delay * 0.7f), (int) (delay * 1.3f));
                _delayed = true;
                Utility.DelayAction.Add(
                    delay + 1, delegate
                    {
                        ObjectManager.Player.Spellbook.LevelUpSpell(slot);
                        _delayed = false;
                    });
            }
        }

        private int GetRLevel()
        {
            try
            {
                if (ObjectManager.Player.ChampionName.Equals("Karma", StringComparison.CurrentCultureIgnoreCase))
                {
                    return _r.Level - 1;
                }
                if (ObjectManager.Player.ChampionName.Equals("Jayce", StringComparison.CurrentCultureIgnoreCase))
                {
                    return _r.Level - 1;
                }
                if (ObjectManager.Player.ChampionName.Equals("Nidalee", StringComparison.CurrentCultureIgnoreCase))
                {
                    return _r.Level - 1;
                }
                if (ObjectManager.Player.ChampionName.Equals("Elise", StringComparison.CurrentCultureIgnoreCase))
                {
                    return _r.Level - 1;
                }
                return _r.Level;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 1;
        }

        private int MaxSpellLevel(SpellSlot slot, int level)
        {
            var l = slot == SpellSlot.R
                ? (level >= 16 ? 3 : (level >= 11 ? 2 : (level >= 6 ? 1 : 0)))
                : (level >= 9 ? 5 : (level >= 7 ? 4 : (level >= 5 ? 3 : (level >= 3 ? 2 : 1))));
            try
            {
                if (slot == SpellSlot.R)
                {
                    if (ObjectManager.Player.ChampionName.Equals("Karma", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return l + 1;
                    }
                    if (ObjectManager.Player.ChampionName.Equals("Jayce", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return l + 1;
                    }
                    if (ObjectManager.Player.ChampionName.Equals("Nidalee", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return l + 1;
                    }
                    if (ObjectManager.Player.ChampionName.Equals("Elise", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return l + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return l;
        }

        private struct SpellInfoStruct
        {
            public readonly SpellSlot Slot;
            public readonly int Value;

            public SpellInfoStruct(SpellSlot slot, int value)
            {
                Slot = slot;
                Value = value;
            }
        }
    }
}