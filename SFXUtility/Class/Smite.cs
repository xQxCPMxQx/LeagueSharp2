#region License

/*
 Copyright 2014 - 2014 Nikita Bernthaler
 Smite.cs is part of SFXUtility.
 
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

#endregion

namespace SFXUtility.Class
{
    #region

    using System;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SharpDX;

    #endregion

    internal class Smite
    {
        #region Fields

        public readonly bool Available = false;
        public readonly int Range = 750;
        public readonly SpellSlot Slot = SpellSlot.Unknown;
        public readonly string SummonerName = "SummonerSmite";

        #endregion

        #region Constructors

        public Smite()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, SummonerName, StringComparison.CurrentCultureIgnoreCase)))
            {
                Available = true;
                Slot = spell.Slot;
            }
        }

        #endregion

        #region Methods

        public double CalculateDamage()
        {
            int level = ObjectManager.Player.Level;
            int[] stages =
            {
                20*level + 370,
                30*level + 330,
                40*level + 240,
                50*level + 100
            };
            return stages.Max();
        }

        public bool CanUseSpell()
        {
            return Available && !ObjectManager.Player.IsDead && !ObjectManager.Player.IsStunned &&
                   Slot != SpellSlot.Unknown &&
                   ObjectManager.Player.Spellbook.CanUseSpell(Slot) == SpellState.Ready;
        }

        public bool CanUseSpell(Obj_AI_Minion target)
        {
            return CanUseSpell() && target != null && target.IsValid && !target.IsInvulnerable && !target.IsAlly &&
                   IsInRange(target);
        }

        public bool CastSpell(Obj_AI_Minion target)
        {
            if (!CanUseSpell(target))
                return false;
            ObjectManager.Player.Spellbook.CastSpell(Slot, target);
            return true;
        }

        public bool IsInRange(Obj_AI_Minion target)
        {
            return target != null && target.IsValid &&
                   Vector3.Distance(ObjectManager.Player.Position, target.Position) <= Range;
        }

        #endregion
    }
}
