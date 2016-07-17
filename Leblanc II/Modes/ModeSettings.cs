using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Leblanc.Champion;
using Leblanc.Common;
using Color = SharpDX.Color;

namespace Leblanc.Modes
{
    internal class ModeSettings
    {
        public static Menu MenuLocal { get; private set; }
        public static Menu MenuSkins { get; private set; }
        public static Menu MenuSettingQ { get; private set; }
        public static Menu MenuSettingE { get; private set; }
        public static Menu MenuFlame { get; private set; }
        public static Menu MenuSpellR { get; private set; }

        public static int MaxERange => MenuSettingE.Item("Settings.E.MaxRange").GetValue<Slider>().Value;
        public static int EHitchance => MenuSettingE.Item("Settings.E.Hitchance").GetValue<StringList>().SelectedIndex;
        public static void Init(Menu MenuParent)
        {
            MenuLocal = new Menu("Settings", "Settings").SetFontStyle(FontStyle.Regular, Color.Aqua);;
            MenuParent.AddSubMenu(MenuLocal);

            MenuSettingQ = new Menu("Humanizer Spell Cast", "SettingsSpellCast").SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.Q.MenuColor());
            {
                MenuSettingQ.AddItem(new MenuItem("Settings.SpellCast.Active", "Active: ").SetValue(false)).SetTooltip("Exp: Rengar / Shaco / Wukong / Kha'Zix / Vayne / Enemy Ganker from the bush").SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.Q.MenuColor());
                string[] strQ = new string[1000 / 250];
                for (float i = 250; i <= 1000; i += 250)
                {
                    strQ[(int) (i / 250 - 1)] = (i / 1000) + " sec. ";
                }
                MenuSettingQ.AddItem(new MenuItem("Settings.SpellCast.VisibleDelay", "Cast Delay: Instatly Visible Enemy").SetValue(new StringList(strQ, 2))).SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.Q.MenuColor());
                //MenuSettingQ.AddItem(new MenuItem("Settings.SpellCast.Clone", "Clone Cast: Wukong/Leblanc/Shaco Clone").SetValue(new StringList(new []{"Off", "Cast Q", "Cast W", "Cast E"}, 0))).SetTooltip("Exp: Shaco / Leblanc / Wukong").SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.Q.MenuColor());

                MenuSettingQ.AddItem(new MenuItem("Settings.SpellCast.Default", "Load Recommended Settings").SetValue(true)).SetFontStyle(FontStyle.Bold, Color.Wheat)
                    .ValueChanged += (sender, args) =>
                                {
                                    if (args.GetNewValue<bool>() == true)
                                    {
                                        LoadDefaultSettingsQ();
                                    }
                                };
            }
            MenuLocal.AddSubMenu(MenuSettingQ);

            
            MenuSettingE = new Menu("E Settings:", "MenuSettings.E").SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.W.MenuColor());
            int eRange = (int)PlayerSpells.E.Range;
            MenuSettingE.AddItem(new MenuItem("Settings.E.MaxRange", "E: Max. Rage [Default: 800]").SetValue(new Slider(eRange - 20, eRange/2, eRange + 50))).SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.W.MenuColor());
            MenuSettingE.AddItem(new MenuItem("Settings.E.Hitchance", "E:").SetValue(new StringList(new[] { "Hitchance = Very High", "Hitchance >= High", "Hitchance >= Medium", "Hitchance >= Low"}, 2)).SetFontStyle(FontStyle.Regular, Champion.PlayerSpells.W.MenuColor()));
            MenuLocal.AddSubMenu(MenuSettingE);

            MenuFlame = new Menu("Flame", "Flame");
            MenuFlame.AddItem(new MenuItem("Flame.Laugh", "After Kill:").SetValue(new StringList(new[] {"Off", "Joke", "Taunt", "Laugh", "Mastery Badge", "Random" }, 5)));

            Modes.ModeJump.Init(MenuLocal);
            
            
        }

        static void LoadDefaultSettingsQ()
        {
            string[] strQ = new string[1000 / 250];
            //for (var i = 250; i <= 1000; i += 250)
            //{
            //    str[i / 250 - 1] = i + " ms. ";
            //}
            for (float i = 250; i <= 1000; i += 250)
            {
                strQ[(int)(i / 250 - 1)] = (i / 100) + " sec. ";
            }
            MenuSettingQ.Item("Settings.SpellCast.VisibleDelay").SetValue(new StringList(strQ, 2));
            MenuSettingQ.Item("Settings.SpellCast.Active").SetValue(true);
            //MenuSettingQ.Item("Settings.SpellCast.Clone").SetValue(new StringList(new[] {"Off", "Cast Q", "Cast W", "Cast E"}, 3));
        }
    }
}
