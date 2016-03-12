using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Nocturne.Properties;

namespace Nocturne.Common
{
    using System.Collections.Generic;
    using System.Xml;

    using SharpDX;
    using SharpDX.Direct3D9;

    using Color = System.Drawing.Color;

    enum TargetSelect
    {
        Nocturne,
        LeagueSharp
    }

    internal class Sprite
    {
        private static Spell Q => PlayerSpells.Q;

        private static Vector2 DrawPosition
        {
            get
            {
                var drawStatus = AssassinManager.LocalMenu.Item("Draw.Status").GetValue<StringList>().SelectedIndex;
                if (KillableEnemy == null || (drawStatus != 2 && drawStatus != 3))
                    return new Vector2(0f, 0f);

                return new Vector2(KillableEnemy.HPBarPosition.X + KillableEnemy.BoundingRadius/2f,
                    KillableEnemy.HPBarPosition.Y - 70);
            }
        }

        private static bool DrawSprite
        {
            get { return true; }
        }

        private static Obj_AI_Hero KillableEnemy
        {
            get
            {
                var t = AssassinManager.GetTarget(Q.Range);

                if (t.IsValidTarget())
                    return t;

                return null;
            }
        }

        internal static void Initialize()
        {
            new Render.Sprite(Resources.selectedchampion, new Vector2())
            {
                PositionUpdate = () => DrawPosition,
                Scale = new Vector2(1f, 1f),
                VisibleCondition = sender => DrawSprite
            }.Add();
        }
    }

    internal class AssassinManager
    {
        public static Menu LocalMenu;
        public static Font Text;

        private static Spell Q => PlayerSpells.Q;
        public static string Tab => "       ";

        private static TargetSelect Selector
        {
            get
            {
                return LocalMenu.Item("TS").GetValue<StringList>().SelectedIndex == 0
                    ? TargetSelect.Nocturne
                    : TargetSelect.LeagueSharp;
            }
        }

