#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AntiRengar.cs is part of SFXUtility.

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

#endregion

namespace SFXUtility.Features.Activators
{
    internal class AntiRengar : Child<Activators>
    {
        private readonly List<HeroSpell> _heroSpells = new List<HeroSpell>
        {
            new HeroSpell("Vayne", SpellSlot.E, 550f),
            new HeroSpell("Tristana", SpellSlot.R, 550f),
            new HeroSpell("Draven", SpellSlot.E, 1100f),
            new HeroSpell("LeeSin", SpellSlot.R, 375f),
            new HeroSpell("Janna", SpellSlot.R, 550f),
            new HeroSpell("Fiddlesticks", SpellSlot.Q, 575f),
            new HeroSpell("Ashe", SpellSlot.R, 1500f),
            new HeroSpell("Braum", SpellSlot.R, 1200f),
            new HeroSpell("Thresh", SpellSlot.E, 400f),
            new HeroSpell("Urgot", SpellSlot.R, 700f),
            new HeroSpell("VelKoz", SpellSlot.E, 800f),
            new HeroSpell("Morgana", SpellSlot.Q, 1175f)
        };

        private Obj_AI_Hero _rengar;
        private Spell _spell;

        public AntiRengar(Activators parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Anti Rengar"; }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            base.OnDisable();
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

                var spellsMenu = new Menu("Spells", Name + "Spells");
                foreach (var spell in _heroSpells)
                {
                    spellsMenu.AddItem(
                        new MenuItem(spellsMenu.Name + spell.Name, spell.Name + " " + spell.Slot).SetValue(true));
                }
                Menu.AddSubMenu(spellsMenu);

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
                var heroSpell =
                    _heroSpells.FirstOrDefault(
                        h => h.Name.Equals(ObjectManager.Player.ChampionName, StringComparison.OrdinalIgnoreCase));
                var rengar =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        h => h.ChampionName.Equals("Rengar", StringComparison.OrdinalIgnoreCase));
                if (heroSpell == null || rengar == null)
                {
                    OnUnload(null, new UnloadEventArgs(true));
                }
                else
                {
                    _rengar = rengar;
                    _spell = new Spell(heroSpell.Slot, heroSpell.Range);
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            if (ObjectManager.Player.IsDead || _spell == null || _rengar == null || _rengar.IsDead || !_spell.IsReady() ||
                !sender.IsEnemy || !sender.Name.Equals("Rengar_LeapSound.troy", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var menuItem = Menu.Item(Name + "Spells" + ObjectManager.Player.ChampionName);
            if (menuItem != null && menuItem.GetValue<bool>() && _rengar.Distance(ObjectManager.Player) < _spell.Range)
            {
                _spell.Cast(_rengar);
            }
        }

        private class HeroSpell
        {
            public HeroSpell(string name, SpellSlot slot, float range)
            {
                Name = name;
                Slot = slot;
                Range = range;
            }

            public string Name { get; private set; }
            public SpellSlot Slot { get; private set; }
            public float Range { get; private set; }
        }
    }
}