using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Champion;
using Nocturne.Common;

namespace Nocturne.Modes
{
    internal static class ModeSettings
    {
        public static Menu MenuLocal { get; private set; }
        public static Menu MenuSpellQ { get; private set; }
        public static Menu MenuSpellR { get; private set; }
        private static Spell R => PlayerSpells.R;


        public static void Init(Menu ParentMenu)
        {
            MenuLocal = new Menu("Settings", "Settings");
            {

                MenuSpellQ = new Menu("Q:", "Settings.Q");
                {
                    MenuSpellQ.AddItem(
                        new MenuItem("Settings.Q.Hitchance", "Hitchance:").SetValue(
                            new StringList(new[] {"Medium", "High", "Veryhigh"}, 1))
                            .SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor()));
                    MenuLocal.AddSubMenu(MenuSpellQ);
                }

                MenuSpellR = new Menu("R:", "Settings.R");
                {
                    MenuSpellR.AddItem(
                        new MenuItem("Settings.BlockR", "Block R if there is no enemy in R range!").SetValue(true)
                            .SetFontStyle(FontStyle.Regular, PlayerSpells.R.MenuColor()));
                    MenuLocal.AddSubMenu(MenuSpellR);
                }
            }
            ParentMenu.AddSubMenu(MenuLocal);

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static int GetEnemyCount
        {
            get
            {
                return HeroManager.Enemies.Count(e => !e.IsDead && e.IsValidTarget(R.Range));
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!MenuLocal.Item("Settings.BlockR").GetValue<bool>())
            {
                return;
            }

            if (args.Slot == SpellSlot.R && GetEnemyCount == 0)
            {
                args.Process = false;
            }
        }
    }
}
