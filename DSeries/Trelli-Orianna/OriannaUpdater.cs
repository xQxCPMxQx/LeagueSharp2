using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using LeagueSharp;

namespace Orianna
{
    class OriannaUpdater
    {
        private const int localversion = 10;
        internal static bool isInitialized;

        internal static void InitializeOrianna()
        {
            isInitialized = true;
            UpdateCheck();
        }

        private static void UpdateCheck()
        {
            Game.PrintChat("<font color='#33FFFF'> .: Orianna by trelli loaded :.");
            var bgw = new BackgroundWorker();
            bgw.DoWork += bgw_DoWork;
            bgw.RunWorkerAsync();
        }

        private static void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            var myUpdater = new Updater("https://raw.githubusercontent.com/trelli/LeagueSharp/stable/Orianna/Version/Orianna.version",
                    "https://github.com/trelli/LeagueSharp/raw/stable/Orianna/Release/Orianna.exe", localversion);
            if (myUpdater.NeedUpdate)
            {
                Game.PrintChat("<font color='#33FFFF'> .: Orianna - Master of Clockwork: Updating ...");
                if (myUpdater.Update())
                {
                    Game.PrintChat("<font color='#33FFFF'> .: Orianna - Master of Clockwork: Update complete, reload please.");
                }
            }
            else
            {
                Game.PrintChat("<font color='#33FFFF'> .: Orianna - Master of Clockwork: Most recent version ({0}) loaded!", localversion);
            }
        }
    }

    internal class Updater
    {
        private readonly string _updatelink;

        private readonly System.Net.WebClient _wc = new System.Net.WebClient { Proxy = null };
        public bool NeedUpdate = false;

        public Updater(string versionlink, string updatelink, int localversion)
        {
            _updatelink = updatelink;

            NeedUpdate = Convert.ToInt32(_wc.DownloadString(versionlink)) > localversion;
        }

        public bool Update()
        {
            try
            {
                if (
                    System.IO.File.Exists(
                        System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".bak"))
                {
                    System.IO.File.Delete(
                        System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".bak");
                }
                System.IO.File.Move(System.Reflection.Assembly.GetExecutingAssembly().Location,
                    System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".bak");
                _wc.DownloadFile(_updatelink,
                    System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location));
                return true;
            }
            catch (Exception ex)
            {
                Game.PrintChat("<font color='#33FFFF'> .: Orianna - Master of Clockwork Updater: " + ex.Message);
                return false;
            }
        }
    }
}
