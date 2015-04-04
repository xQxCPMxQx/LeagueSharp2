using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using SharpDX;

namespace KaiHelper
{
    class ResourceImages
    {
        private const string ImagePath = "http://ddragon.leagueoflegends.com/cdn/5.4.1/img/";
        private const string ChampionImageDownloadPath = ImagePath + "champion/";
        private static readonly string ChampionImageSavePath = String.Format("{0}\\ChampionImages\\", Config.AppDataDirectory);

        static ResourceImages()
        {
            Console.WriteLine("Init folder");
            if (Directory.Exists(ChampionImageSavePath))
            {
                Console.WriteLine("Created: " + ChampionImageSavePath);
                Directory.CreateDirectory(ChampionImageSavePath);
            }
        }
        public static Bitmap GetChampionSquare(string championName)
        {
            Bitmap result=null;
            try
            {
                
                if (File.Exists(string.Format("{0}{1}.png", ChampionImageSavePath, championName)))
                {
                    return new Bitmap(string.Format("{0}{1}.png", ChampionImageSavePath, championName));
                }
                result = Helper.CropCircleImage(DownloadChampionSquare(championName) ?? DownloadChampionSquare("Aatrox"));
                Console.WriteLine("{0}{1}.png", ChampionImageSavePath, championName);
                result.Save(string.Format("{0}{1}.png", ChampionImageSavePath, championName), ImageFormat.Png);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }
        private static Bitmap DownloadChampionSquare(string championName)
        {
            Bitmap result = new Bitmap(Helper.Download(string.Format("{0}{1}.png", ChampionImageDownloadPath, championName)));
            return result;
        }
    }
}