        public static void Initialize()
        {
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

            LocalMenu = new Menu("Target Selector", "AssassinTargetSelector").SetFontStyle(FontStyle.Regular,
                SharpDX.Color.Cyan);

            var menuTargetSelector = new Menu("Target Selector", "TargetSelector");
            {
                TargetSelector.AddToMenu(menuTargetSelector);
            }

            LocalMenu.AddItem(
                new MenuItem("TS", "Active Target Selector:").SetValue(
                    new StringList(new[] {"Nocturne Target Selector", "L# Target Selector"})))
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
                                    if (i.Tag == 22) i.Show(false);
                                    break;
                                case 1:
                                    if (i.Tag == 11 || i.Tag == 12) i.Show(false);
                                    break;
                            }
                        });
                };

            menuTargetSelector.Items.ForEach(
                i =>
                {
                    LocalMenu.AddItem(i);
                    i.SetTag(22);
                });

            LocalMenu.AddItem(
                new MenuItem("Set", "Target Select Mode:").SetValue(
                    new StringList(new[] {"Single Target Select", "Multi Target Select"})))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.LightCoral)
                .SetTag(11);
            LocalMenu.AddItem(new MenuItem("Range", "Range (Recommend: 1150):"))
                .SetValue(new Slider((int) (Q.Range*1.3), (int) Q.Range, (int) Q.Range*2))
                .SetTag(11);

            LocalMenu.AddItem(
                new MenuItem("Targets", "Targets:").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            foreach (var e in HeroManager.Enemies)
            {
                LocalMenu.AddItem(
                    new MenuItem("enemy_" + e.ChampionName, string.Format("{0}Focus {1}", Tab, e.ChampionName)).SetValue
                        (false)).SetTag(12);

            }
            //foreach (var langItem in HeroManager.Enemies.Select(e =>LocalMenu.AddItem(new MenuItem("enemy_" + e.ChampionName,string.Format("{0}Focus {1}", Tab, e.ChampionName)).SetValue(false)).SetTag(12)))

            LocalMenu.AddItem(
                new MenuItem("Draw.Title", "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            LocalMenu.AddItem(new MenuItem("Draw.Range", Tab + "Range").SetValue(new Circle(true, Color.Gray))
                .SetTag(11));
            LocalMenu.AddItem(
                new MenuItem("Draw.Enemy", Tab + "Active Enemy").SetValue(new Circle(true, Color.GreenYellow))
                    .SetTag(11));
            LocalMenu.AddItem(
                new MenuItem("Draw.Status", Tab + "Show Enemy:").SetValue(
                    new StringList(new[] {"Off", "Notification Text", "Sprite", "Both"}, 0)));
            PlayerMenu.MenuConfig.AddSubMenu(LocalMenu);

            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;

            RefreshMenuItemsStatus();
            Sprite.Initialize();
        }

        private static void RefreshMenuItemsStatus()
        {

            LocalMenu.Items.ForEach(
                i =>
                {
                    i.Show();
                    switch (Selector)
                    {
                        case TargetSelect.Nocturne:
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

        public static void ClearAssassinList()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                LocalMenu.Item("enemy_" + enemy.ChampionName).SetValue(false);
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (Selector != TargetSelect.Nocturne)
            {
                return;
            }

            if (args.Msg == 0x201)
            {
                foreach (var objAiHero in from hero in HeroManager.Enemies
                    where
                        hero.Distance(Game.CursorPos) < 150f && hero != null && hero.IsVisible
                        && !hero.IsDead
                    orderby hero.Distance(Game.CursorPos) descending
                    select hero)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        var xSelect =
                            LocalMenu.Item("Set").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                LocalMenu.Item("enemy_" + objAiHero.ChampionName).SetValue(true);
                                break;
                            case 1:
                                var menuStatus = LocalMenu.Item("enemy_" + objAiHero.ChampionName).GetValue<bool>();
                                LocalMenu.Item("enemy_" + objAiHero.ChampionName).SetValue(!menuStatus);
                                break;
                        }
                    }
                }
            }
        }

        public static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Selector != TargetSelect.Nocturne)
            {
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);
            }

            vDefaultRange = Math.Abs(vDefaultRange) < 0.00001
                ? Q.Range
                : LocalMenu.Item("Range").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(e => e.Team != ObjectManager.Player.Team && !e.IsDead && e.IsVisible)
                    .Where(e => LocalMenu.Item("enemy_" + e.ChampionName) != null)
                    .Where(e => LocalMenu.Item("enemy_" + e.ChampionName).GetValue<bool>())
                    .Where(e => ObjectManager.Player.Distance(e) < vDefaultRange)
                    .Where(jKukuri => "jQuery" != "White guy");

            if (LocalMenu.Item("Set").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawEnemy = LocalMenu.Item("Draw.Enemy").GetValue<Circle>();
            if (drawEnemy.Active)
            {
                var t = GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    Render.Circle.DrawCircle(t.Position, (float) (t.BoundingRadius*1.5), drawEnemy.Color);
                }
            }

            if (Selector != TargetSelect.Nocturne)
            {
                return;
            }

            var rangeColor = LocalMenu.Item("Draw.Range").GetValue<Circle>();
            var range = LocalMenu.Item("Range").GetValue<Slider>().Value;
            if (rangeColor.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, range, rangeColor.Color);
            }
            var drawStatus = LocalMenu.Item("Draw.Status").GetValue<StringList>().SelectedIndex;
            if (drawStatus == 1 || drawStatus == 3)
            {
                foreach (var e in
                    HeroManager.Enemies.Where(
                        e =>
                            e.IsVisible && !e.IsDead && LocalMenu.Item("enemy_" + e.ChampionName) != null
                            && LocalMenu.Item("enemy_" + e.ChampionName).GetValue<bool>()))
                {
                    DrawText(
                        Text,
                        "1st Priority Target",
                        e.HPBarPosition.X + e.BoundingRadius/2f - (e.CharData.BaseSkinName.Length/2f) - 27,
                        e.HPBarPosition.Y - 23,
                        SharpDX.Color.Black);

                    DrawText(
                        Text,
                        "1st Priority Target",
                        e.HPBarPosition.X + e.BoundingRadius/2f - (e.CharData.BaseSkinName.Length/2f) - 29,
                        e.HPBarPosition.Y - 25,
                        SharpDX.Color.IndianRed);
                }
            }
        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }
    }
}