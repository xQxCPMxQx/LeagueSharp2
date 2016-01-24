using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Olafisback.Properties;
using LeagueSharp;
using LeagueSharp.Common;

namespace Olafisback
{
    using Properties;

    using SharpDX;
    using SharpDX.Direct3D9;

    internal enum TargetSelect
    {
        Olaf,
        LeagueSharp
    }

    public class Captions
    {
        public static string MenuTab => "    ";
    }

    internal class AssassinManager
    {
        public static Menu LocalMenu;
        public static Font Text;

        private static TargetSelect Selector => LocalMenu.Item("TS").GetValue<StringList>().SelectedIndex == 0
            ? TargetSelect.Olaf
            : TargetSelect.LeagueSharp;

        public void Initialize()
        {
            new Render.Sprite(Resources.selectedchampion, new Vector2())
            {
                PositionUpdate = () => DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();
            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Malgun Gothic",
                    Height = 21,
                    OutputPrecision = FontPrecision.Default,
                    Weight = FontWeight.Bold,
                    Quality = FontQuality.ClearTypeNatural
                });

            LocalMenu = new Menu("Target Selector", "AssassinTargetSelector").SetFontStyle(
                FontStyle.Regular,
                SharpDX.Color.Cyan);

            var menuTargetSelector = new Menu("Target Selector", "TargetSelector");
            {
                TargetSelector.AddToMenu(menuTargetSelector);
            }

            LocalMenu.AddItem(
                new MenuItem("TS", "Active Target Selector:").SetValue(
                    new StringList(new[] {"Olaf Target Selector", "L# Target Selector"})))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow)
                .ValueChanged += (sender, args) =>
                {
                    LocalMenu.Items.ForEach(
                        i =>
                        {
                            i.Show();
                            switch (args.GetNewValue<StringList>().SelectedIndex)
                            {
                                case 0:
                                {
                                    if (i.Tag == 22)
                                    {
                                        i.Show(false);
                                    }
                                    break;
                                }

                                case 1:
                                {
                                    if (i.Tag == 11 || i.Tag == 12)
                                    {
                                        i.Show(false);
                                    }
                                    break;
                                }
                            }
                        });
                };

            menuTargetSelector.Items.ForEach(i =>
            {
                LocalMenu.AddItem(i);
                i.SetTag(22);
            });

