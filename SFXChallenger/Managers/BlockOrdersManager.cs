#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 BlockOrdersManager.cs is part of SFXChallenger.

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
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public static class BlockOrdersManager
    {
        static BlockOrdersManager()
        {
            try
            {
                Spellbook.OnCastSpell += OnSpellbookCastSpell;
                Obj_AI_Base.OnIssueOrder += OnObjAiBaseIssueOrder;
                Game.OnUpdate += OnGameUpdate;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool Orders { get; set; }
        public static bool Spells { get; set; }
        public static bool Automatic { get; set; }
        public static bool Enabled { get; set; }

        private static void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (Automatic)
                {
                    Enabled = ObjectManager.Player.IsChannelingImportantSpell();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (Enabled && Spells && sender.Owner.IsMe)
                {
                    args.Process = false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnObjAiBaseIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            try
            {
                if (Enabled && Orders && sender.IsMe)
                {
                    args.Process = false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}