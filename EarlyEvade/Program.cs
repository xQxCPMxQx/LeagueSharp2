using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace EarlyEvadeWarning
{
    internal class EarlyList
    {
        public string ChampionName { get; set; }
        public string SpellName { get; set; }

        public int Width { get; set; }
        public float Range { get; set; }
        public System.Drawing.Color Color { get; set; }
    }

    internal class Program
    {
        public static readonly List<EarlyList> EarlyList = new List<EarlyList>();
        public static Menu Config;
        public static Font Text;


        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public static Vector3 CenterOfVectors(Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            if (vectors == null || vectors.Length == 0)
                return sum;

            sum = vectors.Aggregate(sum, (current, vec) => current + vec);
            return sum/vectors.Length;
        }

        public static void DrawText(Font vFont, String vText, int vPosX, int vPosY, SharpDX.Color vColor)
        {
            vFont.DrawText(null, vText, vPosX + 2, vPosY + 2,
                vColor != SharpDX.Color.Black ? SharpDX.Color.Black : SharpDX.Color.White);
            vFont.DrawText(null, vText, vPosX, vPosY, vColor);
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural
                });

            Config = new Menu("Early Evade Warning", "Early Evade", true).SetFontStyle(FontStyle.Regular,
                SharpDX.Color.Aqua);

            Load();
            foreach (var e in HeroManager.Enemies)
            {
                foreach (var eList in EarlyList)
                {
                    if (eList.ChampionName == e.ChampionName)
                    {
                        var menuSub = new Menu(eList.ChampionName, eList.ChampionName);
                        var menuSubSpell = new Menu("Spell " + eList.SpellName, eList.SpellName);

                        menuSubSpell.AddItem(
                            new MenuItem(eList.ChampionName + eList.SpellName, "Active").SetValue(new Circle(true,
                                eList.Color))).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
                        menuSubSpell.AddItem(
                            new MenuItem(eList.ChampionName + "width", "Width").SetValue(new Slider(eList.Width,
                                eList.Width, 150)));
                        menuSubSpell.AddItem(
                            new MenuItem(eList.ChampionName + "range", "Range").SetValue(new Slider((int) eList.Range,
                                (int) eList.Range, 2000)));

                        menuSub.AddSubMenu(menuSubSpell);
                        Config.AddSubMenu(menuSub);
                    }

                    if (e.ChampionName == "Vayne")
                    {
                        var menuSub = new Menu("Vayne", "Vayne");
                        var menuSubSpell = new Menu("VayneE", "E Stun");
                        menuSubSpell.AddItem(
                            new MenuItem("Draw.VayneE", "Active:").SetValue(new Circle(true, eList.Color)))
                            .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
                        menuSub.AddSubMenu(menuSubSpell);
                        Config.AddSubMenu(menuSub);

                    }
                }
            }
            var menuDraw = new Menu("Drawings", "Drawings");
            {
                menuDraw.AddItem(new MenuItem("Draw.Line", "Draw Line").SetValue(true));
                menuDraw.AddItem(new MenuItem("Draw.Text", "Draw Champion Name").SetValue(true));
            }


            Config.AddItem(
                new MenuItem("Enabled", "Enabled!").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow)
                .Permashow(true, "Early Evade Warning");
            Config.AddSubMenu(menuDraw);
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat("Early Evade Warning System loaded!</font>");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }
            
            if (!Config.Item("Enabled").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (Config.Item("Draw.VayneE") != null && Config.Item("Draw.VayneE").GetValue<Circle>().Active)
            {
                foreach (
                    var e in
                        HeroManager.Enemies.Where(
                            e => e.ChampionName.ToLower() == "vayne" && e.Distance(ObjectManager.Player.Position) < 900))
                {
                    for (var i = 1; i < 8; i++)
                    {
                        var championBehind = ObjectManager.Player.Position
                                             + Vector3.Normalize(e.ServerPosition - ObjectManager.Player.Position)
                                             *(-i*50);
                        if (Config.Item("Draw.Line").GetValue<bool>())
                        {
                            Render.Circle.DrawCircle(championBehind, 35f,
                                championBehind.IsWall() ? System.Drawing.Color.Red : System.Drawing.Color.Gray, 3);
                        }
                    }
                }
            }


            foreach (var e in HeroManager.Enemies.Where(e => e.IsValidTarget(2000)))
            {
                foreach (var eList in EarlyList)
                {
                    if (eList.ChampionName == e.ChampionName)
                    {

                        var menuActive = Config.Item(eList.ChampionName + eList.SpellName).GetValue<Circle>();

                        if (menuActive.Active)
                        {
                            var xminions = 0;
                            if (e.IsValidTarget(eList.Range))
                            {
                                for (var i = 1;
                                    i < e.Position.Distance(ObjectManager.Player.Position)/eList.Width;
                                    i++)
                                {
                                    var championBehind = ObjectManager.Player.Position
                                                         + Vector3.Normalize(
                                                             e.ServerPosition - ObjectManager.Player.Position)
                                                         *(i*eList.Width);

                                    var list = eList;
                                    var allies =
                                        HeroManager.Allies.Where(
                                            a => a.Distance(ObjectManager.Player.Position) < list.Range);
                                    var minions = MinionManager.GetMinions(
                                        ObjectManager.Player.Position,
                                        eList.Range,
                                        MinionTypes.All,
                                        MinionTeam.Ally);
                                    var mobs = MinionManager.GetMinions(
                                        ObjectManager.Player.Position,
                                        eList.Range,
                                        MinionTypes.All,
                                        MinionTeam.Neutral);

                                    xminions += minions.Count(m => m.Distance(championBehind) < eList.Width)
                                                + allies.Count(a => a.Distance(championBehind) < eList.Width)
                                                + mobs.Count(m => m.Distance(championBehind) < eList.Width);
                                }

                                if (xminions == 0)
                                {
                                    if (Config.Item("Draw.Line").GetValue<bool>())
                                    {
                                        var rec = new Geometry.Polygon.Rectangle(ObjectManager.Player.Position,
                                            e.Position, eList.Width - 10);
                                        rec.Draw(eList.Color, 1);
                                    }

                                    if (Config.Item("Draw.Text").GetValue<bool>())
                                    {
                                        Vector3[] x = new[] {ObjectManager.Player.Position, e.Position};
                                        var aX =
                                            Drawing.WorldToScreen(
                                                new Vector3(
                                                    CenterOfVectors(x).X,
                                                    CenterOfVectors(x).Y,
                                                    CenterOfVectors(x).Z));
                                        DrawText(
                                            Text,
                                            vText: eList.ChampionName + " : " + eList.SpellName,
                                            vPosX: (int) aX.X - 15,
                                            vPosY: (int) aX.Y - 15,
                                            vColor: SharpDX.Color.GreenYellow);
                                    }
                                }
                            }
                        }
                    }
                }
            }


        }

        private static void Load()
        {
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "LeBlanc",
                    SpellName = "E",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Morgana",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Blitzcrank",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Amumu",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Braum",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Ezreal",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Brand",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Jinx",
                    SpellName = "W",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Braum",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Corki",
                    SpellName = "R",
                    Width = 75,
                    Range = 1500,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Mundo",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Kalista",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Karma",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Kogmaw",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "LeeSin",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Lux",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Nautilius",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1000,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Nidalee",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1500,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Quinn",
                    SpellName = "Q",
                    Width = 75,
                    Range = 850,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Rumble",
                    SpellName = "E",
                    Width = 75,
                    Range = 850,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "TahmKench",
                    SpellName = "Q",
                    Width = 75,
                    Range = 850,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Thresh",
                    SpellName = "Q",
                    Width = 75,
                    Range = 1200,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Veigar",
                    SpellName = "Q",
                    Width = 75,
                    Range = 900,
                    Color = System.Drawing.Color.AliceBlue
                });
            EarlyList.Add(
                new EarlyList
                {
                    ChampionName = "Zyra",
                    SpellName = "E",
                    Width = 75,
                    Range = 900,
                    Color = System.Drawing.Color.AliceBlue
                });
        }
    }
}
