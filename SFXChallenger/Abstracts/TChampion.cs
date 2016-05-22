#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TChampion.cs is part of SFXChallenger.

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
using SFXChallenger.Library.Logger;
using SFXChallenger.SFXTargetSelector;

#endregion

namespace SFXChallenger.Abstracts
{
    // ReSharper disable once InconsistentNaming
    internal abstract class TChampion : Champion
    {
        public readonly float MaxRange;
        public List<Obj_AI_Hero> Targets = new List<Obj_AI_Hero>();

        protected TChampion(float maxRange)
        {
            MaxRange = maxRange;
        }

        protected override void OnCorePreUpdate(EventArgs args)
        {
            try
            {
                Targets = TargetSelector.GetTargets(MaxRange).ToList();
                base.OnCorePreUpdate(args);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}