#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 InitiatorManager.cs is part of SFXChallenger.

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
using SFXChallenger.Args;
using SFXChallenger.Library;
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public class InitiatorManager
    {
        private static Menu _menu;
        private static readonly HashSet<SpellData> Initiators;

        static InitiatorManager()
        {
            try
            {
                Initiators = new HashSet<SpellData>
                {
                    new SpellData("Aatrox", SpellSlot.Q),
                    new SpellData("Akali", SpellSlot.R),
                    new SpellData("Alistar", SpellSlot.W),
                    new SpellData("Amumu", SpellSlot.Q),
                    new SpellData("Diana", SpellSlot.R),
                    new SpellData("Elise", SpellSlot.E, "EliseSpiderEInitial"),
                    new SpellData("FiddleSticks", SpellSlot.R),
                    new SpellData("Fiora", SpellSlot.Q),
                    new SpellData("Gnar", SpellSlot.E, "GnarBigE"),
                    new SpellData("Gragas", SpellSlot.E),
                    new SpellData("Hecarim", SpellSlot.R),
                    new SpellData("Irelia", SpellSlot.Q),
                    new SpellData("JarvanIV", SpellSlot.E, "JarvanIVDragonStrike"),
                    new SpellData("JarvanIV", SpellSlot.R),
                    new SpellData("Jax", SpellSlot.Q),
                    new SpellData("Kassadin", SpellSlot.R),
                    new SpellData("Katarina", SpellSlot.E),
                    new SpellData("Kennen", SpellSlot.E),
                    new SpellData("KhaZix", SpellSlot.E),
                    new SpellData("KhaZix", SpellSlot.E, "KhazixELong"),
                    new SpellData("LeeSin", SpellSlot.Q, "BlindMonkQTwo"),
                    new SpellData("Leona", SpellSlot.E),
                    new SpellData("Lissandra", SpellSlot.E),
                    new SpellData("Malphite", SpellSlot.R),
                    new SpellData("Maokai", SpellSlot.W),
                    new SpellData("MonkeyKing", SpellSlot.E),
                    new SpellData("Nocturne", SpellSlot.R),
                    new SpellData("Olaf", SpellSlot.R),
                    new SpellData("Poppy", SpellSlot.E),
                    new SpellData("Rammus", SpellSlot.Q),
                    new SpellData("RekSai", SpellSlot.E, "RekSaiEBurrowed"),
                    new SpellData("Renekton", SpellSlot.E),
                    new SpellData("Rengar", SpellSlot.R),
                    new SpellData("Sejuani", SpellSlot.Q),
                    new SpellData("Shen", SpellSlot.E),
                    new SpellData("Shyvana", SpellSlot.R),
                    new SpellData("Sion", SpellSlot.R),
                    new SpellData("Talon", SpellSlot.E),
                    new SpellData("Thresh", SpellSlot.Q, "ThreshQLeap"),
                    new SpellData("Tristana", SpellSlot.W),
                    new SpellData("Tryndamere", SpellSlot.E),
                    new SpellData("Urgot", SpellSlot.R),
                    new SpellData("Vi", SpellSlot.Q),
                    new SpellData("Vi", SpellSlot.R),
                    new SpellData("Volibear", SpellSlot.Q),
                    new SpellData("Warwick", SpellSlot.R),
                    new SpellData("Yasuo", SpellSlot.R),
                    new SpellData("Zac", SpellSlot.E),
                    new SpellData("Zed", SpellSlot.R),
                    new SpellData("Flash", SpellSlot.Unknown, SummonerManager.Flash.Name, true)
                };
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static event EventHandler<InitiatorManagerArgs> OnInitiator;
        public static event EventHandler<InitiatorManagerArgs> OnAllyInitiator;
        public static event EventHandler<InitiatorManagerArgs> OnEnemyInitiator;

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero == null || hero.IsMe)
                {
                    return;
                }
                var initiator =
                    Initiators.FirstOrDefault(
                        i =>
                            !string.IsNullOrEmpty(i.Name) &&
                            i.Name.ToLower().Equals(args.SData.Name.ToLower(), StringComparison.OrdinalIgnoreCase));
                if (initiator != null)
                {
                    var item = _menu != null
                        ? _menu.Item(_menu.Name + "." + initiator.Hero + "." + initiator.Slot)
                        : null;
                    if (_menu == null || item != null && item.GetValue<bool>())
                    {
                        var eventArgs = new InitiatorManagerArgs(
                            hero, args.Start, args.Target != null ? args.Target.Position : args.End, initiator.Range);
                        OnInitiator.RaiseEvent(null, eventArgs);
                        (hero.IsAlly ? OnAllyInitiator : OnEnemyInitiator).RaiseEvent(null, eventArgs);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void AddToMenu(Menu menu, bool ally, bool enemy)
        {
            try
            {
                _menu = menu;

                foreach (var initiator in
                    Initiators.Where(
                        i =>
                            !string.IsNullOrEmpty(i.Name) &&
                            (i.Custom ||
                             GameObjects.Heroes.Any(
                                 h =>
                                     (ally && h.IsAlly || enemy && h.IsEnemy) &&
                                     h.ChampionName.Equals(i.Hero, StringComparison.OrdinalIgnoreCase))))
                        .GroupBy(i => new { i.Hero, i.Slot })
                        .Select(i => i.Key))
                {
                    menu.AddItem(
                        new MenuItem(
                            menu.Name + "." + initiator.Hero + "." + initiator.Slot,
                            initiator.Hero +
                            (initiator.Slot != SpellSlot.Unknown
                                ? " " + initiator.Slot.ToString().ToUpper()
                                : string.Empty)).SetValue(true));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class SpellData
        {
            public SpellData(string hero, SpellSlot slot, string name = null, bool custom = false)
            {
                try
                {
                    Hero = hero;
                    Slot = slot;
                    Custom = custom;
                    Range = 500;
                    if (name != null)
                    {
                        Name = name;
                    }
                    else if (slot != SpellSlot.Unknown)
                    {
                        var champ =
                            GameObjects.Heroes.FirstOrDefault(
                                h => h.ChampionName.Equals(hero, StringComparison.OrdinalIgnoreCase));
                        if (champ != null)
                        {
                            var spell = champ.GetSpell(Slot);
                            if (spell != null)
                            {
                                Name = spell.Name;
                                Range = spell.SData.CastRange > spell.SData.CastRangeDisplayOverride + 1000
                                    ? spell.SData.CastRangeDisplayOverride
                                    : spell.SData.CastRange;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public float Range { get; set; }
            public string Hero { get; private set; }
            public SpellSlot Slot { get; private set; }
            public bool Custom { get; set; }
            public string Name { get; private set; }
        }
    }
}