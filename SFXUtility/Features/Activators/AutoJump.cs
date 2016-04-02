#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AutoJump.cs is part of SFXUtility.

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
using SFXUtility.Library.Logger;
using SharpDX;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXUtility.Features.Activators
{
    internal class AutoJump : Child<Activators>
    {
        private readonly List<HeroJump> _heroJumps = new List<HeroJump>
        {
            new HeroJump(
                "LeeSin", SpellSlot.W, 700f, true, false,
                spell =>
                    spell != null && spell.Instance.Name.Equals("BlindMonkWOne", StringComparison.OrdinalIgnoreCase)),
            new HeroJump("Jax", SpellSlot.Q, 700f, true, true, null),
            new HeroJump("Katarina", SpellSlot.E, 700f, true, true, null)
        };

        private HeroJump _heroJump;
        private float _lastWardTime;
        private Spell _spell;

        public AutoJump(Activators parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Auto Jump"; }
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
                Menu.AddItem(new MenuItem(Name + "PlaceWards", "Place Wards").SetValue(true));
                Menu.AddItem(new MenuItem(Name + "Allies", "Allies").SetValue(true));
                Menu.AddItem(new MenuItem(Name + "Enemies", "Enemies").SetValue(false));
                Menu.AddItem(
                    new MenuItem(Name + "ExistingRange", "Existing Range").SetValue(
                        new Slider(250, 0, (int) _heroJumps.Min(h => h.Range))));
                Menu.AddItem(new MenuItem(Name + "Hotkey", "Hotkey").SetValue(new KeyBind('T', KeyBindType.Press)));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

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
                var heroJump =
                    _heroJumps.FirstOrDefault(
                        h => h.Name.Equals(ObjectManager.Player.ChampionName, StringComparison.OrdinalIgnoreCase));
                if (heroJump == null)
                {
                    OnUnload(null, new UnloadEventArgs(true));
                }
                else
                {
                    _heroJump = heroJump;
                    _spell = new Spell(heroJump.Slot, heroJump.Range);
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private IEnumerable<Obj_AI_Base> GetPossibleObjects(Vector3 position)
        {
            var allies = Menu.Item(Name + "Allies").GetValue<bool>() && _heroJump.Allies;
            var enemies = Menu.Item(Name + "Enemies").GetValue<bool>() && _heroJump.Enemies;
            var existingRange = Menu.Item(Name + "ExistingRange").GetValue<Slider>().Value;

            IEnumerable<Obj_AI_Base> objects = new List<Obj_AI_Base>();

            if (allies && enemies)
            {
                objects = objects.Concat(GameObjects.Heroes).Concat(GameObjects.Minions).Concat(GameObjects.Wards);
            }
            else
            {
                if (allies)
                {
                    objects =
                        objects.Concat(GameObjects.AllyHeroes)
                            .Concat(GameObjects.AllyMinions.Concat(GameObjects.AllyWards));
                }
                else if (enemies)
                {
                    objects =
                        objects.Concat(GameObjects.EnemyHeroes)
                            .Concat(GameObjects.EnemyMinions.Concat(GameObjects.EnemyWards));
                }
            }

            return
                objects.Concat(GameObjects.Jungle)
                    .Where(
                        o =>
                            !o.IsMe && o.IsValidTarget(_spell.Range, false) &&
                            o.ServerPosition.Distance(position) <= existingRange);
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.IsDead || !Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    return;
                }

                if (_spell != null && _heroJump != null && _spell.IsReady() && _heroJump.CustomCheck != null &&
                    _heroJump.CustomCheck(_spell))
                {
                    var jumpPosition = ObjectManager.Player.ServerPosition.Extend(
                        Game.CursorPos, Math.Min(_spell.Range, ObjectManager.Player.Position.Distance(Game.CursorPos)));
                    var castPosition = ObjectManager.Player.ServerPosition.Extend(
                        Game.CursorPos, Math.Min(600, ObjectManager.Player.Position.Distance(Game.CursorPos)));

                    var possibleJumps = GetPossibleObjects(jumpPosition);
                    var target = possibleJumps.FirstOrDefault();
                    if (target != null)
                    {
                        _spell.CastOnUnit(target);
                        return;
                    }

                    var possibleJumps2 = GetPossibleObjects(castPosition);
                    var target2 = possibleJumps2.FirstOrDefault();
                    if (target2 != null)
                    {
                        _spell.CastOnUnit(target2);
                        return;
                    }

                    if (Game.Time - _lastWardTime >= 3 && Menu.Item(Name + "PlaceWards").GetValue<bool>())
                    {
                        var wardSlot = GetWardSlot();
                        if (wardSlot != SpellSlot.Unknown)
                        {
                            ObjectManager.Player.Spellbook.CastSpell(wardSlot, castPosition);
                            _lastWardTime = Game.Time;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private SpellSlot GetWardSlot()
        {
            try
            {
                if (ItemData.Trackers_Knife.GetItem().IsOwned() && ItemData.Trackers_Knife.GetItem().IsReady())
                {
                    return ItemData.Trackers_Knife.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Sightstone.GetItem().IsOwned() && ItemData.Sightstone.GetItem().IsReady())
                {
                    return ItemData.Sightstone.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Ruby_Sightstone.GetItem().IsOwned() && ItemData.Ruby_Sightstone.GetItem().IsReady())
                {
                    return ItemData.Ruby_Sightstone.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Eye_of_the_Watchers.GetItem().IsOwned() && ItemData.Eye_of_the_Watchers.GetItem().IsReady())
                {
                    return ItemData.Eye_of_the_Watchers.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Eye_of_the_Equinox.GetItem().IsOwned() && ItemData.Eye_of_the_Equinox.GetItem().IsReady())
                {
                    return ItemData.Eye_of_the_Equinox.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Eye_of_the_Oasis.GetItem().IsOwned() && ItemData.Eye_of_the_Oasis.GetItem().IsReady())
                {
                    return ItemData.Eye_of_the_Oasis.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Warding_Totem_Trinket.GetItem().IsOwned() &&
                    ItemData.Warding_Totem_Trinket.GetItem().IsReady())
                {
                    return ItemData.Warding_Totem_Trinket.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Farsight_Alteration.GetItem().IsOwned() && ItemData.Farsight_Alteration.GetItem().IsReady())
                {
                    return ItemData.Farsight_Alteration.GetItem().Slots.FirstOrDefault();
                }
                if (ItemData.Vision_Ward.GetItem().IsOwned() && ItemData.Vision_Ward.GetItem().IsReady())
                {
                    return ItemData.Vision_Ward.GetItem().Slots.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return SpellSlot.Unknown;
        }

        private class HeroJump
        {
            public HeroJump(string name,
                SpellSlot slot,
                float range,
                bool allies,
                bool enemies,
                Func<Spell, bool> customCheck)
            {
                Name = name;
                Slot = slot;
                Range = range;
                Allies = allies;
                Enemies = enemies;
                CustomCheck = customCheck;
            }

            public string Name { get; private set; }
            public SpellSlot Slot { get; private set; }
            public float Range { get; private set; }
            public bool Allies { get; private set; }
            public bool Enemies { get; private set; }
            public Func<Spell, bool> CustomCheck { get; private set; }
        }
    }
}