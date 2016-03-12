using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;

namespace Nocturne.Common
{
    internal class MobManager
    {
        public static Menu LocalMenu { get; private set; }
        public static Menu SubMenuBuffs { get; private set; }

        private static readonly List<JungleCamp> JungleMobs = new List<JungleCamp>();

        private static SharpDX.Direct3D9.Font mapFont;
        private static SharpDX.Direct3D9.Font miniMapFont;
        private static int _nextTime;


        public static void Initialize(Menu mainMenu)
        {
            LocalMenu = new Menu("Buff Manager", "BuffManager").SetFontStyle(FontStyle.Regular, Color.Aquamarine);
            {
                string[] timeRemaining = new[]
                {"Off", "10 secs. remain to respawn", "20 secs. remain to respawn", "30 secs. remain to respawn"};

                SubMenuBuffs = new Menu("Buffs", "BuffManager.Buffs");
                {
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.Active", "Active: Warned Me! ").SetValue(
                            new StringList(new[] {"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, Color.GreenYellow));
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.AllyBlue", "Blue: Ally").SetValue(new StringList(timeRemaining, 2))
                            .SetFontStyle(FontStyle.Regular, Color.Aqua));
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.EnemyBlue", "Blue: Enemy").SetValue(new StringList(timeRemaining, 2))
                            .SetFontStyle(FontStyle.Regular, Color.IndianRed));
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.AllyRed", "Red: Ally").SetValue(new StringList(timeRemaining, 2))
                            .SetFontStyle(FontStyle.Regular, Color.Aqua));
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.AllyEnemy", "Red: Enemy").SetValue(new StringList(timeRemaining, 2))
                            .SetFontStyle(FontStyle.Regular, Color.IndianRed));
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.Dragon", "Dragon").SetValue(new StringList(timeRemaining, 2))
                            .SetFontStyle(FontStyle.Regular, Color.Coral));
                    SubMenuBuffs.AddItem(
                        new MenuItem("BuffManager.Dragon", "Baron").SetValue(new StringList(timeRemaining, 2))
                            .SetFontStyle(FontStyle.Regular, Color.DeepPink));
                    LocalMenu.AddSubMenu(SubMenuBuffs);
                }

                LocalMenu.AddItem(
                    new MenuItem("BuffManager.JungleTimerFormat", "Display Format").SetValue(
                        new StringList(new[] {"m:ss", "ss"})));

                LocalMenu.AddItem(new MenuItem("JungleActive", "Jungle Timer").SetValue(true));

                JungleMobs.Add(new JungleCamp("SRU_Blue", 300, new Vector3(3871.489f, 7901.054f, 51.90324f),
                    new[] {"SRU_Blue1.1.1", "SRU_BlueMini1.1.2", "SRU_BlueMini21.1.3"}));

                JungleMobs.Add(new JungleCamp("SRU_Red", 300, new Vector3(7862f, 4112f, 53.71951f),
                    new[] {"SRU_Red4.1.1", "SRU_RedMini4.1.2", "SRU_RedMini4.1.3"}));

                JungleMobs.Add(new JungleCamp("SRU_Dragon", 360, new Vector3(9866.148f, 4414.014f, -71.2406f),
                    new[] {"SRU_Dragon6.1.1"}));

                JungleMobs.Add(new JungleCamp("SRU_Blue", 300, new Vector3(10931.73f, 6990.844f, 51.72291f),
                    new[] {"SRU_Blue7.1.1", "SRU_BlueMini7.1.2", "SRU_BlueMini27.1.3"}));

                JungleMobs.Add(new JungleCamp("SRU_Red", 300, new Vector3(7016.869f, 10775.55f, 56.00922f),
                    new[] {"SRU_Red10.1.1", "SRU_RedMini10.1.2", "SRU_RedMini10.1.3"}));

                JungleMobs.Add(new JungleCamp("SRU_Baron", 420, new Vector3(5007.124f, 10471.45f, -71.2406f),
                    new[] {"SRU_Baron12.1.1"}));

