using System.Collections.Generic;
using System.Drawing;
using LeagueSharp.Common;
using Color = SharpDX.Color;

namespace Marksman.Utils
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using LeagueSharp;

    using SharpDX;
    using SharpDX.Direct3D9;

    internal partial class EarlyList
    {
        public string ChampionName { get; set; }
        public string SpellName { get; set; }

        public int Width { get; set; }
        public float Range { get; set; }
        public System.Drawing.Color Color { get; set; }
    }

    internal class EarlyEvade
    {
        public readonly List<EarlyList> EarlyList = new List<EarlyList>();
        public static Menu MenuLocal;
        public EarlyEvade()
        {
            Load();
            
            MenuLocal = new Menu("Early Evade Warning", "Early Evade").SetFontStyle(FontStyle.Regular, Color.Aqua);

            foreach (var e in HeroManager.Enemies)
            {
                foreach (var eList in EarlyList)
                {
                    if (eList.ChampionName == e.ChampionName)
                    {
                        var menuSub = new Menu(eList.ChampionName, eList.ChampionName);
                        var menuSubSpell = new Menu("Spell " + eList.SpellName, eList.SpellName);

                        menuSubSpell.AddItem(new MenuItem(eList.ChampionName + eList.SpellName, "Active").SetValue(new Circle(true, eList.Color))).SetFontStyle(FontStyle.Regular, Color.Aqua);
                        menuSubSpell.AddItem(new MenuItem(eList.ChampionName + "width", "Width").SetValue(new Slider(eList.Width, eList.Width, 150)));
                        menuSubSpell.AddItem(new MenuItem(eList.ChampionName + "range", "Range").SetValue(new Slider((int)eList.Range, (int)eList.Range, 2000)));
                        
                        menuSub.AddSubMenu(menuSubSpell);
                        MenuLocal.AddSubMenu(menuSub);
                    }

                    if (e.ChampionName == "Vayne")
                    {
                        var menuSub = new Menu("Vayne", "Vayne");
                        var menuSubSpell = new Menu("VayneE", "E Stun");
                        menuSubSpell.AddItem(new MenuItem("Draw.VayneE", "Active:").SetValue(new Circle(true, eList.Color))).SetFontStyle(FontStyle.Regular, Color.Aqua);
                        menuSub.AddSubMenu(menuSubSpell);
                        MenuLocal.AddSubMenu(menuSub);

                    }
                }
            }
            var menuDraw = new Menu("Drawings", "Drawings");
            {
                menuDraw.AddItem(new MenuItem("Draw.Line", "Draw Line").SetValue(true));
                menuDraw.AddItem(new MenuItem("Draw.Text", "Draw Champion Name").SetValue(true));
            }
            MenuLocal.AddSubMenu(menuDraw);

            MenuLocal.AddItem(new MenuItem("Enabled", "Enabled!").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle))).SetFontStyle(FontStyle.Regular, Color.GreenYellow).Permashow(true, "Marksman| Early Evade Warning");

            Drawing.OnDraw += Drawing_OnDraw;

        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!MenuLocal.Item("Enabled").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (MenuLocal.Item("Draw.VayneE") != null && MenuLocal.Item("Draw.VayneE").GetValue<Circle>().Active)
            {
                foreach (var e in HeroManager.Enemies.Where(e => e.ChampionName.ToLower() == "udyr" && e.Distance(ObjectManager.Player.Position) < 900))
                {
                    for (var i = 1; i < 8; i++)
                    {
                        var championBehind = ObjectManager.Player.Position
                                             + Vector3.Normalize(e.ServerPosition - ObjectManager.Player.Position)
                                             * (-i * 50);
                        if (MenuLocal.Item("Draw.Line").GetValue<bool>())
                        {
                            Render.Circle.DrawCircle(championBehind, 35f, championBehind.IsWall() ? System.Drawing.Color.Red : System.Drawing.Color.Gray, 3);
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
                        
                        var menuActive = MenuLocal.Item(eList.ChampionName + eList.SpellName).GetValue<Circle>();

                        if (menuActive.Active)
                        {
                            var xminions = 0;
                            if (e.IsValidTarget(eList.Range))
                            {
                                for (var i = 1;
                                     i < e.Position.Distance(ObjectManager.Player.Position) / eList.Width;
                                     i++)
                                {
                                    var championBehind = ObjectManager.Player.Position
                                                         + Vector3.Normalize(
                                                             e.ServerPosition - ObjectManager.Player.Position)
                                                         * (i * eList.Width);

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
                                    if (MenuLocal.Item("Draw.Line").GetValue<bool>())
                                    {
                                        var rec = new Geometry.Polygon.Rectangle( ObjectManager.Player.Position,e.Position, eList.Width - 10);
                                        rec.Draw(eList.Color, 1);
                                    }

                                    if (MenuLocal.Item("Draw.Text").GetValue<bool>())
                                    {
                                        Vector3[] x = new[] { ObjectManager.Player.Position, e.Position };
                                        var aX =
                                            Drawing.WorldToScreen(
                                                new Vector3(
                                                    Utils.CenterOfVectors(x).X,
                                                    Utils.CenterOfVectors(x).Y,
                                                    Utils.CenterOfVectors(x).Z));
                                        Utils.DrawText(
                                            Utils.Text,
                                            vText: eList.ChampionName + " : " + eList.SpellName,
                                            vPosX: (int)aX.X - 15,
                                            vPosY: (int)aX.Y - 15,
                                            vColor: Color.GreenYellow);
                                    }
                                    //Drawing.DrawText(aX.X - 15,aX.Y - 15,System.Drawing.Color.GreenYellow, format: eList.ChampionName + " : " + eList.SpellName);
                                }
                            }
                        }
                    }
                }
            }

        }

        private void Load()
        {
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "LeBlanc", SpellName = "E", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Morgana", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Blitzcrank", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Amumu", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Braum", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Ezreal", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Brand", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Jinx", SpellName = "W", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Braum", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Corki", SpellName = "R", Width = 75, Range = 1500,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Mundo", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Kalista", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Karma", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Kogmaw", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "LeeSin", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Lux", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Nautilius", SpellName = "Q", Width = 75, Range = 1000,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Nidalee", SpellName = "Q", Width = 75, Range = 1500,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Quinn", SpellName = "Q", Width = 75, Range = 850,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Rumble", SpellName = "E", Width = 75, Range = 850,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "TahmKench", SpellName = "Q", Width = 75, Range = 850,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Thresh", SpellName = "Q", Width = 75, Range = 1200,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Veigar", SpellName = "Q", Width = 75, Range = 900,
                        Color = System.Drawing.Color.AliceBlue
                    });
            EarlyList.Add(
                new EarlyList
                    {
                        ChampionName = "Zyra", SpellName = "E", Width = 75, Range = 900,
                        Color = System.Drawing.Color.AliceBlue
                    });
        }
    }
}
