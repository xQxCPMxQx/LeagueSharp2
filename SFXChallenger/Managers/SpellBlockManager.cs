#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SpellBlockManager.cs is part of SFXChallenger.

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
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public class SpellBlockManager
    {
        private static Menu _menu;
        private static readonly HashSet<BlockedSpell> BlockedSpells;
        // Credits: Trees
        static SpellBlockManager()
        {
            try
            {
                BlockedSpells = new HashSet<BlockedSpell>
                {
                    new BlockedSpell("Akali", SpellSlot.Q),
                    new BlockedSpell("Anivia", SpellSlot.E),
                    new BlockedSpell("Annie", SpellSlot.Q),
                    new BlockedSpell("Alistar", SpellSlot.W),
                    new BlockedSpell("Azir", SpellSlot.R),
                    new BlockedSpell("Bard", SpellSlot.R),
                    new BlockedSpell("Blitzcrank", SpellSlot.E) { AutoAttackName = "PowerFistAttack" },
                    new BlockedSpell("Brand", SpellSlot.R),
                    new BlockedSpell("Chogath", SpellSlot.R),
                    new BlockedSpell("Darius", SpellSlot.R),
                    new BlockedSpell("Fiddlesticks", SpellSlot.Q),
                    new BlockedSpell("Fizz", SpellSlot.Q),
                    new BlockedSpell("Gangplank", SpellSlot.Q),
                    new BlockedSpell("Garen", SpellSlot.Q) { AutoAttackName = "GarenQAttack" },
                    new BlockedSpell("Garen", SpellSlot.R),
                    new BlockedSpell("Gragas", SpellSlot.W) { AutoAttackName = "DrunkenRage" },
                    new BlockedSpell("Hecarim", SpellSlot.E) { AutoAttackName = "hecarimrampattack" },
                    new BlockedSpell("Irelia", SpellSlot.E),
                    new BlockedSpell("Janna", SpellSlot.W),
                    new BlockedSpell("Jayce", SpellSlot.E),
                    new BlockedSpell("Kassadin", SpellSlot.Q),
                    new BlockedSpell("Khazix", SpellSlot.Q),
                    new BlockedSpell("LeBlanc", SpellSlot.R),
                    new BlockedSpell("LeeSin", SpellSlot.R),
                    new BlockedSpell("Leona", SpellSlot.Q) { AutoAttackName = "LeonaShieldOfDaybreakAttack" },
                    new BlockedSpell("Lissandra", (SpellSlot) 48),
                    new BlockedSpell("Lulu", SpellSlot.W),
                    new BlockedSpell("Malphite", SpellSlot.Q),
                    new BlockedSpell("Maokai", SpellSlot.W),
                    new BlockedSpell("MissFortune", SpellSlot.Q),
                    new BlockedSpell("MonkeyKing", SpellSlot.Q) { AutoAttackName = "MonkeyKingQAttack" },
                    new BlockedSpell("Mordekaiser", SpellSlot.Q) { AutoAttackName = "mordekaiserqattack2" },
                    new BlockedSpell("Mordekaiser", SpellSlot.R),
                    new BlockedSpell("Nasus", SpellSlot.Q) { AutoAttackName = "NasusQAttack" },
                    new BlockedSpell("Nasus", SpellSlot.W),
                    new BlockedSpell("Nidalee", SpellSlot.Q)
                    {
                        AutoAttackName = "NidaleeTakedownAttack",
                        ModelName = "nidalee_cougar"
                    },
                    new BlockedSpell("Nunu", SpellSlot.E),
                    new BlockedSpell("Malzahar", SpellSlot.R),
                    new BlockedSpell("Pantheon", SpellSlot.W),
                    new BlockedSpell("Poppy", SpellSlot.Q) { AutoAttackBuff = "PoppyDevastatingBlow" },
                    new BlockedSpell("Poppy", SpellSlot.E),
                    new BlockedSpell("Quinn", SpellSlot.E) { ModelName = "quinnvalor" },
                    new BlockedSpell("Rammus", SpellSlot.E),
                    new BlockedSpell("Renekton", SpellSlot.W) { AutoAttackName = "RenektonExecute" },
                    new BlockedSpell("Renekton", SpellSlot.W) { AutoAttackName = "RenektonSuperExecute" },
                    new BlockedSpell("Rengar", SpellSlot.Q) { AutoAttackName = "RengarBasicAttack" },
                    new BlockedSpell("Riven", SpellSlot.Q),
                    new BlockedSpell("Ryze", SpellSlot.W),
                    new BlockedSpell("Shaco", SpellSlot.Q),
                    new BlockedSpell("Shyvana", SpellSlot.Q) { AutoAttackName = "ShyvanaDoubleAttackHit" },
                    new BlockedSpell("Singed", SpellSlot.E),
                    new BlockedSpell("Skarner", SpellSlot.R),
                    new BlockedSpell("Syndra", SpellSlot.R),
                    new BlockedSpell("Swain", SpellSlot.E),
                    new BlockedSpell("TahmKench", SpellSlot.W),
                    new BlockedSpell("Talon", SpellSlot.E),
                    new BlockedSpell("Taric", SpellSlot.E),
                    new BlockedSpell("Teemo", SpellSlot.Q),
                    new BlockedSpell("Tristana", SpellSlot.R),
                    new BlockedSpell("Trundle", SpellSlot.Q) { AutoAttackName = "TrundleQ" },
                    new BlockedSpell("Trundle", SpellSlot.R),
                    new BlockedSpell("TwistedFate", SpellSlot.W) { AutoAttackName = "goldcardpreattack" },
                    new BlockedSpell("Udyr", SpellSlot.E) { AutoAttackName = "UdyrBearAttack" },
                    new BlockedSpell("Urgot", SpellSlot.R),
                    new BlockedSpell("Vayne", SpellSlot.E),
                    new BlockedSpell("Veigar", SpellSlot.R),
                    new BlockedSpell("Vi", SpellSlot.R),
                    new BlockedSpell("Vladimir", SpellSlot.R),
                    new BlockedSpell("Volibear", SpellSlot.Q) { AutoAttackName = "VolibearQAttack" },
                    new BlockedSpell("Volibear", SpellSlot.W),
                    new BlockedSpell("XinZhao", SpellSlot.Q) { AutoAttackName = "XenZhaoThrust3" },
                    new BlockedSpell("XinZhao", SpellSlot.R),
                    new BlockedSpell("Yorick", SpellSlot.E),
                    new BlockedSpell("Zac", SpellSlot.R)
                };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool Contains(Obj_AI_Hero unit, GameObjectProcessSpellCastEventArgs args)
        {
            var name = unit.ChampionName;
            var slot = unit.GetSpellSlot(args.SData.Name);

            foreach (var spell in
                BlockedSpells.Where(o => o.Name.Equals(name))
                    .Where(spell => !spell.HasModelCondition || unit.CharData.BaseSkinName.Equals(spell.ModelName))
                    .Where(spell => !spell.HasBuffCondition || unit.HasBuff(spell.AutoAttackBuff)))
            {
                if (spell.IsAutoAttack)
                {
                    if (!args.SData.IsAutoAttack())
                    {
                        continue;
                    }

                    var condition = spell.AutoAttackName.Equals(args.SData.Name);

                    if (unit.ChampionName.Equals("Rengar"))
                    {
                        condition = condition && unit.Mana.Equals(5);
                    }
                    condition = condition && _menu.Item(_menu.Name + "." + name + "AA") != null &&
                                _menu.Item(_menu.Name + "." + name + "AA").GetValue<bool>();
                    if (condition)
                    {
                        return true;
                    }
                    continue;
                }

                if (_menu.Item(_menu.Name + "." + name) == null || !_menu.Item(_menu.Name + "." + name).GetValue<bool>() ||
                    !spell.Slot.Equals(slot))
                {
                    continue;
                }

                if (name.Equals("Riven"))
                {
                    var buff = unit.Buffs.FirstOrDefault(b => b.Name.Equals("RivenTriCleave"));
                    if (buff != null && buff.Count == 3)
                    {
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        public static void AddToMenu(Menu menu, bool ally, bool enemy, bool autoAttack)
        {
            try
            {
                _menu = menu;
                foreach (var hero in GameObjects.Heroes.Where(h => (ally && h.IsAlly || enemy && h.IsEnemy) && !h.IsMe))
                {
                    var lHero = hero;
                    foreach (var spell in
                        BlockedSpells.Where(
                            o =>
                                o.Name.Equals(lHero.ChampionName, StringComparison.OrdinalIgnoreCase) &&
                                (autoAttack && o.IsAutoAttack || !autoAttack && !o.IsAutoAttack)))
                    {
                        var name = lHero.ChampionName.Equals("MonkeyKing") ? "Wukong" : lHero.ChampionName;
                        var slot = spell.Slot.Equals(48) ? SpellSlot.R : spell.Slot;
                        menu.AddItem(
                            new MenuItem(
                                menu.Name + "." + (spell.IsAutoAttack ? lHero.ChampionName + "AA" : lHero.ChampionName),
                                name + " " + slot + (spell.IsAutoAttack ? " AA" : string.Empty)).SetValue(true));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public class BlockedSpell
        {
            public string AutoAttackBuff;
            public string AutoAttackName;
            public string ModelName;
            public string Name;
            public SpellSlot Slot;

            public BlockedSpell(string name, SpellSlot slot)
            {
                Name = name;
                Slot = slot;
            }

            public bool IsAutoAttack
            {
                get { return !string.IsNullOrWhiteSpace(AutoAttackName); }
            }

            public bool HasBuffCondition
            {
                get { return !string.IsNullOrWhiteSpace(AutoAttackBuff); }
            }

            public bool HasModelCondition
            {
                get { return !string.IsNullOrWhiteSpace(ModelName); }
            }
        }
    }
}