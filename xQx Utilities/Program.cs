using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace xQxUtilities
{
    class Program
    {
        static void Main(string[] args)
        {
            var ultCooldown = new UltCooldown();
            WelcomeMessage();
        }

        private static void WelcomeMessage()
        {
            Game.PrintChat("<font color='#70DBDB'>xQx Utilities | Loaded!</font>");
        }
    }
}