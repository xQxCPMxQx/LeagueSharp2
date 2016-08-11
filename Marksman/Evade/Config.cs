// Copyright 2014 - 2014 Esk0r
// Config.cs is part of Evade.
// 
// Evade is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Evade is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Evade. If not, see <http://www.gnu.org/licenses/>.

#region

using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Marksman.Evade
{
    internal static class Config
    {
        public const bool PrintSpellData = false;
        public const bool TestOnAllies = false;
        public const int SkillShotsExtraRadius = 9;
        public const int SkillShotsExtraRange = 20;
        public const int GridSize = 10;
        public const int ExtraEvadeDistance = 15;
        public const int DiagonalEvadePointsCount = 7;
        public const int DiagonalEvadePointsStep = 20;

        public const int CrossingTimeOffset = 250;

        public const int EvadingFirstTimeOffset = 250;
        public const int EvadingSecondTimeOffset = 0;

        public const int EvadingRouteChangeTimeOffset = 250;

        public const int EvadePointChangeInterval = 300;
        public static int LastEvadePointChangeT = 0;

        public static Menu Menu, MenuEvadeSpells, MenuSkillShots;

        public static void CreateMenu()
        {
            Menu = new Menu("Evade", "Evade");

            //Create the evade spells submenus.
            MenuEvadeSpells = new Menu("Evade spells", "evadeSpells");
            foreach (var spell in EvadeSpellDatabase.Spells)
            {
                var subMenu = new Menu(spell.Name, spell.Name);

                if (spell.IsTargetted && spell.ValidTargets.Contains(SpellValidTargets.AllyWards))
                {
                    subMenu.AddItem(new MenuItem("WardJump" + spell.Name, "WardJump").SetValue(true));
                }

                subMenu.AddItem(new MenuItem("Enabled" + spell.Name, "Enabled").SetValue(true));
                //subMenu.AddItem(new MenuItem("OnlyDangerous" + spell.Name, "Only For Dangerous Spells").SetValue(true));

                MenuEvadeSpells.AddSubMenu(subMenu);
            }
            Menu.AddSubMenu(MenuEvadeSpells);

            //Create the skillshots submenus.
            MenuSkillShots = new Menu("Skillshots", "Skillshots");

            foreach (var hero in HeroManager.Enemies)
            {
                if (hero.ChampionName == "Vayne")
                {
                    Menu vayneE = new Menu("Vayne - Block 3. Silver Buff Stack", "VayneE");
                    MenuSkillShots.AddSubMenu(vayneE);
                    vayneE.AddItem(new MenuItem("VayneBlockSilverBuff", "Enabled").SetValue(true));
                }

                foreach (var spell in SpellDatabase.Spells)
                {
                    if (spell.ChampionName.ToLower() == hero.ChampionName.ToLower())
                    {
                        var subMenu = new Menu(spell.MenuItemName, spell.MenuItemName);

                        //subMenu.AddItem(new MenuItem("IsDangerous" + spell.MenuItemName, "Is Dangerous").SetValue(spell.IsDangerous));
                        subMenu.AddItem(new MenuItem("Draw" + spell.MenuItemName, "Draw").SetValue(true));
                        subMenu.AddItem(
                            new MenuItem("Enabled" + spell.MenuItemName, "Enabled").SetValue(!spell.DisabledByDefault));

                        MenuSkillShots.AddSubMenu(subMenu);
                    }
                }
            }

            Menu.AddSubMenu(MenuSkillShots);

            var shielding = new Menu("Ally shielding", "Shielding");

            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly && !ally.IsMe))
            {
                shielding.AddItem(new MenuItem("shield" + ally.ChampionName, "Shield " + ally.ChampionName).SetValue(true));
            }
            Menu.AddSubMenu(shielding);

            var collision = new Menu("Collision", "Collision");
            collision.AddItem(new MenuItem("MinionCollision", "Minion collision").SetValue(true));
            collision.AddItem(new MenuItem("HeroCollision", "Hero collision").SetValue(true));
            collision.AddItem(new MenuItem("YasuoCollision", "Yasuo wall collision").SetValue(true));
            collision.AddItem(new MenuItem("EnableCollision", "Enabled").SetValue(true));
            //TODO add mode.
            Menu.AddSubMenu(collision);

            var drawings = new Menu("Drawings", "Drawings");
            drawings.AddItem(new MenuItem("EnabledColor", "Enabled spell color").SetValue(Color.White));
            drawings.AddItem(new MenuItem("DisabledColor", "Disabled spell color").SetValue(Color.Red));
            drawings.AddItem(new MenuItem("MissileColor", "Missile color").SetValue(Color.LimeGreen));
            drawings.AddItem(new MenuItem("Border", "Border Width").SetValue(new Slider(1, 5, 1)));

            drawings.AddItem(new MenuItem("EnableDrawings", "Enabled").SetValue(true));
            Menu.AddSubMenu(drawings);

            var misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("DisableFow", "Disable fog of war dodging").SetValue(true));
            misc.AddItem(new MenuItem("ShowEvadeStatus", "Show Evade Status").SetValue(false));
            Menu.AddSubMenu(misc);

            Menu.AddItem(
                new MenuItem("Enabled", "Enabled").SetValue(new KeyBind("K".ToCharArray()[0], KeyBindType.Toggle, true)));
        }
    }
}