            LocalMenu.AddItem(
                new MenuItem("Set", "Target Select Mode:").SetValue(
                    new StringList(new[] {"Single Target Select", "Multi Target Select"})))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.LightCoral)
                .SetTag(11);
            LocalMenu.AddItem(new MenuItem("Range", "Range (Recommend: Max):"))
                .SetValue(new Slider((int) (Program.Q.Range*1.5), (int) Program.Q.Range, (int) (Program.Q.Range*2)))
                .SetTag(11);

            LocalMenu.AddItem(
                new MenuItem("Targets", "Targets:").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            foreach (Obj_AI_Hero e in HeroManager.Enemies)
            {
                LocalMenu.AddItem(
                    new MenuItem("enemy_" + e.ChampionName, $"{Captions.MenuTab}Focus {e.ChampionName}")
                        .SetValue(false)).SetTag(12);

            }
            //foreach (var langItem in HeroManager.Enemies.Select(e =>LocalMenu.AddItem(new MenuItem("enemy_" + e.ChampionName,string.Format("{0}Focus {1}", GameUtils.MenuTab, e.ChampionName)).SetValue(false)).SetTag(12)))

            LocalMenu.AddItem(
                new MenuItem("Draw.Title", "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            LocalMenu.AddItem(
                new MenuItem("Draw.Range", Captions.MenuTab + "Range").SetValue(new Circle(true,
                    System.Drawing.Color.Gray)).SetTag(11));
            LocalMenu.AddItem(
                new MenuItem("Draw.Enemy", Captions.MenuTab + "ActiveJungle Enemy").SetValue(new Circle(true,
                    System.Drawing.Color.GreenYellow)).SetTag(11));
            LocalMenu.AddItem(
                new MenuItem("Draw.Status", Captions.MenuTab + "Show Enemy:").SetValue(
                    new StringList(new[] {"Off", "Text", "Picture", "Line", "All"}, 0)));
            Program.Config.AddSubMenu(LocalMenu);

            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;

            RefreshMenuItemsStatus();
        }

        private void RefreshMenuItemsStatus()
        {

            LocalMenu.Items.ForEach(
                i =>
                {
                    i.Show();
                    switch (Selector)
                    {
                        case TargetSelect.Olaf:
                            if (i.Tag == 22)
                            {
                                i.Show(false);
                            }
                            break;
                        case TargetSelect.LeagueSharp:
                            if (i.Tag == 11)
                            {
                                i.Show(false);
                            }
                            break;
                    }
                });
        }

        public void ClearAssassinList()
        {
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                LocalMenu.Item("enemy_" + enemy.ChampionName).SetValue(false);
            }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (Selector != TargetSelect.Olaf)
            {
                return;
            }

            if (args.Msg == 0x201)
            {
                foreach (var objAiHero in from hero in HeroManager.Enemies
                    where
                        hero.Distance(Game.CursorPos) < 150f && hero != null && hero.IsVisible && !hero.IsDead
                    orderby
                        hero.Distance(Game.CursorPos) descending
                    select
                        hero)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        int set = Program.Config.Item("Set").GetValue<StringList>().SelectedIndex;

                        switch (set)
                        {
                            case 0:
                            {
                                ClearAssassinList();
                                Program.Config.Item("enemy_" + objAiHero.ChampionName).SetValue(true);
                                break;
                            }
                            case 1:
                            {
                                var menuStatus =
                                    Program.Config.Item("enemy_" + objAiHero.ChampionName).GetValue<bool>();
                                Program.Config.Item("enemy_" + objAiHero.ChampionName).SetValue(!menuStatus);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static bool In<T>(T source, params T[] list)
        {
            return list.Equals(source);
        }

        public static bool NotIn<T>(T source, params T[] list)
        {
            return !list.Equals(source);
        }

        public static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical,
            IEnumerable<Obj_AI_Hero> ignoredChamps = null)
        {
            //if (Selector != TargetSelect.Olaf)
            //{
            //    return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);
            //}

            vDefaultRange = Math.Abs(vDefaultRange) < 0.00001
                ? Program.Q.Range
                : LocalMenu.Item("Range").GetValue<Slider>().Value;

            if (ignoredChamps == null)
            {
                ignoredChamps = new List<Obj_AI_Hero>();
            }

            var vEnemy =
                HeroManager.Enemies.FindAll(hero => ignoredChamps.All(ignored => ignored.NetworkId != hero.NetworkId))
                    .Where(e => e.IsValidTarget(vDefaultRange))
                    .Where(e => LocalMenu.Item("enemy_" + e.ChampionName) != null)
                    .Where(e => LocalMenu.Item("enemy_" + e.ChampionName).GetValue<bool>())
                    .Where(e => ObjectManager.Player.Distance(e) < vDefaultRange);

            if (LocalMenu.Item("Set").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any() ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType) : objAiHeroes[0];

            return t;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            var drawEnemy = LocalMenu.Item("Draw.Enemy").GetValue<Circle>();
            if (drawEnemy.Active)
            {
                var t = GetTarget(Program.Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    Render.Circle.DrawCircle(t.Position, (float) (t.BoundingRadius*1.5), drawEnemy.Color);
                }
            }

            if (Selector != TargetSelect.Olaf)
            {
                return;
            }

            Circle rangeColor = LocalMenu.Item("Draw.Range").GetValue<Circle>();
            int range = LocalMenu.Item("Range").GetValue<Slider>().Value;
            if (rangeColor.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, range, rangeColor.Color);
            }

            int drawStatus = LocalMenu.Item("Draw.Status").GetValue<StringList>().SelectedIndex;
            if (drawStatus == 1 || drawStatus == 4)
            {
                foreach (
                    var e in
                        HeroManager.Enemies.Where(
                            e =>
                                e.IsVisible && !e.IsDead && LocalMenu.Item("enemy_" + e.ChampionName) != null &&
                                LocalMenu.Item("enemy_" + e.ChampionName).GetValue<bool>()))
                {
                    DrawText(Text, "1st Priority Target",
                        e.HPBarPosition.X + e.BoundingRadius/2f - (e.CharData.BaseSkinName.Length/2f) - 27,
                        e.HPBarPosition.Y - 23, SharpDX.Color.Black);

                    DrawText(Text, "1st Priority Target",
                        e.HPBarPosition.X + e.BoundingRadius/2f - (e.CharData.BaseSkinName.Length/2f) - 29,
                        e.HPBarPosition.Y - 25, SharpDX.Color.IndianRed);
                }
            }

            if (drawStatus == 3 || drawStatus == 4)
            {
                foreach (
                    LeagueSharp.Common.Geometry.Polygon.Line line in
                        HeroManager.Enemies.Where(
                            e =>
                                e.IsVisible && !e.IsDead && LocalMenu.Item("enemy_" + e.ChampionName) != null &&
                                LocalMenu.Item("enemy_" + e.ChampionName).GetValue<bool>())
                            .Select(
                                e =>
                                    new LeagueSharp.Common.Geometry.Polygon.Line(ObjectManager.Player.Position,
                                        e.Position,
                                        ObjectManager.Player.Distance(e.Position))))
                {
                    line.Draw(System.Drawing.Color.Wheat, 2);
                }
            }

        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }

        private Vector2 DrawPosition
        {
            get
            {
                var drawStatus = LocalMenu.Item("Draw.Status").GetValue<StringList>().SelectedIndex;
                if (KillableEnemy == null || (drawStatus != 2 && drawStatus != 4)) return new Vector2(0f, 0f);

                return new Vector2(
                    KillableEnemy.HPBarPosition.X + KillableEnemy.BoundingRadius/2f,
                    KillableEnemy.HPBarPosition.Y - 70);
            }
        }

        private bool DrawSprite => true;

        private Obj_AI_Hero KillableEnemy
        {
            get
            {
                Obj_AI_Hero t = GetTarget(Program.Q.Range);

                return t.IsValidTarget() ? t : null;
            }
        }
    }
}