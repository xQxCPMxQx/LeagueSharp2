using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace xQxUtilities
{
    class UltCooldown
    {
        public static Menu Config;

        public static Dictionary<string, string> Language = new Dictionary<string, string>();
        public static string DefaultLanguage;

        public static double TimeUltReady;
        public static double TimeCooldown;

        private static int rnd;

        public static Spell R;

        static UltCooldown()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;

        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            R = new Spell(SpellSlot.R);
            TimeUltReady = (int)Game.Time;
            TimeCooldown = (int)Game.Time;

            Config = new Menu("xQx | Utilities", "JunglePosition", true);

            /* [ Settings ] */
            var menuUlt = new Menu("Tell Ult Cooldown", "TellUltCooldown");

            Config.AddSubMenu(menuUlt);

            var menuSet = new Menu("Settings", "Settings");
            menuSet.AddItem(new MenuItem("TellCooldown", "Tell Cooldown").SetValue(true));
            menuSet.AddItem(
                new MenuItem("MinCD", "Min. Cooldown").SetValue(new StringList(new[] {"5", "10", "15", "20"})));
            menuUlt.AddSubMenu(menuSet);

            menuUlt.AddItem(new MenuItem("TellNow", "Tell Now!").SetValue(new KeyBind("J".ToCharArray()[0],
                KeyBindType.Press)));

            /* [ Language List ]*/
            LoadLanguageList();
            var languageMenu = new Menu("Language", "Language", false);
            foreach (KeyValuePair<string, string> lang in Language)
            {
                var langItem = languageMenu.AddItem(new MenuItem(lang.Key, lang.Key).SetValue(false));

                KeyValuePair<string, string> lang1 = lang;
                langItem.ValueChanged += (sender, argsEvent) =>
                {
                    if (argsEvent.GetNewValue<bool>())
                    {
                        languageMenu.Items.ForEach(
                            x =>
                            {
                                if (x.GetValue<bool>() && x.Name != lang1.Key)
                                    x.SetValue(false);
                            });
                        DefaultLanguage = lang1.Key;
                        Game.PrintChat(string.Format("Default Language: <font color='#FFF9C200'>{0}</font>", lang1.Key));
                    }
                };
            }

            menuUlt.AddSubMenu(languageMenu);
            Config.AddToMainMenu();
            Game.OnGameUpdate += Game_OnGameUpdate;

            foreach (var menuItem in
                    from menuItem in languageMenu.Items where menuItem.GetValue<bool>()
                    from lang in Language where menuItem.Name == lang.Key
                select menuItem)
            {
                DefaultLanguage = menuItem.Name;
                
            }
            
            if (DefaultLanguage == null)
            { 
                Game.PrintChat("laksjdlaskjdlaskjd");
                DefaultLanguage = "English";
                languageMenu.Item("English").SetValue(true);
            }

            Game.PrintChat(
                string.Format("Default Language: <font color='#FFF9C200'>{0}</font> You can choose your language.",
                    DefaultLanguage));

        }

        private static void LoadLanguageList()
        {
            Language.Add("Deutsch", "{0} sekunden Ulti");
            Language.Add("English", "{0} seconds Ulti");
            Language.Add("Espanol", "{0} segundos Ulti");
            Language.Add("Francais", "{0} secondes Ulti");
            Language.Add("Italian", "{0} fino all'Ulti");
            Language.Add("Portugues", "{0} segundo Ulti");
            Language.Add("Romanian", "{0} pana la Ulti");
            Language.Add("Russian", "{0} секунд");
            Language.Add("Turkce", "{0} saniye Ulti");
            Language.Add("Chinese", "{0} 秒極致");
        }

        public static int GetRandomNumber
        {
            get
            {
                var xMinCd = Config.Item("MinCD").GetValue<Slider>().Value;
                var xMaxCd = Config.Item("MaxCD").GetValue<Slider>().Value;

                var random = new Random();
                return random.Next(xMinCd, xMaxCd + 1);
            }
        }

        static float CooldownR
        {
            get
            {
                var cdREx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).CooldownExpires;
                var xCd = Game.Time < cdREx ? cdREx - Game.Time : 0;
                return xCd;
            }
        }

        private static void SayNow(string xTime)
        {
            if (R.Level == 0)
                return;

            foreach (var lang in from lang in Language.Where(lang => lang.Key == DefaultLanguage)
                let xGameTime = (int) Game.Time
                where TimeCooldown < xGameTime
                select lang) 
            {
                if (xTime == "UltUp")
                    Game.Say("Ult up!");
                else
                    Game.Say(string.Format(lang.Value, xTime));
                TimeCooldown = (int)Game.Time;
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;
            if (Config.Item("TellNow").GetValue<KeyBind>().Active)
            {
                var cdR = (int)CooldownR;
                SayNow(cdR == 0 ? "UltUp" : cdR.ToString());
            }


            if (Config.Item("TellCooldown").GetValue<bool>() && !R.IsReady() && R.Level > 0)
            {
                var cdR = (int)CooldownR;
                var xIndex = Config.Item("MinCD").GetValue<StringList>().SelectedIndex;
                xIndex += 1;
                xIndex *= 5;

                if (cdR > 0 && cdR <= xIndex && TimeCooldown + xIndex < (int)Game.Time)
                    SayNow(xIndex.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
