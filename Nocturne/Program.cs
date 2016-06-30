using LeagueSharp;

namespace Nocturne
{
    internal class Program
    {
        public static string ChampionName => "Einstein Exory";
        private static void Main(string[] args)
        {
            if (ObjectManager.Player.ChampionName == "Nocturne")
            {
                Nocturne.Init();
            }
        }
    }
}