                mapFont = new Font(Drawing.Direct3DDevice, new System.Drawing.Font("Calibri", 16));
                miniMapFont = new Font(Drawing.Direct3DDevice, new System.Drawing.Font("Calibri", 8));
            }
            mainMenu.AddSubMenu(LocalMenu);
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (LocalMenu.Item("BuffManager.Active").GetValue<StringList>().SelectedIndex == 0)
            {
                return;
            }

            if ((int) Game.ClockTime - _nextTime >= 0)
            {
                _nextTime = (int) Game.ClockTime + 1;
                IEnumerable<Obj_AI_Base> minions =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(minion => !minion.IsDead && minion.IsValid && minion.Name.ToUpper().StartsWith("SRU"));

                IEnumerable<JungleCamp> junglesAlive =
                    JungleMobs.Where(
                        jungle =>
                            !jungle.IsDead &&
                            jungle.Names.Any(
                                s =>
                                    minions.Where(minion => minion.Name == s)
                                        .Select(minion => minion.Name)
                                        .FirstOrDefault() != null));
                foreach (JungleCamp jungle in junglesAlive)
                {
                    jungle.Visibled = true;
                }

                IEnumerable<JungleCamp> junglesDead =
                    JungleMobs.Where(
                        jungle =>
                            !jungle.IsDead && jungle.Visibled &&
                            jungle.Names.All(
                                s =>
                                    minions.Where(minion => minion.Name == s)
                                        .Select(minion => minion.Name)
                                        .FirstOrDefault() == null));

                foreach (JungleCamp jungle in junglesDead)
                {
                    jungle.IsDead = true;
                    jungle.Visibled = false;
                    jungle.NextRespawnTime = (int) Game.ClockTime + jungle.RespawnTime;
                }

                foreach (JungleCamp jungleCamp in
                    JungleMobs.Where(jungleCamp => (jungleCamp.NextRespawnTime - (int) Game.ClockTime) <= 0))
                {
                    jungleCamp.IsDead = false;
                    jungleCamp.NextRespawnTime = 0;
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (LocalMenu.Item("BuffManager.Active").GetValue<StringList>().SelectedIndex == 0)
            {
                return;
            }

            foreach (JungleCamp jungleCamp in JungleMobs.Where(camp => camp.NextRespawnTime > 0))
            {
                int timeClock = jungleCamp.NextRespawnTime - (int) Game.ClockTime;
                string time = LocalMenu.Item("BuffManager.JungleTimerFormat").GetValue<StringList>().SelectedIndex == 0
                    ? Helper.FormatTime(timeClock)
                    : timeClock.ToString(CultureInfo.InvariantCulture);

                Vector2 pos = Drawing.WorldToScreen(jungleCamp.Position);
                Helper.DrawText(mapFont, time, (int) pos.X, (int) pos.Y - 15, Color.White);
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (LocalMenu.Item("BuffManager.Active").GetValue<StringList>().SelectedIndex == 0)
            {
                return;
            }

            foreach (JungleCamp jungleCamp in JungleMobs.Where(camp => camp.NextRespawnTime > 0))
            {
                int timeClock = jungleCamp.NextRespawnTime - (int) Game.ClockTime;
                string time = LocalMenu.Item("BuffManager.JungleTimerFormat").GetValue<StringList>().SelectedIndex == 0
                    ? Helper.FormatTime(timeClock)
                    : timeClock.ToString(CultureInfo.InvariantCulture);

                Vector2 pos = Drawing.WorldToMinimap(jungleCamp.Position);
                Helper.DrawText(miniMapFont, time, (int) pos.X, (int) pos.Y - 8, Color.White);
            }
        }
    }

    public class JungleCamp
    {
        public bool IsDead;
        public string Name;
        public string[] Names;
        public int NextRespawnTime;
        public Vector3 Position;
        public int RespawnTime;
        public bool Visibled;

        public JungleCamp(string name, int respawnTime, Vector3 position, string[] names)
        {
            Name = name;
            RespawnTime = respawnTime;
            Position = position;
            Names = names;
            IsDead = false;
            Visibled = false;
        }
    }
}