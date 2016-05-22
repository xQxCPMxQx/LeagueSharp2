#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Summoners.cs is part of SFXWard.

 SFXWard is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXWard is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXWard. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXWard.Library.Extensions.NET;
using SFXWard.Library.Logger;
using SharpDX;

#endregion

namespace SFXWard.Data
{
    internal class SummonerSpell
    {
        private SpellSlot? _slot;
        public string Name { get; set; }
        public float Range { get; set; }

        public SpellSlot Slot
        {
            get { return (SpellSlot) (_slot ?? (_slot = ObjectManager.Player.GetSpellSlot(Name))); }
        }
    }

    internal static class Summoners
    {
        public static SummonerSpell BlueSmite;
        public static SummonerSpell RedSmite;
        public static SummonerSpell Ignite;
        public static SummonerSpell Smite;
        public static List<SummonerSpell> SummonerSpells;

        static Summoners()
        {
            try
            {
                // ReSharper disable once StringLiteralTypo
                BlueSmite = new SummonerSpell { Name = "s5_summonersmiteplayerganker", Range = 750f };
                RedSmite = new SummonerSpell { Name = "s5_summonersmiteduel", Range = 750f };
                Ignite = new SummonerSpell { Name = "SummonerDot", Range = 600f };
                Smite = new SummonerSpell { Name = "SummonerSmite", Range = 750f };

                SummonerSpells = new List<SummonerSpell> { Ignite, Smite, BlueSmite, RedSmite };
            }
            catch (Exception ex)
            {
                SummonerSpells = new List<SummonerSpell>();
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool IsReady(this SummonerSpell spell)
        {
            return spell.Slot != SpellSlot.Unknown && spell.Slot.IsReady();
        }

        public static List<SummonerSpell> AvailableSummoners()
        {
            return SummonerSpells.Where(ss => ss.Exists()).ToList();
        }

        public static bool Exists(this SummonerSpell spell)
        {
            return spell.Slot != SpellSlot.Unknown;
        }

        public static void Cast(this SummonerSpell spell)
        {
            ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
        }

        public static void Cast(this SummonerSpell spell, Obj_AI_Hero target)
        {
            ObjectManager.Player.Spellbook.CastSpell(spell.Slot, target);
        }

        public static void Cast(this SummonerSpell spell, Vector3 position)
        {
            ObjectManager.Player.Spellbook.CastSpell(spell.Slot, position);
        }

        public static float CalculateBlueSmiteDamage()
        {
            return 20 + ObjectManager.Player.Level * 8;
        }

        public static float CalculateRedSmiteDamage()
        {
            return 54 + ObjectManager.Player.Level * 6;
        }

        public static float CalculateComboDamage(Obj_AI_Hero target, bool ignite, bool smite)
        {
            try
            {
                var damage = 0f;
                if (ignite && Ignite.Exists() && Ignite.IsReady() &&
                    target.Position.Distance(ObjectManager.Player.Position) <= Ignite.Range)
                {
                    damage += (float) ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
                }
                if (smite)
                {
                    if (BlueSmite.Exists() && BlueSmite.IsReady() &&
                        target.Position.Distance(ObjectManager.Player.Position) <= BlueSmite.Range)
                    {
                        damage += CalculateBlueSmiteDamage();
                    }
                    else if (RedSmite.Exists() && RedSmite.IsReady() &&
                             target.Position.Distance(ObjectManager.Player.Position) <= RedSmite.Range)
                    {
                        damage += CalculateRedSmiteDamage();
                    }
                }
                return damage;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public static void UseComboSummoners(Obj_AI_Hero target, bool ignite, bool smite)
        {
            try
            {
                if (ignite && Ignite.Exists() && Ignite.IsReady() &&
                    target.Position.Distance(ObjectManager.Player.Position) <= Ignite.Range)
                {
                    Ignite.Cast(target);
                }
                if (smite)
                {
                    if (BlueSmite.Exists() && BlueSmite.IsReady() &&
                        target.Position.Distance(ObjectManager.Player.Position) <= BlueSmite.Range)
                    {
                        BlueSmite.Cast(target);
                    }
                    else if (RedSmite.Exists() && RedSmite.IsReady() &&
                             target.Position.Distance(ObjectManager.Player.Position) <= RedSmite.Range)
                    {
                        RedSmite.Cast(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static string FixName(string name)
        {
            try
            {
                return name.Contains("Smite", StringComparison.OrdinalIgnoreCase)
                    ? "summonersmite"
                    : (name.Contains("Teleport", StringComparison.OrdinalIgnoreCase)
                        ? "summonerteleport"
                        : name.ToLower());
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return name;
        }
    }
}